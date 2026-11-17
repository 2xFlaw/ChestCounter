using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows.Forms;

namespace ChestCounter;

public sealed record PrizeChance(string Key, string Name, double Den, double Boost, double Chance, double Next50, bool Owned);

public sealed record ChanceSnapshot(
    IReadOnlyList<PrizeChance> Prizes, int Chests, TreasureWindow? Live, TreasureWindow? Next, DateTime WeekStart,
    bool LiveUnknown, bool NextUnknown);

/// <summary>
/// Cumulative chance, this week, of having found each prize: 1 minus the product of (1 - rate) over every chest you
/// opened since the weekly reset. Treasure Hour boosts apply to chests opened inside a Treasure Hour.
/// Treasure Hours come only from your odds file (2xflaw-odds.json). The last two (see TreasureHours) have no announced
/// boosts yet, so they count as normal odds until you confirm them.
/// </summary>
public static class WeeklyChanceModel
{
    public const int Lookahead = 50;

    // Per-chest odds published by Epic (via fortnite.bot). Constant, except during Treasure Hours.
    public static readonly (string Key, string Name, double Den)[] Table =
    {
        ("gold-outfit", "Weekly Gold Outfit", 1562500),
        ("pirate-shop-access", "Pirate Shop Access", 137500),
        ("vehicle-decal", "Weekly Vehicle & Decal", 7813),
        ("pickaxe-back-bling", "Weekly Pickaxe or Back Bling", 568),
        ("jam-track", "Weekly Jam Track", 391),
        ("loading-screen", "Weekly Loading Screen", 272),
        ("gizmo", "Gizmo (Any)", 127),
    };

    private static T? Read<T>(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Boost a Treasure Hour gives to a prize (1 when it has none, or when its boosts are not announced).</summary>
    public static double BoostFor(TreasureWindow? w, string key) => TreasureHours.Boost(w, key);

    public static ChanceSnapshot Compute(Store store, Monitor monitor, DateTime now)
    {
        var cache = PersonalReadCache.Load(store);
        var key = Profiles.Current(cache, monitor.Config)?.Key ?? "";
        var ledger = Read<PersonalLedger>(Path.Combine(store.Folder, "2xflaw-ledger.json")) ?? new PersonalLedger();
        var odds = Read<PersonalOdds>(Path.Combine(store.Folder, "2xflaw-odds.json")) ?? PersonalOdds.Seed();

        var start = WeeklyReset.Start(now);
        var end = start.AddDays(7);

        var windows = odds.Windows.OrderBy(w => w.StartUtc).ToList();

        var all = cache.Matches.Concat(ledger.Imports.Select(x => x.Match))
            .Where(m => Profiles.Key(m) == key && m.IsBattleRoyale)
            .GroupBy(m => string.IsNullOrEmpty(m.SessionId) ? $"{m.ReplayFile}|{m.MatchStartUtc:O}" : m.SessionId)
            .Select(g => g.First()).ToList();
        var matches = all.Where(m => m.MatchStartUtc is { } t && t < end && t.AddSeconds(m.DurationSeconds ?? 0) >= start).ToList();

        var events = new List<(DateTime? time, int count)>();
        foreach (var m in matches)
        {
            if (m.Chests.Count > 0)
            {
                events.AddRange(m.Chests.Select(c => ((DateTime?)m.MatchStartUtc!.Value.AddSeconds(c.TimeSeconds), 1)).Where(e => e.Item1 >= start && e.Item1 < end));
            }
            else
            {
                events.Add((null, PersonalWeek.PersonalCount(m, start, end)));
            }
        }
        foreach (var e in ledger.Entries.Where(le => le.Profile == key && le.AtUtc >= start && le.AtUtc <= now))
        {
            events.Add((e.AtUtc, e.Mine));
        }

        var live = windows.FirstOrDefault(w => now >= w.StartUtc && now < w.EndUtc);
        var next = windows.Where(w => w.StartUtc > now).OrderBy(w => w.StartUtc).FirstOrDefault();

        var owned = monitor.Config.OwnedRewards;
        var prizes = new List<PrizeChance>();
        foreach (var (prizeKey, name, den) in Table)
        {
            double noDrop = 1;
            foreach (var e in events)
            {
                var w = e.time is { } t ? windows.FirstOrDefault(x => t >= x.StartUtc && t < x.EndUtc) : null;
                noDrop *= Math.Pow(1 - Math.Clamp(BoostFor(w, prizeKey) / den, 0, 1), e.count);
            }
            var chance = 1 - noDrop;
            var rateNow = Math.Clamp(BoostFor(live, prizeKey) / den, 0, 1);
            var next50 = 1 - noDrop * Math.Pow(1 - rateNow, Lookahead);
            var isOwned = prizeKey != "gizmo" && owned.TryGetValue(WeeklyReset.OwnedKey(prizeKey, now), out var v) && v;
            prizes.Add(new PrizeChance(prizeKey, name, den, BoostFor(live, prizeKey), chance, next50, isOwned));
        }

        return new ChanceSnapshot(
            prizes, events.Sum(x => x.count), live, next, start,
            live != null && TreasureHours.IsUnconfirmed(live), next != null && TreasureHours.IsUnconfirmed(next));
    }

    /// <summary>Extra chests (at the normal rate) until the cumulative chance reaches 50%; 0 when already past it.</summary>
    public static double ChestsToHalf(double chance, double den)
    {
        if (chance >= 0.5)
        {
            return 0;
        }
        var p = 1 / den;
        return Math.Log(0.5 / (1 - chance)) / Math.Log(1 - p);
    }

    /// <summary>Cumulative chance after <paramref name="chests"/> more chests at a per-chest boost.</summary>
    public static double After(double chance, double den, double boost, int chests) =>
        1 - (1 - chance) * Math.Pow(1 - Math.Clamp(boost / den, 0, 1), chests);
}

internal static class ChanceText
{
    public static string Pct(double chance)
    {
        var v = chance * 100;
        if (v <= 0)
        {
            return "0%";
        }
        return (v >= 10 ? v.ToString("0.0") : v >= 1 ? v.ToString("0.00") : v.ToString("0.0000")) + "%";
    }

    public static void Draw(Graphics g, string text, Font font, Rectangle box, Color color, TextFormatFlags flags = TextFormatFlags.Left)
    {
        TextRenderer.DrawText(g, text, font, box, color,
            flags | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    /// <summary>Pill-shaped tag, right edge at <paramref name="right"/>. The text is set in wide-spaced capitals.</summary>
    public static void Chip(Graphics g, string text, Font font, int right, int top, Color accent)
    {
        text = Fonts.Track(text.ToUpperInvariant());
        var size = TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var rect = new RectangleF(right - size.Width - 20, top, size.Width + 20, size.Height + 10);
        using var path = Shape.Rounded(rect, rect.Height / 2);
        using var fill = new SolidBrush(Color.FromArgb(34, accent));
        using var edge = new Pen(Color.FromArgb(120, accent), 1f);
        g.FillPath(fill, path);
        g.DrawPath(edge, path);
        Draw(g, text, font, Rectangle.Round(rect), accent, TextFormatFlags.HorizontalCenter);
    }

    /// <summary>Pill-shaped tag, left edge at <paramref name="left"/>.</summary>
    public static void ChipAt(Graphics g, string text, Font font, int left, int top, Color accent)
    {
        var tracked = Fonts.Track(text.ToUpperInvariant());
        var size = TextRenderer.MeasureText(g, tracked, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        Chip(g, text, font, left + size.Width + 20, top, accent);
    }

    /// <summary>Four-point sparkle.</summary>
    public static void Star(Graphics g, float cx, float cy, float r, Color color)
    {
        var points = new PointF[8];
        for (var i = 0; i < 8; i++)
        {
            var angle = Math.PI / 4 * i - Math.PI / 2;
            var radius = i % 2 == 0 ? r : r * 0.26f;
            points[i] = new PointF(cx + (float)(Math.Cos(angle) * radius), cy + (float)(Math.Sin(angle) * radius));
        }
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, points);
    }
}

/// <summary>The big card: picture of the goal, its cumulative odds and an animated progression path.</summary>
[SupportedOSPlatform("windows")]
internal sealed class GoalPanel : Control
{
    public sealed record Vm(string Name, string Tag, bool Auto, double Chance, double Den, double Boost, int Chests, double ChestsToHalf, double GainNow, double GainNormal, int Ahead, Bitmap Image, string? LiveText);

    private static readonly (float X, float Y, float Size, float Phase, float Speed)[] Sparks =
    {
        (0.06f, 0.10f, 8f, 0.0f, 1.1f),
        (0.95f, 0.18f, 10f, 1.7f, 0.9f),
        (0.50f, 0.00f, 6f, 3.1f, 1.3f),
        (1.00f, 0.62f, 8f, 4.2f, 1.0f),
        (0.02f, 0.72f, 9f, 2.4f, 1.2f),
        (0.84f, 1.00f, 6f, 5.0f, 0.8f),
        (0.30f, 1.00f, 5f, 0.9f, 1.4f),
    };

    private readonly Font _tag = Fonts.Body(7.5f, FontStyle.Bold);
    private readonly Font _title = Fonts.Display(22f);
    private readonly Font _big = Fonts.Display(38f);
    private readonly Font _sub = Theme.Text(9f);
    private readonly Font _subBold = Theme.Text(9f, FontStyle.Bold);
    private readonly Font _small = Theme.Text(7.5f);
    private Vm? _vm;
    private string? _message;
    private double _shown;
    private double _target;

    public GoalPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Height = 244;
    }

    /// <summary>Sparkles and the shimmer on the progress bar.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Sparkle { get; set; } = true;

    public void ShowGoal(Vm vm)
    {
        _vm = vm;
        _message = null;
        _target = vm.Chance;
        Invalidate();
    }

    public void ShowMessage(string message)
    {
        _vm = null;
        _message = message;
        Invalidate();
    }

    /// <summary>Advances the count-up / fill animation. Returns true while it is still moving.</summary>
    public bool Step()
    {
        var d = _target - _shown;
        if (Math.Abs(d) < 0.00002)
        {
            if (_shown != _target)
            {
                _shown = _target;
                Invalidate();
            }
            return false;
        }
        _shown += d * 0.14;
        Invalidate();
        return true;
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 120)
        {
            return;
        }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var boosted = _vm?.Boost > 1;
        var accent = boosted ? Theme.Amber : Theme.Cyan;
        var seconds = Environment.TickCount64 / 1000.0;
        var card = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var path = Shape.Rounded(card, 20))
        {
            using var fill = new SolidBrush(Theme.Card);
            g.FillPath(fill, path);
            g.SetClip(path);
            using (var glow = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), Color.FromArgb(boosted ? 56 : 38, accent), Color.FromArgb(0, accent), LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(glow, 0, 0, Width, Height);
            }
            g.ResetClip();
            using var edge = new Pen(Theme.CardEdge, 1f);
            g.DrawPath(edge, path);
        }

        if (_vm == null)
        {
            ChanceText.Draw(g, _message ?? "", _title, new Rectangle(24, 0, Width - 48, Height), Theme.Mint, TextFormatFlags.HorizontalCenter);
            return;
        }

        var vm = _vm;
        const int picture = 190;
        var pictureRect = new Rectangle(22, 24, picture, picture);
        g.DrawImage(vm.Image, pictureRect);
        using (var ring = Shape.Rounded(new RectangleF(pictureRect.X, pictureRect.Y, pictureRect.Width, pictureRect.Height), 16))
        using (var ringPen = new Pen(Color.FromArgb(150, accent), 2f))
        {
            g.DrawPath(ringPen, ring);
        }

        if (Sparkle)
        {
            foreach (var (sx, sy, size, phase, speed) in Sparks)
            {
                var wave = Math.Sin(seconds * speed * 2.2 + phase);
                var k = wave > 0 ? wave * wave : 0;
                if (k < 0.02)
                {
                    continue;
                }
                var color = Color.FromArgb((int)(40 + 215 * k), boosted ? Theme.Amber : Color.White);
                ChanceText.Star(g, pictureRect.X + sx * pictureRect.Width, pictureRect.Y + sy * pictureRect.Height, (float)(size * (0.55 + 0.45 * k)), color);
            }
        }

        var x0 = pictureRect.Right + 26;
        var xr = Width - 24;

        ChanceText.ChipAt(g, vm.Tag, _tag, x0, 22, vm.Auto ? Theme.Cyan : Theme.Amber);
        if (vm.LiveText != null)
        {
            ChanceText.Chip(g, vm.LiveText, _tag, xr, 22, Theme.Amber);
        }

        ChanceText.Draw(g, vm.Name.ToUpperInvariant(), _title, new Rectangle(x0, 50, xr - x0, 34), Theme.Ink);
        ChanceText.Draw(g, ChanceText.Pct(_shown), _big, new Rectangle(x0, 84, xr - x0, 54), accent);
        ChanceText.Draw(g, "cumulative chance this week", _sub, new Rectangle(x0, 138, xr - x0, 18), Theme.Muted);
        ChanceText.Draw(g, $"1 in {vm.Den:N0} per chest   \u00b7   {vm.Chests:N0} chests opened this week", _sub, new Rectangle(x0, 158, xr - x0, 18), Theme.Ink);
        ChanceText.Draw(g,
            vm.ChestsToHalf <= 0 ? "Past the 50% mark" : $"About {Math.Ceiling(vm.ChestsToHalf):N0} more chests to reach 50%",
            _subBold, new Rectangle(x0, 178, xr - x0, 18), vm.ChestsToHalf <= 0 ? Theme.Mint : Theme.Muted);

        var lookahead = vm.Boost > 1
            ? $"If you open {vm.Ahead:N0} more chests: +{ChanceText.Pct(vm.GainNow)} now (Treasure Hour) vs +{ChanceText.Pct(vm.GainNormal)} normally"
            : $"If you open {vm.Ahead:N0} more chests: +{ChanceText.Pct(vm.GainNormal)}";
        ChanceText.Draw(g, lookahead, _sub, new Rectangle(x0, 138, xr - x0, 18), vm.Boost > 1 ? Theme.Amber : Theme.Muted, TextFormatFlags.Right);

        // The path shows only what you have really earned this week (empty at 0%).
        var left = x0 + 7;
        var right = xr - 8;
        var track = new RectangleF(left, 206, right - left, 10);
        using (var trackPath = Shape.Rounded(track, 5))
        using (var trackFill = new SolidBrush(Theme.Panel))
        {
            g.FillPath(trackFill, trackPath);
        }

        var shown = Math.Clamp(_shown, 0, 1);
        var fillWidth = (float)(track.Width * shown);
        if (shown > 0.0005 && fillWidth > 0.5f)
        {
            var fill = new RectangleF(track.X, track.Y, Math.Max(fillWidth, 10f), track.Height);
            using var fillPath = Shape.Rounded(fill, 5);
            using (var gradient = new LinearGradientBrush(fill, Theme.Cyan, Theme.Mint, LinearGradientMode.Horizontal))
            {
                g.FillPath(gradient, fillPath);
            }

            if (Sparkle && fill.Width > 24)
            {
                // A soft highlight sweeping along the filled part of the bar.
                var sweep = (float)((seconds % 2.6) / 2.6);
                var bandX = fill.X - 40 + sweep * (fill.Width + 80);
                g.SetClip(fillPath);
                using var shine = new LinearGradientBrush(new RectangleF(bandX, fill.Y, 40, fill.Height), Color.FromArgb(0, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Horizontal)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Positions = new[] { 0f, 0.5f, 1f },
                        Colors = new[] { Color.FromArgb(0, Color.White), Color.FromArgb(110, Color.White), Color.FromArgb(0, Color.White) },
                    },
                };
                g.FillRectangle(shine, bandX, fill.Y, 40, fill.Height);
                g.ResetClip();
            }
        }

        var milestones = new (double At, string Label)[] { (0, "0"), (0.25, "25%"), (0.5, "50%"), (0.75, "75%"), (1, "100%") };
        foreach (var (at, label) in milestones)
        {
            var cx = track.X + (float)(track.Width * at);
            var reached = shown >= at - 1e-9 && at > 0;
            const float d = 14f;
            var dot = new RectangleF(cx - d / 2, track.Y + track.Height / 2 - d / 2, d, d);
            using (var dotFill = new SolidBrush(reached ? Theme.Mint : Theme.Card))
            using (var dotEdge = new Pen(reached ? Theme.Mint : Theme.Line, 2f))
            {
                g.FillEllipse(dotFill, dot);
                g.DrawEllipse(dotEdge, dot);
            }
            ChanceText.Draw(g, label, _small, new Rectangle((int)cx - 30, 222, 60, 14), reached ? Theme.Mint : Theme.Muted, TextFormatFlags.HorizontalCenter);
        }

        if (shown > 0.003 && shown < 1)
        {
            var mx = track.X + (float)(track.Width * shown);
            var marker = new RectangleF(mx - 8, track.Y + track.Height / 2 - 8, 16, 16);
            using var halo = new SolidBrush(Color.FromArgb(70, accent));
            g.FillEllipse(halo, marker.X - 4, marker.Y - 4, marker.Width + 8, marker.Height + 8);
            using var core = new SolidBrush(Theme.Ink);
            g.FillEllipse(core, marker);
            using var corePen = new Pen(accent, 2.5f);
            g.DrawEllipse(corePen, marker);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tag.Dispose();
            _title.Dispose();
            _big.Dispose();
            _sub.Dispose();
            _subBold.Dispose();
            _small.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Treasure Hour planner: pick how many chests you plan to open and see your cumulative chance for the goal
/// at the normal rate next to the Treasure Hour rate. This is the "what is the hour worth" view.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class PlannerPanel : Control
{
    public sealed record Vm(string Status, bool Live, string WindowText, string PrizeName, double Chance, double Den, double Boost, bool Unknown);

    public static readonly int[] Options = { 25, 50, 100, 200, 400 };
    private const int ChipW = 54;
    private const int ChipGap = 6;

    private readonly Font _label = Fonts.Body(7.5f, FontStyle.Bold);
    private readonly Font _chip = Theme.Text(9f, FontStyle.Bold);
    private readonly Font _value = Fonts.Display(24f);
    private readonly Font _sub = Theme.Text(9f);
    private readonly Font _arrow = Theme.Text(16f, FontStyle.Bold);
    private Vm? _vm;
    private int _n = 100;
    private int _hover = -1;
    private double _normalShown, _normalTarget, _boostShown, _boostTarget;

    public PlannerPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Height = 134;
    }

    public event Action<int>? Chosen;

    public void SetState(Vm? vm, int chests)
    {
        _vm = vm;
        _n = chests;
        if (vm != null)
        {
            _normalTarget = WeeklyChanceModel.After(vm.Chance, vm.Den, 1, chests);
            _boostTarget = WeeklyChanceModel.After(vm.Chance, vm.Den, vm.Boost, chests);
        }
        Invalidate();
    }

    public bool Step()
    {
        var moving = false;
        void Ease(ref double shown, double target)
        {
            var d = target - shown;
            if (Math.Abs(d) < 0.00002)
            {
                shown = target;
                return;
            }
            shown += d * 0.16;
            moving = true;
        }
        Ease(ref _normalShown, _normalTarget);
        Ease(ref _boostShown, _boostTarget);
        Invalidate();
        return moving;
    }

    private static Rectangle ChipRect(int i) => new(20 + i * (ChipW + ChipGap), 88, ChipW, 30);

    private int ChipAt(Point p)
    {
        for (var i = 0; i < Options.Length; i++)
        {
            if (ChipRect(i).Contains(p))
            {
                return i;
            }
        }
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var index = ChipAt(e.Location);
        if (index != _hover)
        {
            _hover = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }
        var index = ChipAt(e.Location);
        if (index >= 0)
        {
            Chosen?.Invoke(Options[index]);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 300 || _vm == null)
        {
            return;
        }
        var vm = _vm;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var accent = vm.Live ? Theme.Amber : Theme.Cyan;

        using (var path = Shape.Rounded(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 18))
        using (var fill = new SolidBrush(Theme.Card))
        using (var edge = new Pen(Theme.CardEdge, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        ChanceText.ChipAt(g, vm.Status, _label, 20, 14, accent);
        ChanceText.Draw(g, vm.WindowText, _sub, new Rectangle(20, 14, Width - 44, 28), Theme.Muted, TextFormatFlags.Right);
        ChanceText.Draw(g, Fonts.Track("IF YOU OPEN THIS MANY CHESTS"), _label, new Rectangle(20, 60, 340, 22), Theme.Muted);

        for (var i = 0; i < Options.Length; i++)
        {
            var rect = ChipRect(i);
            var on = Options[i] == _n;
            using var chipPath = Shape.Rounded(rect, 10);
            using var chipFill = new SolidBrush(on ? Color.FromArgb(46, accent) : i == _hover ? Theme.PanelHover : Theme.Panel);
            using var chipEdge = new Pen(on ? accent : Theme.CardEdge, on ? 1.5f : 1f);
            g.FillPath(chipFill, chipPath);
            g.DrawPath(chipEdge, chipPath);
            ChanceText.Draw(g, Options[i].ToString("N0"), _chip, rect, on ? accent : Theme.Ink, TextFormatFlags.HorizontalCenter);
        }

        const int block = 190;
        var x2 = Width - 24 - block;
        var x1 = x2 - block - 26;
        if (x1 < 20 + Options.Length * (ChipW + ChipGap) + 10)
        {
            return;
        }

        ChanceText.Draw(g, Fonts.Track("NORMAL RATE"), _label, new Rectangle(x1, 52, block, 18), Theme.Muted);
        ChanceText.Draw(g, ChanceText.Pct(_normalShown), _value, new Rectangle(x1, 70, block, 32), Theme.Ink);
        ChanceText.Draw(g, $"+{ChanceText.Pct(_normalTarget - vm.Chance)}", _sub, new Rectangle(x1, 102, block, 18), Theme.Muted);

        if (vm.Boost > 1)
        {
            ChanceText.Draw(g, "\u2192", _arrow, new Rectangle(x1 + block, 66, 26, 36), Theme.Muted, TextFormatFlags.HorizontalCenter);
            ChanceText.Draw(g, Fonts.Track($"TREASURE HOUR {vm.Boost:0.#}\u00d7"), _label, new Rectangle(x2, 52, block, 18), Theme.Amber);
            ChanceText.Draw(g, ChanceText.Pct(_boostShown), _value, new Rectangle(x2, 70, block, 32), Theme.Amber);
            ChanceText.Draw(g, $"+{ChanceText.Pct(_boostTarget - vm.Chance)}", _sub, new Rectangle(x2, 102, block, 18), Theme.Amber);
        }
        else if (vm.Unknown)
        {
            // Boosts of the last two Treasure Hours are not announced yet: a grey question mark.
            var badge = new RectangleF(x2, 62, 44, 44);
            using (var badgePath = Shape.Rounded(badge, 12))
            using (var badgeFill = new SolidBrush(Color.FromArgb(58, 51, 69)))
            {
                g.FillPath(badgeFill, badgePath);
            }
            ChanceText.Draw(g, "?", _value, Rectangle.Round(badge), Color.FromArgb(196, 186, 209), TextFormatFlags.HorizontalCenter);
            ChanceText.Draw(g, "Boosts not announced yet", _sub, new Rectangle(x2 + 56, 62, block - 40, 44), Theme.Muted);
        }
        else
        {
            ChanceText.Draw(g, "No Treasure Hour boost for this prize", _sub, new Rectangle(x2 - 40, 70, block + 40, 32), Theme.Muted);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _label.Dispose();
            _chip.Dispose();
            _value.Dispose();
            _sub.Dispose();
            _arrow.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class OddsRow
{
    public required PrizeChance Prize { get; init; }
    public required Bitmap Image { get; init; }
    public double Fill;
    public double Target;

    /// <summary>Cumulative chance after the number of chests chosen in the planner.</summary>
    public double Ahead;
}

/// <summary>Modern odds table: one animated row per prize still to find. Click a row to aim for that prize.</summary>
[SupportedOSPlatform("windows")]
internal sealed class OddsTable : Control
{
    private const int HeaderH = 36;
    private const int RowH = 62;

    private readonly Font _head = Fonts.Body(7.5f, FontStyle.Bold);
    private readonly Font _name = Theme.Text(10f, FontStyle.Bold);
    private readonly Font _sub = Theme.Text(8.5f);
    private readonly Font _pct = Fonts.Display(16f);
    private List<OddsRow> _rows = new();
    private string? _selected;
    private int _hover = -1;
    private int _ahead = 100;

    public OddsTable()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public event Action<string>? Picked;

    public IReadOnlyList<OddsRow> Rows => _rows;

    public int WantedHeight => HeaderH + _rows.Count * RowH + 10;

    public void SetRows(List<OddsRow> rows, string? selected, int ahead)
    {
        _rows = rows;
        _selected = selected;
        _ahead = ahead;
        Invalidate();
    }

    public bool Step()
    {
        var moving = false;
        foreach (var row in _rows)
        {
            var d = row.Target - row.Fill;
            if (Math.Abs(d) < 0.0004)
            {
                row.Fill = row.Target;
                continue;
            }
            row.Fill += d * 0.12;
            moving = true;
        }
        Invalidate();
        return moving;
    }

    private int RowAt(Point p)
    {
        var index = (p.Y - HeaderH) / RowH;
        return p.Y >= HeaderH && index >= 0 && index < _rows.Count ? index : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var index = RowAt(e.Location);
        if (index != _hover)
        {
            _hover = index;
            Cursor = index >= 0 && _rows[index].Prize.Key != "gizmo" ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }
        var index = RowAt(e.Location);
        if (index >= 0 && _rows[index].Prize.Key != "gizmo")
        {
            Picked?.Invoke(_rows[index].Prize.Key);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 200)
        {
            return;
        }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using (var path = Shape.Rounded(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 18))
        using (var fill = new SolidBrush(Theme.Card))
        using (var edge = new Pen(Theme.CardEdge, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        var barLeft = 310;
        var barRight = Width - 150;
        ChanceText.Draw(g, Fonts.Track("PRIZE"), _head, new Rectangle(20, 0, 200, HeaderH), Theme.Muted);
        ChanceText.Draw(g, Fonts.Track($"CUMULATIVE ODDS THIS WEEK  \u00b7  DASHED = IF YOU OPEN {_ahead:N0} CHESTS"), _head, new Rectangle(barLeft, 0, Math.Max(40, barRight - barLeft), HeaderH), Theme.Muted);
        ChanceText.Draw(g, Fonts.Track("CHANCE"), _head, new Rectangle(Width - 130, 0, 112, HeaderH), Theme.Muted, TextFormatFlags.Right);

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var top = HeaderH + i * RowH;
            var selected = row.Prize.Key == _selected;
            var boosted = row.Prize.Boost > 1;
            var detail = selected || i == _hover;

            var band = new RectangleF(8, top + 3, Width - 16, RowH - 6);
            if (detail)
            {
                using var bandPath = Shape.Rounded(band, 12);
                using var bandFill = new SolidBrush(selected ? Color.FromArgb(30, Theme.Cyan) : Theme.PanelHover);
                g.FillPath(bandFill, bandPath);
                if (selected)
                {
                    using var bandEdge = new Pen(Color.FromArgb(110, Theme.Cyan), 1.2f);
                    g.DrawPath(bandEdge, bandPath);
                }
            }
            else if (i > 0)
            {
                using var line = new Pen(Theme.CardEdge, 1f);
                g.DrawLine(line, 24, top, Width - 24, top);
            }

            g.DrawImage(row.Image, new Rectangle(20, top + 11, 40, 40));

            var textLeft = 74;
            ChanceText.Draw(g, row.Prize.Name, _name, new Rectangle(textLeft, top + 10, barLeft - textLeft - 12, 22), Theme.Ink);
            var sub = row.Prize.Key == "gizmo" ? $"1 in {row.Prize.Den:N0} per chest  \u00b7  can repeat" : $"1 in {row.Prize.Den:N0} per chest";
            ChanceText.Draw(g, sub, _sub, new Rectangle(textLeft, top + 32, barLeft - textLeft - 12, 18), Theme.Muted);
            if (boosted)
            {
                var subWidth = TextRenderer.MeasureText(g, sub, _sub, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                ChanceText.Draw(g, $"  \u00b7  {row.Prize.Boost:0.#}\u00d7 now", _sub, new Rectangle(textLeft + subWidth, top + 32, 80, 18), Theme.Amber);
            }

            var track = new RectangleF(barLeft, top + RowH / 2f - 4, Math.Max(40, barRight - barLeft), 8);
            using (var trackPath = Shape.Rounded(track, 4))
            using (var trackFill = new SolidBrush(Theme.Panel))
            {
                g.FillPath(trackFill, trackPath);
            }

            // Projection: shown only for the selected or hovered row, as a dashed outline that follows the planner.
            var ghostEnd = Math.Clamp(row.Fill + (row.Ahead - row.Prize.Chance), 0, 1);
            if (detail && ghostEnd > row.Fill + 0.0005)
            {
                var gx = track.X + (float)(track.Width * row.Fill);
                var gw = (float)(track.Width * (ghostEnd - row.Fill));
                if (gw >= 2f)
                {
                    var ghost = new RectangleF(gx, track.Y, gw, track.Height);
                    var color = boosted ? Theme.Amber : Theme.Cyan;
                    using var ghostPath = Shape.Rounded(ghost, 4);
                    using var ghostFill = new SolidBrush(Color.FromArgb(boosted ? 90 : 50, color));
                    using var ghostEdge = new Pen(Color.FromArgb(210, color), 1f) { DashStyle = DashStyle.Dash };
                    g.FillPath(ghostFill, ghostPath);
                    g.DrawPath(ghostEdge, ghostPath);
                }
            }

            var fillWidth = (float)(track.Width * Math.Clamp(row.Fill, 0, 1));
            if (row.Fill > 0.0005 && fillWidth > 0.5f)
            {
                var fill = new RectangleF(track.X, track.Y, Math.Max(fillWidth, 8f), track.Height);
                using var fillPath = Shape.Rounded(fill, 4);
                using var gradient = new LinearGradientBrush(fill, Theme.Cyan, Theme.Mint, LinearGradientMode.Horizontal);
                g.FillPath(gradient, fillPath);
            }

            var pctBox = detail ? new Rectangle(Width - 130, top + 8, 112, 28) : new Rectangle(Width - 130, top, 112, RowH);
            ChanceText.Draw(g, ChanceText.Pct(row.Fill), _pct, pctBox, selected ? Theme.Cyan : Theme.Ink, TextFormatFlags.Right);
            if (detail)
            {
                ChanceText.Draw(g, $"\u2192 {ChanceText.Pct(row.Ahead)}", _sub, new Rectangle(Width - 130, top + 36, 112, 16), boosted ? Theme.Amber : Theme.Muted, TextFormatFlags.Right);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _head.Dispose();
            _name.Dispose();
            _sub.Dispose();
            _pct.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Weekly chances: your cumulative odds this week for every prize you have not got yet. The big card shows your
/// goal (by default the next closest one); click any row to aim for another. Prizes ticked as owned in Weekly rewards
/// disappear. Treasure Hours switch on automatically, and the planner shows what a Treasure Hour is worth for your goal.
/// The week resets every Sunday at 14:00 GMT.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WeeklyChancesForm : Form
{
    private const int HeaderHeight = 124;

    private readonly Monitor _monitor;
    private readonly Store _store;
    private readonly string _folder;
    private readonly Dictionary<string, Bitmap> _images = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _data = new() { Interval = 15000 };
    private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
    private readonly System.Windows.Forms.Timer _sparkle = new() { Interval = 50 };
    private readonly Label _countdown;
    private readonly Label _hour;
    private readonly Panel _body;
    private readonly GoalPanel _goal = new();
    private readonly PlannerPanel _planner = new();
    private readonly OddsTable _table = new();
    private readonly Label _note;
    private readonly GlowButton _auto;
    private DateTime _weekStart;
    private string? _pick;
    private int _plan = 100;
    private ChanceSnapshot? _snap;

    public WeeklyChancesForm(Monitor monitor, Store store)
    {
        _monitor = monitor;
        _store = store;
        _folder = Path.Combine(store.Folder, "rewards");

        Text = "Weekly chances";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 900);
        MinimumSize = new Size(880, 560);
        Icon = AppIcon.Load();

        // Header laid out in three separate rows so nothing overlaps: title, subtitle, then the Auto button.
        var header = new Panel { Dock = DockStyle.Top, Height = HeaderHeight, BackColor = Theme.Bg };
        header.Controls.Add(new Label
        {
            Text = "WEEKLY CHANCES",
            Font = Fonts.Display(26f),
            ForeColor = Theme.Ink,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(500, 48),
            Location = new Point(20, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
        });
        header.Controls.Add(new Label
        {
            Text = "Your odds of having found each prize this week. Click a prize to aim for it.",
            Font = Theme.Text(9f),
            ForeColor = Theme.Muted,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(560, 22),
            Location = new Point(22, 56),
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
        });

        _auto = new GlowButton { Text = "Auto: next closest", Size = new Size(190, 34), Radius = 11, Font = Theme.Text(8.5f, FontStyle.Bold), Location = new Point(22, 84) };
        _auto.Click += (_, _) =>
        {
            _pick = null;
            SavePick(null);
            Apply();
        };

        _countdown = new Label
        {
            Font = Fonts.Display(16f),
            ForeColor = Theme.Cyan,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(420, 34),
            Location = new Point(900 - 20 - 420, 8),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
        };
        var resetNote = new Label
        {
            Text = "Resets every Sunday at 14:00 GMT",
            Font = Theme.Text(8f),
            ForeColor = Theme.Muted,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(420, 20),
            Location = new Point(900 - 20 - 420, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
        };
        _hour = new Label
        {
            Font = Theme.Text(9f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(560, 24),
            Location = new Point(900 - 20 - 560, 88),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
        };
        header.Controls.Add(_auto);
        header.Controls.Add(_countdown);
        header.Controls.Add(resetNote);
        header.Controls.Add(_hour);

        _note = new Label
        {
            Font = Theme.Text(8f),
            ForeColor = Theme.Muted,
            BackColor = Theme.Bg,
            AutoSize = false,
            UseMnemonic = false,
        };

        _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Bg };
        _body.Controls.Add(_goal);
        _body.Controls.Add(_planner);
        _body.Controls.Add(_table);
        _body.Controls.Add(_note);
        Controls.Add(_body);
        Controls.Add(header);
        _body.Resize += (_, _) => DoLayout();

        _table.Picked += key =>
        {
            _pick = key;
            SavePick(key);
            Apply();
        };
        _planner.Chosen += n =>
        {
            _plan = n;
            Apply();
        };

        _pick = LoadPick();
        _weekStart = WeeklyReset.Start(DateTime.UtcNow);

        _anim.Tick += (_, _) =>
        {
            var a = _goal.Step();
            var b = _table.Step();
            var c = _planner.Step();
            if (!a && !b && !c)
            {
                _anim.Stop();
            }
        };
        _sparkle.Tick += (_, _) =>
        {
            if (_goal.Sparkle && Visible && WindowState != FormWindowState.Minimized)
            {
                _goal.Invalidate();
            }
        };
        _clock.Tick += (_, _) =>
        {
            var now = DateTime.UtcNow;
            UpdateCountdown();
            var weekChanged = WeeklyReset.Start(now) != _weekStart;
            var hourChanged = _snap != null
                && ((_snap.Live != null && now >= _snap.Live.EndUtc) || (_snap.Next != null && now >= _snap.Next.StartUtc));
            if (weekChanged || hourChanged)
            {
                _weekStart = WeeklyReset.Start(now);
                RefreshData();
            }
        };
        _data.Tick += (_, _) => RefreshData();
        Activated += (_, _) => RefreshData();

        UpdateCountdown();
        RefreshData();
        _clock.Start();
        _data.Start();
        _sparkle.Start();
    }

    private string PickPath => Path.Combine(_store.Folder, "2xflaw-goal.txt");

    private string? LoadPick()
    {
        try
        {
            var text = File.Exists(PickPath) ? File.ReadAllText(PickPath).Trim() : "";
            return text.Length == 0 ? null : text;
        }
        catch
        {
            return null;
        }
    }

    private void SavePick(string? key)
    {
        try
        {
            File.WriteAllText(PickPath, key ?? "");
        }
        catch
        {
            // the goal just will not be remembered
        }
    }

    private static string Range(TreasureWindow w) =>
        $"{w.StartUtc:ddd d MMM HH:mm}\u2013{w.EndUtc:HH:mm} GMT" + (TreasureHours.IsUnconfirmed(w) ? "  (final \u00b7 boosts not announced yet)" : "");

    private void UpdateCountdown()
    {
        var now = DateTime.UtcNow;
        var left = WeeklyReset.Remaining(now);
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }
        _countdown.Text = $"RESETS IN {left.Days}D {left.Hours:00}H {left.Minutes:00}M {left.Seconds:00}S";

        if (_snap?.Live is { } live && now < live.EndUtc)
        {
            var l = live.EndUtc - now;
            _hour.Text = $"TREASURE HOUR LIVE  \u00b7  ends in {(int)l.TotalHours:00}:{l.Minutes:00}:{l.Seconds:00}" + (_snap.LiveUnknown ? "  \u00b7  boosts not announced" : "");
            _hour.ForeColor = _snap.LiveUnknown ? Theme.Muted : Theme.Amber;
        }
        else if (_snap?.Next is { } next)
        {
            var l = next.StartUtc - now;
            _hour.Text = $"Next Treasure Hour in {l.Days}d {l.Hours:00}h {l.Minutes:00}m {l.Seconds:00}s" + (_snap.NextUnknown ? "  \u00b7  final, boosts not announced" : "");
            _hour.ForeColor = _snap.NextUnknown ? Theme.Muted : Theme.Cyan;
        }
        else
        {
            _hour.Text = "No more Treasure Hours";
            _hour.ForeColor = Theme.Muted;
        }
    }

    private void RefreshData()
    {
        try
        {
            _snap = WeeklyChanceModel.Compute(_store, _monitor, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Weekly chances", ex);
            return;
        }
        Apply();
        UpdateCountdown();
    }

    private Bitmap Picture(string key, int size, int radius)
    {
        var id = $"{key}|{size}";
        if (!_images.TryGetValue(id, out var bitmap))
        {
            bitmap = RewardPictures.Load(_folder, key, size, radius);
            _images[id] = bitmap;
        }
        return bitmap;
    }

    private void Apply()
    {
        if (_snap is not { } snap)
        {
            return;
        }

        _goal.Sparkle = _monitor.Config.Animations;

        var open = snap.Prizes.Where(p => !p.Owned).ToList();
        var ordered = open.Where(p => p.Key != "gizmo").OrderByDescending(p => p.Chance).ThenBy(p => p.Den).Concat(open.Where(p => p.Key == "gizmo")).ToList();
        var candidates = ordered.Where(p => p.Key != "gizmo").ToList();

        var goal = _pick != null ? candidates.FirstOrDefault(p => p.Key == _pick) : null;
        var auto = goal == null;
        goal ??= candidates.FirstOrDefault();

        var previous = _table.Rows.ToDictionary(r => r.Prize.Key, r => r.Fill);
        _table.SetRows(
            ordered.Select(p => new OddsRow
            {
                Prize = p,
                Image = Picture(p.Key, 40, 10),
                Target = p.Chance,
                Fill = previous.TryGetValue(p.Key, out var f) ? f : 0,
                Ahead = WeeklyChanceModel.After(p.Chance, p.Den, p.Boost, _plan),
            }).ToList(),
            goal?.Key,
            _plan);

        if (goal == null)
        {
            _goal.ShowMessage("Everything is ticked as owned. Fresh prizes arrive at the reset.");
            _planner.Visible = false;
        }
        else
        {
            var live = snap.Live != null && goal.Boost > 1
                ? $"TREASURE HOUR  {goal.Boost:0.#}\u00d7"
                : null;
            _goal.ShowGoal(new GoalPanel.Vm(
                goal.Name,
                auto ? "NEXT CLOSEST" : "YOUR PICK",
                auto,
                goal.Chance,
                goal.Den,
                goal.Boost,
                snap.Chests,
                WeeklyChanceModel.ChestsToHalf(goal.Chance, goal.Den),
                WeeklyChanceModel.After(goal.Chance, goal.Den, goal.Boost, _plan) - goal.Chance,
                WeeklyChanceModel.After(goal.Chance, goal.Den, 1, _plan) - goal.Chance,
                _plan,
                Picture(goal.Key, 190, 16),
                live));

            var window = snap.Live ?? snap.Next;
            var unknown = window != null && TreasureHours.IsUnconfirmed(window);
            var status = snap.Live != null ? "LIVE NOW" : snap.Next != null ? "NEXT TREASURE HOUR" : "NO MORE TREASURE HOURS";
            var text = window == null ? "" : Range(window);
            _planner.Visible = true;
            _planner.SetState(
                new PlannerPanel.Vm(status, snap.Live != null, text, goal.Name, goal.Chance, goal.Den, WeeklyChanceModel.BoostFor(window, goal.Key), unknown),
                _plan);
        }

        var hidden = snap.Prizes.Count(p => p.Owned);
        _note.Text =
            "How it works: for each chest you opened since the weekly reset, a prize has a 1-in-N chance. Chests opened inside a Treasure Hour use the boosted rate. " +
            "The big percentage and the solid bars are what you have really earned this week, so they stay empty until you open chests. " +
            "The dashed outline (selected or hovered row) and the planner show what the number of chests you pick would be worth, with and without the Treasure Hour. " +
            "Chests of a match still in progress count once it ends. " +
            "Treasure Hour times come from your 2xflaw-odds.json file. The last two Treasure Hours (28 and 29 Nov) are confirmed, but their boosts are not announced yet, so they count as normal odds until you confirm them. " +
            "Except for Gizmos, you cannot get the same prize twice." +
            (hidden > 0 ? $" {hidden} owned prize{(hidden == 1 ? "" : "s")} hidden: untick them in Weekly rewards to bring them back." : "");

        var autoOn = _pick == null || auto;
        _auto.BackColor = autoOn ? Theme.Accent : Theme.Panel;
        _auto.ForeColor = autoOn ? Theme.AccentInk : Theme.Ink;
        _auto.FlatAppearance.BorderColor = autoOn ? Theme.Accent : Theme.CardEdge;
        _auto.Invalidate();

        DoLayout();
        if (!_anim.Enabled)
        {
            _anim.Start();
        }
    }

    private void DoLayout()
    {
        const int margin = 20;
        var width = Math.Max(640, _body.ClientSize.Width - margin * 2);
        _goal.SetBounds(margin, 8, width, 244);
        var y = _goal.Bottom + 12;
        if (_planner.Visible)
        {
            _planner.SetBounds(margin, y, width, 134);
            y = _planner.Bottom + 12;
        }
        _table.SetBounds(margin, y, width, _table.WantedHeight);
        _note.SetBounds(margin, _table.Bottom + 12, width, 130);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clock.Stop();
            _clock.Dispose();
            _data.Stop();
            _data.Dispose();
            _anim.Stop();
            _anim.Dispose();
            _sparkle.Stop();
            _sparkle.Dispose();
            foreach (var image in _images.Values)
            {
                image.Dispose();
            }
            _images.Clear();
        }
        base.Dispose(disposing);
    }
}
