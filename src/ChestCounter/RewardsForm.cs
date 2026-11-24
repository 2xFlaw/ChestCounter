using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Weekly rewards: every Treasure Hunt week as a row of five pictures plus the Pirate Shop Token. Items not found
/// yet are greyed out; click one when you find it and it lights up. Ownership cannot be read from replays, so the
/// player ticks it. Ticks are saved in config.json (same keys as Weekly chances) and, when sync is on, shared with
/// chestcounter.io.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RewardsForm : Form
{
    private readonly Monitor _monitor;
    private readonly Store _store;
    private readonly HuntBoard _board;
    private readonly Panel _scroll;
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };

    public RewardsForm(Monitor monitor, Store store)
    {
        _monitor = monitor;
        _store = store;
        if (WeeklyReset.MigratePirateOwnership(monitor.Config, DateTime.UtcNow)) store.SaveConfig(monitor.Config);
        Text = AppText.T("Weekly rewards");
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(880, 780);
        MinimumSize = new Size(600, 480);
        ShowInTaskbar = false;
        Icon = AppIcon.Load();
        KeyPreview = true;

        _board = new HuntBoard(store, TreasureHunt.Load(store), IsOwned, SetOwned) { Dock = DockStyle.Top };
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Bg };
        _scroll.Controls.Add(_board);
        Controls.Add(_scroll);
        _scroll.Resize += (_, _) => FitBoard();
        _board.SyncText = TreasureHunt.CanSync ? null : HuntText.T("Saved on this PC. Turn on sync in Settings to share with chestcounter.io.");

        _clock.Tick += (_, _) => _board.Tick();
        _clock.Start();
        Shown += async (_, _) =>
        {
            FitBoard();
            await RefreshFromWebsite();
        };
    }

    private void FitBoard()
    {
        // the board is docked to the top, so its width always follows the panel and only the
        // vertical scroll bar can appear; work out the height for the width it will end up with
        var full = _scroll.Width;
        if (full <= 0)
        {
            return;
        }
        var width = full;
        var height = _board.Measure(width);
        if (height > _scroll.Height)
        {
            width = full - SystemInformation.VerticalScrollBarWidth;
            height = _board.Measure(width);
        }
        _scroll.AutoScrollMinSize = new Size(0, height);
        _board.Height = height;
    }

    private bool IsOwned(string key) => _monitor.Config.OwnedRewards.TryGetValue(key, out var v) && v;

    private void SetOwned(string key, bool value)
    {
        _monitor.Change(c => c.OwnedRewards[key] = value);
        TreasureHunt.Remember(_store, key, value);
        _ = PushNow();
    }

    private bool _syncing;
    private bool _again;

    private async Task PushNow()
    {
        if (_syncing)
        {
            _again = true;
            return;
        }
        _syncing = true;
        try
        {
            do
            {
                _again = false;
                await SyncOnce(applyServer: false);
            }
            while (_again && !IsDisposed);
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task SyncOnce(bool applyServer)
    {
        if (!TreasureHunt.CanSync)
        {
            return;
        }
        var local = new Dictionary<string, bool>(_monitor.Config.OwnedRewards);
        var server = await Task.Run(() => TreasureHunt.Sync(_store, local));
        if (IsDisposed)
        {
            return;
        }
        if (server == null)
        {
            _board.SyncText = HuntText.T("Could not reach chestcounter.io. Saved on this PC and sent next time.");
            return;
        }
        _board.SyncText = HuntText.T("Synced with chestcounter.io");
        if (!applyServer)
        {
            return;
        }
        var changed = server.Where(p => TreasureHunt.IsSyncKey(p.Key) && IsOwned(p.Key) != p.Value).ToList();
        if (changed.Count > 0)
        {
            _monitor.Change(c =>
            {
                foreach (var (key, value) in changed)
                {
                    c.OwnedRewards[key] = value;
                }
            });
            _board.Reload(animate: true);
        }
    }

    private async Task RefreshFromWebsite()
    {
        var weeks = await TreasureHunt.Refresh(_store, _board.Weeks);
        if (IsDisposed)
        {
            return;
        }
        if (weeks != null)
        {
            _board.SetWeeks(weeks);
            FitBoard();
        }
        if (!_syncing)
        {
            _syncing = true;
            try
            {
                await SyncOnce(applyServer: true);
            }
            finally
            {
                _syncing = false;
            }
            if (_again && !IsDisposed)
            {
                await PushNow();
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
        base.OnKeyDown(e);
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
        }
        base.Dispose(disposing);
    }
}

/// <summary>The painted grid: header with the found counter, the Pirate Shop Token banner, then one row per week.</summary>
[SupportedOSPlatform("windows")]
internal sealed class HuntBoard : Control
{
    private static readonly Color Gold = Color.FromArgb(246, 190, 70);
    private static readonly Color TileBack = Color.FromArgb(31, 22, 37);
    private static readonly Color LockedEdge = Color.FromArgb(66, 52, 72);

    private static Color SlotColour(string slot) => slot switch
    {
        "gold-outfit" => Color.FromArgb(214, 170, 72),
        "vehicle-decal" => Color.FromArgb(236, 128, 52),
        "pickaxe-back-bling" => Color.FromArgb(72, 150, 236),
        "jam-track" => Color.FromArgb(236, 84, 170),
        _ => Color.FromArgb(52, 206, 178),
    };

    private sealed class Tile
    {
        public required HuntWeek Week;
        public required HuntItem Item;
        public required string Key;
        public RectangleF Box;
        public bool Owned;
        public bool Future;
        public double Shown;      // 0 = grey, 1 = colour
        public double Pop = 1;    // 0..1 progress of the unlock burst (1 = finished)
        public Bitmap? Colour;
        public Bitmap? Grey;
        public int Px;
    }

    private readonly Store _store;
    private readonly Func<string, bool> _isOwned;
    private readonly Action<string, bool> _setOwned;
    private readonly List<Tile> _tiles = new();
    private readonly System.Windows.Forms.Timer _anim = new() { Interval = 15 };
    private readonly ToolTip _tip = new() { InitialDelay = 350, ReshowDelay = 100 };
    private DateTime _last = DateTime.UtcNow;

    private readonly Font _eyebrow = Fonts.Body(9f, FontStyle.Bold);
    private readonly Font _title = Fonts.Display(30f);
    private readonly Font _countBig = Fonts.Display(40f);
    private readonly Font _small = Theme.Text(9f);
    private readonly Font _smallBold = Theme.Text(9f, FontStyle.Bold);
    private readonly Font _weekFont = Fonts.Display(19f);
    private readonly Font _name = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _bannerTitle = Theme.Text(12.5f, FontStyle.Bold);
    private readonly Font _bannerState = Fonts.Display(17f);
    private readonly Font _chip = Fonts.Body(7.5f, FontStyle.Bold);

    private Bitmap? _pirate;
    private Bitmap? _pirateGrey;
    private int _piratePx;
    private RectangleF _banner;
    private bool _pirateOwned;
    private double _pirateShown;
    private double _piratePop = 1;
    private int _hover = -2;          // -1 = banner, -2 = nothing
    private string? _syncText;
    private float _footerY;

    public IReadOnlyList<HuntWeek> Weeks { get; private set; }

    public string? SyncText
    {
        get => _syncText;
        set
        {
            if (_syncText != value)
            {
                _syncText = value;
                Invalidate();
            }
        }
    }

    public HuntBoard(Store store, IReadOnlyList<HuntWeek> weeks, Func<string, bool> isOwned, Action<string, bool> setOwned)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        _store = store;
        _isOwned = isOwned;
        _setOwned = setOwned;
        Weeks = weeks;
        _anim.Tick += (_, _) => Animate();
        Build();
    }

    private float S => DeviceDpi / 96f;

    private void Build()
    {
        foreach (var t in _tiles)
        {
            t.Colour?.Dispose();
            t.Grey?.Dispose();
        }
        _tiles.Clear();
        var now = DateTime.UtcNow;
        foreach (var week in Weeks)
        {
            foreach (var item in week.Items)
            {
                var key = TreasureHunt.OwnedKey(week, item.Slot);
                var owned = _isOwned(key);
                _tiles.Add(new Tile { Week = week, Item = item, Key = key, Owned = owned, Shown = owned ? 1 : 0, Future = week.StartUtc > now });
            }
        }
        _pirateOwned = _isOwned(TreasureHunt.PirateKey);
        _pirateShown = _pirateOwned ? 1 : 0;
        Invalidate();
    }

    public void SetWeeks(IReadOnlyList<HuntWeek> weeks)
    {
        Weeks = weeks;
        Build();
    }

    /// <summary>Reads the ticks again (after a sync); items that changed animate.</summary>
    public void Reload(bool animate)
    {
        foreach (var t in _tiles)
        {
            var owned = _isOwned(t.Key);
            if (owned != t.Owned)
            {
                t.Owned = owned;
                if (animate && owned)
                {
                    t.Pop = 0;
                }
                else if (!animate)
                {
                    t.Shown = owned ? 1 : 0;
                }
            }
        }
        var pirate = _isOwned(TreasureHunt.PirateKey);
        if (pirate != _pirateOwned)
        {
            _pirateOwned = pirate;
            if (pirate && animate)
            {
                _piratePop = 0;
            }
        }
        StartAnimation();
        Invalidate();
    }

    /// <summary>Once a second: the countdown, and a new week that started while the window is open.</summary>
    public void Tick()
    {
        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var t in _tiles)
        {
            var future = t.Week.StartUtc > now;
            if (future != t.Future)
            {
                t.Future = future;
                changed = true;
            }
        }
        _last = now;
        Invalidate(new Rectangle(0, 0, Width, (int)(_banner.Top > 0 ? _banner.Top : 120 * S)));
        if (changed)
        {
            Invalidate();
        }
    }

    // ---- layout ------------------------------------------------------------------------------------------------

    private float Pad => 28 * S;
    private float LabelWidth(float width) => (width < 700 * S ? 118 : 150) * S;
    private float Gap => 14 * S;

    private float TileSize(int width)
    {
        var avail = width - Pad * 2 - LabelWidth(width) - Gap * 4;
        return Math.Clamp(avail / 5f, 72 * S, 136 * S);
    }

    private float RowHeight(float tile) => tile + 52 * S;
    private float BannerTop => 128 * S;
    private float BannerHeight => 76 * S;
    private float RowsTop => BannerTop + BannerHeight + 26 * S;

    /// <summary>Height for a given width (and lays out the tiles).</summary>
    public int Measure(int width)
    {
        if (Weeks == null)
        {
            return 0;
        }
        var tile = TileSize(width);
        var left = Pad + LabelWidth(width);
        var y = RowsTop;
        _banner = new RectangleF(Pad, BannerTop, width - Pad * 2, BannerHeight);
        foreach (var week in Weeks)
        {
            var row = _tiles.Where(t => t.Week == week).ToList();
            for (var i = 0; i < row.Count; i++)
            {
                row[i].Box = new RectangleF(left + i * (tile + Gap), y, tile, tile);
            }
            y += RowHeight(tile);
        }
        _footerY = y + 2 * S;
        return (int)Math.Ceiling(y + 70 * S);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Measure(Width);
    }

    // ---- pictures ----------------------------------------------------------------------------------------------

    private void EnsurePictures(Tile t)
    {
        var px = (int)Math.Ceiling(t.Box.Width);
        if (t.Colour != null && t.Px == px)
        {
            return;
        }
        t.Colour?.Dispose();
        t.Grey?.Dispose();
        t.Colour = TreasureHunt.Picture(_store, t.Item.Image, px);
        t.Grey = t.Colour != null ? TreasureHunt.Locked(t.Colour) : null;
        t.Px = px;
    }

    private void EnsurePirate(int px)
    {
        if (_pirate != null && _piratePx == px)
        {
            return;
        }
        _pirate?.Dispose();
        _pirateGrey?.Dispose();
        _pirate = TreasureHunt.Picture(_store, TreasureHunt.PirateImage, px);
        _pirateGrey = _pirate != null ? TreasureHunt.Locked(_pirate) : null;
        _piratePx = px;
    }

    private static void DrawFaded(Graphics g, Bitmap image, RectangleF dest, float alpha)
    {
        if (alpha >= 0.999f)
        {
            g.DrawImage(image, dest);
            return;
        }
        if (alpha <= 0.001f)
        {
            return;
        }
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = alpha });
        g.DrawImage(image, Rectangle.Round(dest), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
    }

    // ---- animation ---------------------------------------------------------------------------------------------

    private const double UnlockSeconds = 0.7;
    private const double LockSeconds = 0.25;
    private DateTime _frame = DateTime.UtcNow;

    private void StartAnimation()
    {
        if (!_anim.Enabled)
        {
            _frame = DateTime.UtcNow;
            _anim.Start();
        }
    }

    private void Animate()
    {
        var now = DateTime.UtcNow;
        var dt = Math.Clamp((now - _frame).TotalSeconds, 0, 0.1);
        _frame = now;
        var busy = false;
        foreach (var t in _tiles)
        {
            busy |= Step(ref t.Shown, ref t.Pop, t.Owned, dt);
        }
        busy |= Step(ref _pirateShown, ref _piratePop, _pirateOwned, dt);
        Invalidate();
        if (!busy)
        {
            _anim.Stop();
        }
    }

    private static bool Step(ref double shown, ref double pop, bool owned, double dt)
    {
        var busy = false;
        var target = owned ? 1.0 : 0.0;
        if (Math.Abs(shown - target) > 0.0001)
        {
            var speed = owned ? 1 / (UnlockSeconds * 0.6) : 1 / LockSeconds;
            shown = owned ? Math.Min(1, shown + dt * speed) : Math.Max(0, shown - dt * speed);
            busy = true;
        }
        if (pop < 1)
        {
            pop = Math.Min(1, pop + dt / UnlockSeconds);
            busy = true;
        }
        return busy;
    }

    private static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);

    // ---- input -------------------------------------------------------------------------------------------------

    private int HitTest(Point p)
    {
        for (var i = 0; i < _tiles.Count; i++)
        {
            var b = _tiles[i].Box;
            var withName = new RectangleF(b.X, b.Y, b.Width, b.Height + 40 * S);
            if (withName.Contains(p))
            {
                return i;
            }
        }
        return _banner.Contains(p) ? -1 : -2;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hit = HitTest(e.Location);
        if (hit == _hover)
        {
            return;
        }
        _hover = hit;
        Cursor = hit == -2 || (hit >= 0 && _tiles[hit].Future) ? Cursors.Default : Cursors.Hand;
        var tip = hit switch
        {
            -2 => null,
            -1 => HuntText.T(_pirateOwned ? "Click to mark as not found" : "Click to mark as found"),
            _ when _tiles[hit].Future => HuntText.T("Not available yet"),
            _ => _tiles[hit].Item.Name + "\n" + HuntText.T(_tiles[hit].Owned ? "Click to mark as not found" : "Click to mark as found"),
        };
        _tip.SetToolTip(this, tip);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = -2;
        _tip.SetToolTip(this, null);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }
        var hit = HitTest(e.Location);
        if (hit == -1)
        {
            _pirateOwned = !_pirateOwned;
            _piratePop = _pirateOwned ? 0 : 1;
            _setOwned(TreasureHunt.PirateKey, _pirateOwned);
        }
        else if (hit >= 0 && !_tiles[hit].Future)
        {
            var t = _tiles[hit];
            t.Owned = !t.Owned;
            t.Pop = t.Owned ? 0 : 1;
            _setOwned(t.Key, t.Owned);
        }
        else
        {
            return;
        }
        _hover = -3; // refresh the tooltip text on the next move
        OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, e.X, e.Y, 0));
        StartAnimation();
    }

    // ---- painting ----------------------------------------------------------------------------------------------

    private static void Text(Graphics g, string text, Font font, Color colour, RectangleF box, TextFormatFlags flags) =>
        TextRenderer.DrawText(g, text, font, Rectangle.Round(box), colour, flags | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

    private static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var s = S;
        var now = DateTime.UtcNow;

        // header
        var found = _tiles.Count(t => t.Owned);
        Text(g, HuntText.T("TREASURE HUNT"), _eyebrow, Gold, new RectangleF(Pad, 22 * s, 400 * s, 18 * s), TextFormatFlags.Left);
        Text(g, AppText.T("Weekly rewards"), _title, Theme.Ink, new RectangleF(Pad, 40 * s, Width - Pad * 2 - 200 * s, 52 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        var current = Weeks.LastOrDefault(w => w.StartUtc <= now && now < w.EndUtc);
        var left = WeeklyReset.Remaining(now);
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }
        var reset = ToolText.F("Resets in {0}d {1:00}h {2:00}m {3:00}s", left.Days, left.Hours, left.Minutes, left.Seconds);
        var line = current != null ? HuntText.F("Week {0} ends {1:ddd d MMM}", current.Number, current.EndUtc.ToLocalTime()) + "   \u00b7   " + reset : reset;
        Text(g, line, _smallBold, Theme.Muted, new RectangleF(Pad, 96 * s, Width - Pad * 2 - 200 * s, 20 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        var total = _tiles.Count;
        var right = new RectangleF(Width - Pad - 220 * s, 24 * s, 220 * s, 18 * s);
        Text(g, HuntText.T("FOUND"), _eyebrow, Theme.Muted, right, TextFormatFlags.Right);
        var totalText = " / " + total.ToString(L.Culture);
        var totalSize = TextRenderer.MeasureText(g, totalText, _countBig, Size.Empty, TextFormatFlags.NoPadding);
        var countBox = new RectangleF(Width - Pad - 300 * s, 42 * s, 300 * s, 60 * s);
        Text(g, totalText, _countBig, Theme.Ink, countBox, TextFormatFlags.Right);
        Text(g, found.ToString(L.Culture), _countBig, Theme.Mint, new RectangleF(countBox.X, countBox.Y, countBox.Width - totalSize.Width, countBox.Height), TextFormatFlags.Right);

        PaintBanner(g, s);

        // rows
        foreach (var week in Weeks)
        {
            var row = _tiles.Where(t => t.Week == week).ToList();
            if (row.Count == 0)
            {
                continue;
            }
            var top = row[0].Box.Y;
            var isCurrent = week == current;
            var future = week.StartUtc > now;
            var labelColour = isCurrent ? Gold : future ? Theme.Muted : Theme.Ink;
            var labelTop = top + row[0].Box.Height / 2 - 30 * s;
            Text(g, HuntText.F("WEEK {0}", week.Number), _weekFont, labelColour, new RectangleF(Pad, labelTop, LabelWidth(Width) - 8 * s, 36 * s), TextFormatFlags.Left);
            var sub = future ? HuntText.F("Starts {0:ddd d MMM}", week.StartUtc.ToLocalTime()) : HuntText.F("{0}/{1} found", row.Count(t => t.Owned), row.Count);
            Text(g, sub, _smallBold, Theme.Muted, new RectangleF(Pad, labelTop + 36 * s, LabelWidth(Width) - 8 * s, 18 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            if (isCurrent)
            {
                var chip = HuntText.T("THIS WEEK");
                var size = TextRenderer.MeasureText(g, chip, _chip, Size.Empty, TextFormatFlags.NoPadding);
                var r = new RectangleF(Pad, labelTop + 58 * s, size.Width + 14 * s, 18 * s);
                using (var path = Shape.Rounded(r, 9 * s))
                using (var fill = new SolidBrush(Color.FromArgb(46, Gold)))
                using (var pen = new Pen(Color.FromArgb(150, Gold), 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                Text(g, chip, _chip, Gold, r, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            foreach (var t in row)
            {
                PaintTile(g, t, s);
            }
        }

        // footer
        var hint = HuntText.T("Click an item when you find it. This week's found items are hidden from Weekly chances.");
        using (var pen = new Pen(Theme.Line, 1f))
        {
            g.DrawLine(pen, Pad, _footerY, Width - Pad, _footerY);
        }
        Text(g, hint, _small, Theme.Muted, new RectangleF(Pad, _footerY + 12 * s, Width - Pad * 2, 20 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        if (!string.IsNullOrEmpty(_syncText))
        {
            Text(g, _syncText, _small, Theme.Muted, new RectangleF(Pad, _footerY + 34 * s, Width - Pad * 2, 20 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    private void PaintBanner(Graphics g, float s)
    {
        var r = _banner;
        var hover = _hover == -1;
        var shown = (float)EaseOut(_pirateShown);
        using (var path = Shape.Rounded(r, 14 * s))
        {
            using var fill = new LinearGradientBrush(r, Color.FromArgb((int)(18 + 34 * shown), Gold), Color.FromArgb(10, Gold), LinearGradientMode.Horizontal);
            using var back = new SolidBrush(Theme.Card);
            g.FillPath(back, path);
            g.FillPath(fill, path);
            using var pen = new Pen(Mix(Color.FromArgb(120, Gold), Gold, hover ? 1 : shown), (hover ? 2f : 1.5f) * s);
            g.DrawPath(pen, path);
        }
        if (_piratePop < 1)
        {
            PaintBurst(g, r, Gold, _piratePop, 14 * s, s);
        }

        var icon = 52 * s;
        var iconBox = new RectangleF(r.X + 14 * s, r.Y + (r.Height - icon) / 2, icon, icon);
        var pop = _piratePop < 1 ? 1 + 0.12 * Math.Sin(Math.PI * _piratePop) : 1;
        var drawn = Grow(iconBox, (float)pop);
        EnsurePirate((int)Math.Ceiling(icon * 1.2f));
        if (_pirateGrey != null && _pirate != null)
        {
            g.DrawImage(_pirateGrey, drawn);
            DrawFaded(g, _pirate, drawn, shown);
        }

        var textLeft = iconBox.Right + 14 * s;
        var stateText = HuntText.T(_pirateOwned ? "FOUND IT" : "NOT FOUND");
        var stateSize = TextRenderer.MeasureText(g, stateText, _bannerState, Size.Empty, TextFormatFlags.NoPadding);
        var textWidth = r.Right - 20 * s - stateSize.Width - 16 * s - textLeft;
        Text(g, HuntText.T("PIRATE SHOP TOKEN"), _bannerTitle, Mix(Theme.Muted, Gold, Math.Max(shown, hover ? 0.7 : 0.45)), new RectangleF(textLeft, r.Y + 17 * s, textWidth, 24 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        Text(g, HuntText.T("14 days of free item shops - found any week"), _small, Theme.Muted, new RectangleF(textLeft, r.Y + 43 * s, textWidth, 20 * s), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        Text(g, stateText, _bannerState, _pirateOwned ? Theme.Mint : Theme.Muted, new RectangleF(r.Right - 20 * s - stateSize.Width, r.Y, stateSize.Width + 2, r.Height), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private static RectangleF Grow(RectangleF r, float scale)
    {
        var w = r.Width * scale;
        var h = r.Height * scale;
        return new RectangleF(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
    }

    /// <summary>The unlock burst: an expanding glow ring and sparks flying out.</summary>
    private static void PaintBurst(Graphics g, RectangleF box, Color colour, double progress, float radius, float s)
    {
        var t = (float)EaseOut(progress);
        var ring = RectangleF.Inflate(box, 4 * s + 16 * s * t, 4 * s + 16 * s * t);
        using (var path = Shape.Rounded(ring, radius + 10 * s * t))
        using (var pen = new Pen(Color.FromArgb((int)(220 * (1 - progress)), colour), (4 - 3 * t) * s))
        {
            g.DrawPath(pen, path);
        }
        var cx = box.X + box.Width / 2;
        var cy = box.Y + box.Height / 2;
        var reach = Math.Max(box.Width, box.Height) * 0.5f + 26 * s * t;
        using var spark = new SolidBrush(Color.FromArgb((int)(255 * (1 - progress)), Color.FromArgb(255, 240, 200)));
        for (var i = 0; i < 10; i++)
        {
            var a = i * Math.PI * 2 / 10 + 0.3;
            var d = reach * (0.75f + 0.25f * (i % 3) / 2f);
            var x = cx + (float)Math.Cos(a) * d;
            var y = cy + (float)Math.Sin(a) * d;
            var size = (3.2f - 2f * t) * s;
            g.FillEllipse(spark, x - size, y - size, size * 2, size * 2);
        }
    }

    private void PaintTile(Graphics g, Tile t, float s)
    {
        EnsurePictures(t);
        var hover = _tiles.IndexOf(t) == _hover && !t.Future;
        var shown = (float)EaseOut(t.Shown);
        var pop = t.Pop < 1 ? 1 + 0.08 * Math.Sin(Math.PI * t.Pop) : 1;
        var box = Grow(t.Box, (float)pop);
        if (hover)
        {
            box.Offset(0, -2 * s);
        }
        var radius = 12 * s;
        var colour = SlotColour(t.Item.Slot);

        if (t.Pop < 1)
        {
            PaintBurst(g, box, colour, t.Pop, radius, s);
        }
        using (var path = Shape.Rounded(box, radius))
        {
            using (var back = new SolidBrush(TileBack))
            {
                g.FillPath(back, path);
            }
            var state = g.Save();
            g.SetClip(path, CombineMode.Intersect);
            if (t.Colour != null && t.Grey != null)
            {
                if (shown < 0.999f)
                {
                    if (t.Future)
                    {
                        DrawFaded(g, t.Grey, box, 0.45f);
                    }
                    else
                    {
                        g.DrawImage(t.Grey, box);
                    }
                }
                DrawFaded(g, t.Colour, box, shown);
            }
            else
            {
                Text(g, "\u2605", _countBig, LockedEdge, box, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            g.Restore(state);

            var edge = t.Future ? Color.FromArgb(46, 38, 52) : Mix(Mix(LockedEdge, colour, hover ? 0.55 : 0.28), colour, shown);
            using var pen = new Pen(edge, (hover || shown > 0.5f ? 2.2f : 1.6f) * s);
            g.DrawPath(pen, path);
        }

        // badge: tick when found, padlock when locked
        var badge = 22 * s;
        var badgeBox = new RectangleF(box.Right - badge - 6 * s, box.Y + 6 * s, badge, badge);
        if (t.Owned)
        {
            var grow = t.Pop < 1 ? (float)Math.Clamp(EaseOutBack(Math.Clamp((t.Pop - 0.25) / 0.6, 0, 1)), 0, 1.4) : 1f;
            if (grow > 0.01f)
            {
                var b = Grow(badgeBox, grow);
                using var fill = new SolidBrush(Theme.Mint);
                g.FillEllipse(fill, b);
                using var tick = new Pen(Color.FromArgb(10, 40, 25), 2.4f * s * grow) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(tick, new[]
                {
                    new PointF(b.X + b.Width * 0.27f, b.Y + b.Height * 0.53f),
                    new PointF(b.X + b.Width * 0.44f, b.Y + b.Height * 0.69f),
                    new PointF(b.X + b.Width * 0.74f, b.Y + b.Height * 0.35f),
                });
            }
        }
        else if (shown < 0.5f)
        {
            using var fill = new SolidBrush(Color.FromArgb(190, 14, 10, 18));
            g.FillEllipse(fill, badgeBox);
            PaintLock(g, badgeBox, t.Future ? Color.FromArgb(120, Theme.Muted) : Theme.Muted, s);
        }

        // name
        var nameBox = new RectangleF(t.Box.X - 6 * s, t.Box.Bottom + 7 * s, t.Box.Width + 12 * s, 34 * s);
        var nameColour = t.Future ? Color.FromArgb(120, Theme.Muted) : Mix(Theme.Muted, Theme.Ink, Math.Max(shown, hover ? 0.6 : 0));
        Text(g, t.Item.Name, _name, nameColour, nameBox, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }

    private static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }

    private static void PaintLock(Graphics g, RectangleF r, Color colour, float s)
    {
        var w = r.Width * 0.42f;
        var h = r.Height * 0.30f;
        var body = new RectangleF(r.X + (r.Width - w) / 2, r.Y + r.Height * 0.47f, w, h);
        using (var path = Shape.Rounded(body, 2f * s))
        using (var fill = new SolidBrush(colour))
        {
            g.FillPath(fill, path);
        }
        var sw = w * 0.62f;
        var shackle = new RectangleF(r.X + (r.Width - sw) / 2, r.Y + r.Height * 0.24f, sw, r.Height * 0.40f);
        using var pen = new Pen(colour, 1.6f * s);
        g.DrawArc(pen, shackle, 180, 180);
        g.DrawLine(pen, shackle.X, shackle.Y + shackle.Height / 2, shackle.X, body.Y);
        g.DrawLine(pen, shackle.Right, shackle.Y + shackle.Height / 2, shackle.Right, body.Y);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _anim.Stop();
            _anim.Dispose();
            _tip.Dispose();
            foreach (var t in _tiles)
            {
                t.Colour?.Dispose();
                t.Grey?.Dispose();
            }
            _pirate?.Dispose();
            _pirateGrey?.Dispose();
            foreach (var f in new[] { _eyebrow, _title, _countBig, _small, _smallBold, _weekFont, _name, _bannerTitle, _bannerState, _chip })
            {
                f.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}
