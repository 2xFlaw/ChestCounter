using System.Drawing.Drawing2D;

namespace ChestCounter;

/// <summary>
/// Dashboard chest statistics: Just you / With teammates switch, five tiles ranked against other players
/// (red = lower ranking, green = top ranking) and a chests-per-match (or per-day) chart with hover details.
/// Reads the saved history of the shown profile; never changes it.
/// </summary>
internal sealed class ChestStatsPanel : Control
{
    private sealed record Row(DateTime When, int Opened, int Broken, int Mates, int? Seconds);
    private sealed record Data(List<Row> Rows);
    private sealed record Bar(DateTime When, int You, int Broken, int Mates)
    {
        public int Total => You + Mates;
    }

    private readonly Monitor _monitor;
    private readonly Store _store;
    private Data _data = new(new List<Row>());
    private RankResult? _rank;
    private bool _withMates;
    private bool _modeChosen;
    private bool _perDay;
    private string _signature = "";
    private int _loading;
    private int _hover = -1;
    private readonly List<(Rectangle Box, Action Click)> _hits = new();
    private readonly List<(float X, float Width)> _columns = new();
    private Rectangle _plot;
    private readonly Font _tileValue = Fonts.Display(24f);
    private readonly Font _tileLabel = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _small = Theme.Text(8.5f);
    private readonly Font _smallBold = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _pill = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _title = Fonts.Display(14f);
    private readonly Font _icon = new("Segoe UI Emoji", 10f);
    private bool HasEmojiFont => string.Equals(_icon.Name, "Segoe UI Emoji", StringComparison.OrdinalIgnoreCase);

    // Used when there is no ranking yet (rankings off, offline or not enough players).
    private static readonly Color[] TileColours =
    {
        Theme.Accent, Color.FromArgb(176, 92, 160), Theme.AccentSoft, Theme.Amber, Theme.Mint,
    };
    // Monochrome emoji, drawn in grey next to each tile title.
    private static readonly string[] Icons = { "\U0001F464", "\U0001F465", "\U0001F3AF", "\U0001F3C6", "\u23F1" };
    private static readonly Color IconGrey = Color.FromArgb(128, 112, 124);
    private static readonly Color YouBar = Theme.Accent;
    private static readonly Color BrokenBar = Theme.AccentSoft;
    private static readonly Color MateBar = Color.FromArgb(120, 72, 112);

    public ChestStatsPanel(Monitor monitor, Store store)
    {
        _monitor = monitor;
        _store = store;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Dock = DockStyle.Top;
        Height = 560;
        Margin = new Padding(0, 12, 0, 12);
        Cursor = Cursors.Default;
        try { _rank = Rankings.Cached(_store.Folder); } catch (Exception ex) { ErrorLog.Write("Player rankings", ex); }
    }

    private float S => DeviceDpi / 96f;
    private int P(float v) => (int)Math.Round(v * S);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Height = P(560);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Height = P(560);
    }

    /// <summary>Reloads the history in the background when the profile, settings or match count changed.</summary>
    public void Refresh(MonitorState s)
    {
        var c = _monitor.Config;
        var sig = $"{s.CurrentKey}|{s.Stats.HistoryMatches}|{s.Stats.LastMatch?.Chests}|{c.CountTeammateChests}|{string.Join(",", c.ExtraKinds())}|{c.CountFrom:O}";
        if (sig == _signature)
        {
            // Same history: only ask for fresh rankings now and then.
            if (_data.Rows.Count > 0) Rank(_data);
            return;
        }
        if (Interlocked.Exchange(ref _loading, 1) == 1) return;
        _signature = sig;
        Task.Run(() =>
        {
            try
            {
                var kinds = c.ExtraKinds();
                var rows = Profiles.View(_store.LoadData(), c).Matches
                    .Where(m => m.IsBattleRoyale)
                    .OrderBy(m => m.MatchStartUtc ?? m.AnalyzedAtUtc)
                    .Select(m =>
                    {
                        var own = Math.Max(0, m.OwnChests);
                        var broken = Math.Min(own, m.Chests.Count(x => x.Destroyed));
                        return new Row((m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime(), own - broken, broken,
                            Math.Max(0, m.TeammatesSeen(kinds)), m.DurationSeconds is > 0 ? m.DurationSeconds : null);
                    }).ToList();
                return new Data(rows);
            }
            catch (Exception ex) { ErrorLog.Write("Dashboard chest statistics", ex); return null; }
        }).ContinueWith(t =>
        {
            Interlocked.Exchange(ref _loading, 0);
            if (t.Result is not { } d) return;
            OnUi(() =>
            {
                _data = d;
                if (!_modeChosen) _withMates = d.Rows.Any(r => r.Mates > 0);
                _hover = -1;
                Invalidate();
                Rank(d);
            });
        });
    }

    private void OnUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); } catch (InvalidOperationException) { }
    }

    // ---- Rankings -------------------------------------------------------------------------------------

    private static RankFigures Figures(List<Row> rows)
    {
        int You(Row r) => r.Opened + r.Broken;
        var timed = rows.Where(r => r.Seconds != null).ToList();
        double minutes = timed.Sum(r => r.Seconds!.Value) / 60.0;
        return new RankFigures(
            rows.Count,
            rows.Sum(You),
            rows.Sum(r => r.Mates),
            rows.Count > 0 ? rows.Average(You) : 0,
            rows.Count > 0 ? rows.Average(r => You(r) + r.Mates) : 0,
            rows.Count > 0 ? rows.Max(You) : 0,
            rows.Count > 0 ? rows.Max(r => You(r) + r.Mates) : 0,
            minutes > 0 ? timed.Sum(You) / minutes : null,
            minutes > 0 ? timed.Sum(r => You(r) + r.Mates) / minutes : null);
    }

    private void Rank(Data d)
    {
        var folder = _store.Folder;
        var figures = Figures(d.Rows);
        if (!Rankings.Due(folder, figures)) return;
        Rankings.Fetch(folder, figures).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion && t.Result is { } r) OnUi(() => { _rank = r; Invalidate(); });
        });
    }

    private void ToggleRankings()
    {
        var on = !Rankings.Enabled(_store.Folder);
        Rankings.SetEnabled(_store.Folder, on);
        _rank = null;
        if (on && _data.Rows.Count > 0) Rank(_data);
    }

    /// <summary>"Top X%" for a figure, when there are at least two players to compare with.</summary>
    private (int Top, int Players)? RankOf(string key)
    {
        if (_rank == null || !_rank.Top.TryGetValue(key, out var top)) return null;
        _rank.Players.TryGetValue(key, out var players);
        return players >= 2 ? (Math.Clamp(top, 1, 100), players) : null;
    }

    /// <summary>Top 1% is green, top 100% is red; everything in between follows the hue wheel through yellow.</summary>
    private static Color RankColour(int top)
    {
        double hue = 120.0 * (100 - Math.Clamp(top, 1, 100)) / 99.0;
        return FromHsl(hue, 0.78, 0.55);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        var (r, g, b) = h < 60 ? (c, x, 0d) : h < 120 ? (x, c, 0d) : (0d, c, x);
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }

    // ---- Input ----------------------------------------------------------------------------------------

    private int Total(Row r) => r.Opened + r.Broken + (_withMates ? r.Mates : 0);

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var (box, click) in _hits)
            if (box.Contains(e.Location)) { click(); _hover = -1; Invalidate(); return; }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hand = _hits.Any(h => h.Box.Contains(e.Location));
        if ((Cursor == Cursors.Hand) != hand) Cursor = hand ? Cursors.Hand : Cursors.Default;
        int hover = -1;
        if (_plot.Contains(e.Location))
            for (int i = 0; i < _columns.Count; i++)
                if (e.X >= _columns[i].X && e.X < _columns[i].X + _columns[i].Width) { hover = i; break; }
        if (hover != _hover) { _hover = hover; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hover != -1) { _hover = -1; Invalidate(); }
    }

    // ---- Drawing helpers ------------------------------------------------------------------------------

    private static GraphicsPath Round(Rectangle r, int radius) => Round((RectangleF)r, radius, true);

    private static GraphicsPath Round(RectangleF r, float radius, bool bottom)
    {
        var p = new GraphicsPath();
        float d = Math.Max(1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        if (bottom)
        {
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        }
        else
        {
            p.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
        }
        p.CloseFigure();
        return p;
    }

    private static new void Text(Graphics g, string text, Font font, Rectangle box, Color colour, TextFormatFlags extra = 0) =>
        TextRenderer.DrawText(g, text, font, box, colour, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | extra);

    private static int Measure(Graphics g, string text, Font font) =>
        TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding).Width;

    // ---- Letter-spaced capitals ----------------------------------------------------------------------------
    // Drawn one character at a time with explicit letter and word gaps, so they never depend on the font having
    // hair or en spaces (the embedded display font draws neither, which is how "CHESTSPERMATCH" happened).

    private static readonly Dictionary<(Font, char), int> CharWidths = new();

    private static int CharWidth(Graphics g, Font font, char c)
    {
        if (!CharWidths.TryGetValue((font, c), out var w))
        {
            w = TextRenderer.MeasureText(g, c.ToString(), font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            CharWidths[(font, c)] = w;
        }
        return w;
    }

    private static float Em(Graphics g, Font font) => font.SizeInPoints * g.DpiY / 72f;

    private static int LetterGap(Graphics g, Font font) => Math.Max(1, (int)Math.Round(Em(g, font) * 0.09f));

    private static int WordGap(Graphics g, Font font) => Math.Max(3, (int)Math.Round(Em(g, font) * 0.42f));

    private static string Upper(string text) => text.ToUpper(L.Culture).Trim();

    private static int CapsWidthRaw(Graphics g, string s, Font font)
    {
        int w = 0, letter = LetterGap(g, font), word = WordGap(g, font);
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsWhiteSpace(s[i])) { w += word; continue; }
            w += CharWidth(g, font, s[i]);
            if (i < s.Length - 1 && !char.IsWhiteSpace(s[i + 1])) w += letter;
        }
        return w;
    }

    private static int CapsWidth(Graphics g, string text, Font font) => CapsWidthRaw(g, Upper(text), font);

    /// <summary>Draws an upper-case, letter-spaced label inside the box (vertically centred, shortened with … if needed).</summary>
    private static void Caps(Graphics g, string text, Font font, Rectangle box, Color colour)
    {
        var s = Upper(text);
        if (CapsWidthRaw(g, s, font) > box.Width)
        {
            while (s.Length > 1 && CapsWidthRaw(g, s + "…", font) > box.Width) s = s[..^1].TrimEnd();
            s += "…";
        }
        int x = box.X, letter = LetterGap(g, font), word = WordGap(g, font);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsWhiteSpace(s[i])) { x += word; continue; }
            int w = CharWidth(g, font, s[i]);
            TextRenderer.DrawText(g, s[i].ToString(), font, new Rectangle(x, box.Y, w + 2, box.Height), colour, flags);
            x += w + (i < s.Length - 1 && !char.IsWhiteSpace(s[i + 1]) ? letter : 0);
        }
    }

    private int Pill(Graphics g, int x, int y, string text, string? badge, bool on, Action click)
    {
        int w = CapsWidth(g, text, _pill) + P(24);
        int bw = badge == null ? 0 : Measure(g, badge, _small) + P(14);
        var box = new Rectangle(x, y, w + bw + (badge == null ? 0 : P(6)), P(32));
        using (var path = Round(box, P(8)))
        using (var fill = new SolidBrush(on ? Theme.Accent : Theme.Panel)) g.FillPath(fill, path);
        Caps(g, text, _pill, new Rectangle(x + P(12), y, w, box.Height), on ? Theme.AccentInk : Theme.Muted);
        if (badge != null)
        {
            var b = new Rectangle(x + w + P(2), y + P(7), bw, P(18));
            using (var path = Round(b, P(5)))
            using (var fill = new SolidBrush(on ? Theme.AccentHover : Theme.Card)) g.FillPath(fill, path);
            Text(g, badge, _small, b, on ? Theme.AccentInk : Theme.Muted, TextFormatFlags.HorizontalCenter);
        }
        _hits.Add((box, click));
        return box.Right;
    }

    /// <summary>Five-pointed star (drawn, so it never depends on a font having ★).</summary>
    private static void Star(Graphics g, float cx, float cy, float r)
    {
        var points = new PointF[10];
        for (int k = 0; k < 10; k++)
        {
            double angle = -Math.PI / 2 + k * Math.PI / 5, radius = k % 2 == 0 ? r : r * 0.45;
            points[k] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
        }
        using var b = new SolidBrush(Theme.Amber);
        g.FillPolygon(b, points);
    }

    private static int NiceStep(int max)
    {
        double raw = Math.Max(1, max) / 4.0, mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        foreach (var f in new[] { 1, 2, 2.5, 5, 10 })
            if (raw <= f * mag) return Math.Max(1, (int)Math.Ceiling(f * mag));
        return Math.Max(1, (int)Math.Ceiling(10 * mag));
    }

    // ---- Paint ----------------------------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        _hits.Clear();
        _columns.Clear();
        _plot = Rectangle.Empty;
        var rows = _data.Rows;
        int youOpened = rows.Sum(r => r.Opened), youBroken = rows.Sum(r => r.Broken), you = youOpened + youBroken;
        int mates = rows.Sum(r => r.Mates);
        var f = L.Culture;

        // Switch, and the rankings toggle on the right.
        int x = Pill(g, 0, 0, StatsText.T("Just you"), you.ToString("N0", f), !_withMates, () => { _withMates = false; _modeChosen = true; });
        Pill(g, x + P(8), 0, StatsText.T("With teammates"), (you + mates).ToString("N0", f), _withMates, () => { _withMates = true; _modeChosen = true; });
        DrawRankingsToggle(g);

        // Tiles
        int top = P(46), gap = P(12), tileH = P(150), count = 5;
        int tileW = (Width - gap * (count - 1)) / count;
        double avg = rows.Count > 0 ? rows.Average(Total) : 0;
        var best = rows.Count > 0 ? rows.MaxBy(Total) : null;
        var timed = rows.Where(r => r.Seconds != null).ToList();
        double minutes = timed.Sum(r => r.Seconds!.Value) / 60.0;
        double perMinute = minutes > 0 ? timed.Sum(Total) / minutes : 0;
        string mode = _withMates ? "All" : "";
        var tiles = new (string Value, string Label, string Foot, bool Dim, string Key)[]
        {
            (you.ToString("N0", f), StatsText.T("By you"), StatsText.F("{0:N0} opened · {1:N0} broken", youOpened, youBroken), false, "total"),
            (mates.ToString("N0", f), StatsText.T("Teammate chests"), StatsText.T("seen near you (at least)"), !_withMates, "mates"),
            (avg.ToString("0.0", f), StatsText.T("Per match"), StatsText.T("average"), false, "avg" + mode),
            ((best == null ? 0 : Total(best)).ToString("N0", f), StatsText.T("Best match"), best == null ? "—" : best.When.ToString(L.En ? "d MMM yyyy" : "d", f), false, "best" + mode),
            (perMinute.ToString("0.0", f), StatsText.T("Per minute"), StatsText.T("of match time"), false, "pm" + mode),
        };
        for (int i = 0; i < count; i++) DrawTile(g, new Rectangle(i * (tileW + gap), top, tileW, tileH), i, tiles[i], i == 0 && you > 0 ? youOpened / (double)you : null);

        DrawChart(g, new Rectangle(0, top + tileH + P(14), Width - 1, Height - (top + tileH + P(14)) - 1), rows);
    }

    private void DrawRankingsToggle(Graphics g)
    {
        bool on = Rankings.Enabled(_store.Folder);
        int players = _rank?.Players.Values.DefaultIfEmpty(0).Max() ?? 0;
        string state = !on ? StatsText.T("Off") : _rank == null ? StatsText.T("Connecting…") : players < 2 ? StatsText.T("Not enough players yet") : StatsText.F("{0:N0} players", players);
        var label = StatsText.T("Player rankings");
        int lw = CapsWidth(g, label, _pill), sw = Measure(g, state, _small);
        int w = lw + sw + P(46), h = P(32);
        var box = new Rectangle(Width - w - 1, 0, w, h);
        using (var path = Round(box, P(8)))
        {
            using var fill = new SolidBrush(Theme.Card); g.FillPath(fill, path);
            using var edge = new Pen(on ? Theme.Accent : Theme.CardEdge); g.DrawPath(edge, path);
        }
        var dot = new Rectangle(box.X + P(12), box.Y + h / 2 - P(4), P(8), P(8));
        using (var b = new SolidBrush(on ? Theme.Mint : Theme.Muted)) g.FillEllipse(b, dot);
        Caps(g, label, _pill, new Rectangle(dot.Right + P(8), box.Y, lw + P(4), h), on ? Theme.Ink : Theme.Muted);
        Text(g, state, _small, new Rectangle(dot.Right + P(8) + lw + P(10), box.Y, sw + P(4), h), Theme.Muted);
        _hits.Add((box, ToggleRankings));
    }

    private void DrawTile(Graphics g, Rectangle box, int i, (string Value, string Label, string Foot, bool Dim, string Key) t, double? openedShare)
    {
        var f = L.Culture;
        var rank = RankOf(t.Key);
        var baseColour = rank is { } r ? RankColour(r.Top) : TileColours[i];
        var colour = t.Dim ? Color.FromArgb(90, baseColour) : baseColour;
        using (var path = Round(box, P(10)))
        {
            using (var card = new SolidBrush(Theme.Card)) g.FillPath(card, path);
            g.SetClip(path);
            using (var tint = new LinearGradientBrush(box, Color.FromArgb(t.Dim ? 14 : 44, baseColour), Color.FromArgb(0, baseColour), 90f)) g.FillRectangle(tint, box);
            using (var stripe = new SolidBrush(colour)) g.FillRectangle(stripe, box.X, box.Y, box.Width, P(3));
            g.ResetClip();
            using var edge = new Pen(rank != null && !t.Dim ? Color.FromArgb(110, baseColour) : Theme.CardEdge); g.DrawPath(edge, path);
        }
        int pad = P(14), inner = box.Width - pad * 2;
        Text(g, t.Value, _tileValue, new Rectangle(box.X + pad, box.Y + P(12), inner, P(42)), t.Dim ? Theme.Muted : colour);
        // Grey icon, then the title.
        // (Only when Windows has its emoji font: without it the icons would be empty boxes.)
        int labelX = box.X + pad;
        if (HasEmojiFont)
        {
            var iconBox = new Rectangle(box.X + pad - P(1), box.Y + P(56), P(20), P(20));
            TextRenderer.DrawText(g, Icons[i], _icon, iconBox, IconGrey, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            labelX = iconBox.Right + P(5);
        }
        Caps(g, t.Label, _tileLabel, new Rectangle(labelX, box.Y + P(56), box.Right - pad - labelX, P(20)), Theme.Ink);
        Text(g, t.Foot, _small, new Rectangle(box.X + pad, box.Y + P(80), inner, P(18)), Theme.Muted);
        // Ranking line
        var rankBox = new Rectangle(box.X + pad, box.Y + P(104), inner, P(18));
        if (rank is { } rk)
            Text(g, StatsText.F("You're in the top {0}%", rk.Top), _smallBold, rankBox, t.Dim ? Theme.Muted : baseColour);
        else if (Rankings.Enabled(_store.Folder) && _rank != null)
            Text(g, StatsText.T("Not enough players yet"), _small, rankBox, Theme.Muted);
        if (openedShare is double share)
        {
            var split = new Rectangle(box.X + pad, box.Bottom - P(16), inner, P(5));
            int ow = (int)(split.Width * share);
            using (var path = Round(split, P(2))) { g.SetClip(path); }
            using (var a = new SolidBrush(YouBar)) g.FillRectangle(a, split.X, split.Y, ow, split.Height);
            using (var b = new SolidBrush(BrokenBar)) g.FillRectangle(b, split.X + ow, split.Y, split.Width - ow, split.Height);
            g.ResetClip();
        }
    }

    private void DrawChart(Graphics g, Rectangle card, List<Row> rows)
    {
        var f = L.Culture;
        using (var path = Round(card, P(12)))
        {
            using var fill = new SolidBrush(Theme.Card); g.FillPath(fill, path);
            using var edge = new Pen(Theme.CardEdge); g.DrawPath(edge, path);
        }
        int cx = card.X + P(18);
        Caps(g, StatsText.T(_perDay ? "Chests per day" : "Chests per match"), _title, new Rectangle(cx, card.Y + P(12), card.Width / 2, P(30)), Theme.Ink);
        int rx = card.Right - P(18);
        int pdW = CapsWidth(g, StatsText.T("Per day"), _pill) + P(24);
        int pmW = CapsWidth(g, StatsText.T("Per match"), _pill) + P(24);
        Pill(g, rx - pdW - pmW - P(8), card.Y + P(12), StatsText.T("Per match"), null, !_perDay, () => _perDay = false);
        Pill(g, rx - pdW, card.Y + P(12), StatsText.T("Per day"), null, _perDay, () => _perDay = true);

        var series = _perDay
            ? rows.GroupBy(r => r.When.Date).OrderBy(gr => gr.Key)
                .Select(gr => new Bar(gr.Key, gr.Sum(r => r.Opened + r.Broken), gr.Sum(r => r.Broken), _withMates ? gr.Sum(r => r.Mates) : 0)).ToList()
            : rows.Select(r => new Bar(r.When, r.Opened + r.Broken, r.Broken, _withMates ? r.Mates : 0)).ToList();
        double seriesAvg = series.Count > 0 ? series.Average(s => s.Total) : 0;
        Text(g, series.Count == 0 ? StatsText.T("No matches yet. They appear here once a replay has been analysed.")
                : StatsText.F(_perDay ? "Oldest to newest. The line marks your average of {0:0.0} per day." : "Oldest to newest. The line marks your average of {0:0.0} per match.", seriesAvg),
            _small, new Rectangle(cx, card.Y + P(46), card.Width - P(36), P(18)), Theme.Muted);

        // Legend
        int ly = card.Y + P(68), lx = cx;
        foreach (var (key, c) in new[] { ("Opened", YouBar), ("Broken", BrokenBar), ("Teammates", MateBar) })
        {
            if (key == "Teammates" && !_withMates) continue;
            var sw = new Rectangle(lx, ly + P(4), P(10), P(10));
            using (var path = Round(sw, P(3)))
            using (var b = new SolidBrush(c)) g.FillPath(b, path);
            var t = StatsText.T(key);
            Text(g, t, _small, new Rectangle(lx + P(15), ly, P(160), P(18)), Theme.Ink);
            lx += P(15) + Measure(g, t, _small) + P(16);
        }

        // The average pill sits in its own gutter on the right, so it never covers the newest bars.
        var avgText = StatsText.F("Avg {0:0.0}", seriesAvg);
        int aw = Measure(g, avgText, _smallBold) + P(14);
        var plot = new Rectangle(cx + P(34), ly + P(32), card.Width - P(70) - aw - P(8), card.Bottom - ly - P(58));
        if (series.Count == 0 || plot.Width < 40 || plot.Height < 40) return;
        int fit = Math.Max(1, plot.Width / Math.Max(1, P(9)));
        var shown = series.Skip(Math.Max(0, series.Count - fit)).ToList();
        if (series.Count > shown.Count)
            Text(g, StatsText.F("Showing the latest {0:N0} of {1:N0}", shown.Count, series.Count), _small, new Rectangle(card.Right - P(300), ly, P(282), P(18)), Theme.Muted, TextFormatFlags.Right);
        int max = Math.Max(1, shown.Max(s => s.Total));
        int step = NiceStep(max);
        int scale = Math.Max(step, (int)Math.Ceiling(max / (double)step) * step);
        _plot = plot;

        // Grid
        using (var grid = new Pen(Color.FromArgb(90, Theme.Line)) { DashStyle = DashStyle.Dot })
        using (var axis = new Pen(Theme.Line))
        {
            for (int v = 0; v <= scale; v += step)
            {
                int y = plot.Bottom - (int)Math.Round(plot.Height * (v / (double)scale));
                g.DrawLine(v == 0 ? axis : grid, plot.X, y, plot.Right, y);
                Text(g, v.ToString("N0", f), _small, new Rectangle(cx - P(4), y - P(9), P(34), P(18)), Theme.Muted, TextFormatFlags.Right);
            }
        }

        float slot = plot.Width / (float)shown.Count;
        float bar = Math.Min(Math.Max(2f, slot * 0.66f), P(46));
        float radius = Math.Min(bar / 2f, P(4));
        int bestIndex = shown.FindIndex(t => t.Total == max);
        string? lastLabel = null; int lastLabelEnd = -100000;
        for (int i = 0; i < shown.Count; i++)
        {
            var s = shown[i];
            float colX = plot.X + i * slot;
            _columns.Add((colX, slot));
            if (i == _hover)
                using (var hl = new SolidBrush(Color.FromArgb(70, Theme.PanelHover))) g.FillRectangle(hl, colX, plot.Y, slot, plot.Height);
            float bx = colX + (slot - bar) / 2;
            float hy = plot.Height * (s.You / (float)scale), hb = plot.Height * (s.Broken / (float)scale), hm = plot.Height * (s.Mates / (float)scale);
            float h = hy + hm;
            if (h >= 0.5f)
            {
                var column = new RectangleF(bx, plot.Bottom - h, bar, h);
                using var path = Round(column, radius, false);
                var state = g.Save();
                g.SetClip(path);
                bool lit = _hover == -1 || _hover == i;
                int a = lit ? 255 : 150;
                using (var b = new LinearGradientBrush(new RectangleF(bx, plot.Y, bar, plot.Height + 1), Color.FromArgb(a, ControlPaint.Light(YouBar, 0.25f)), Color.FromArgb(a, YouBar), 90f))
                    g.FillRectangle(b, bx, plot.Bottom - hy + hb, bar, hy - hb + 1);
                if (hb > 0) using (var b = new SolidBrush(Color.FromArgb(a, BrokenBar))) g.FillRectangle(b, bx, plot.Bottom - hy, bar, hb);
                if (hm > 0) using (var b = new SolidBrush(Color.FromArgb(a, MateBar))) g.FillRectangle(b, bx, plot.Bottom - h, bar, hm);
                g.Restore(state);
            }
            if (i == bestIndex) Star(g, colX + slot / 2, plot.Bottom - h - P(10), P(6));
            var label = s.When.ToString(L.Code == "ja" ? "M/d" : "d MMM", f);
            if (label != lastLabel)
            {
                int lw = Measure(g, label, _small), lx2 = Math.Min((int)bx, plot.Right - lw);
                if (lx2 - lastLabelEnd > P(14))
                {
                    Text(g, label, _small, new Rectangle(lx2, plot.Bottom + P(6), lw + P(4), P(16)), Theme.Muted);
                    lastLabel = label; lastLabelEnd = lx2 + lw;
                }
            }
        }

        // Average line with its value
        int ay = plot.Bottom - (int)Math.Round(plot.Height * (seriesAvg / scale));
        using (var avgPen = new Pen(Theme.Amber, Math.Max(1, P(1.5f))) { DashStyle = DashStyle.Dash }) g.DrawLine(avgPen, plot.X, ay, plot.Right, ay);
        var avgBox = new Rectangle(plot.Right + P(6), ay - P(9), aw, P(18));
        using (var path = Round(avgBox, P(8)))
        using (var b = new SolidBrush(Theme.Amber)) g.FillPath(b, path);
        Text(g, avgText, _smallBold, avgBox, Theme.Bg, TextFormatFlags.HorizontalCenter);

        if (_hover >= 0 && _hover < shown.Count) DrawTooltip(g, shown[_hover], plot, plot.X + _hover * slot + slot / 2);
    }

    private void DrawTooltip(Graphics g, Bar s, Rectangle plot, float centreX)
    {
        var f = L.Culture;
        var head = _perDay ? s.When.ToString(L.En ? "ddd d MMM yyyy" : "D", f) : s.When.ToString(L.En ? "ddd d MMM yyyy, HH:mm" : "g", f);
        var lines = new List<(string Label, string Value, Color Swatch)>
        {
            (StatsText.T("Opened"), (s.You - s.Broken).ToString("N0", f), YouBar),
            (StatsText.T("Broken"), s.Broken.ToString("N0", f), BrokenBar),
        };
        if (_withMates) lines.Add((StatsText.T("Teammates"), s.Mates.ToString("N0", f), MateBar));
        lines.Add((StatsText.T("Total"), s.Total.ToString("N0", f), Color.Empty));
        int lineH = P(18), w = Math.Max(Measure(g, head, _smallBold) + P(24), P(170));
        int h = P(30) + lines.Count * lineH + P(6);
        int x = (int)centreX + P(12);
        if (x + w > plot.Right) x = (int)centreX - P(12) - w;
        x = Math.Max(plot.X, x);
        var box = new Rectangle(x, plot.Y + P(4), w, h);
        using (var path = Round(box, P(8)))
        {
            using var fill = new SolidBrush(Color.FromArgb(245, Theme.Bg)); g.FillPath(fill, path);
            using var edge = new Pen(Theme.Accent); g.DrawPath(edge, path);
        }
        Text(g, head, _smallBold, new Rectangle(box.X + P(12), box.Y + P(8), w - P(24), P(18)), Theme.Ink);
        int y = box.Y + P(30);
        foreach (var (label, value, swatch) in lines)
        {
            if (swatch != Color.Empty)
                using (var b = new SolidBrush(swatch)) g.FillRectangle(b, box.X + P(12), y + P(5), P(8), P(8));
            var font = swatch == Color.Empty ? _smallBold : _small;
            Text(g, label, font, new Rectangle(box.X + P(26), y, w - P(80), lineH), swatch == Color.Empty ? Theme.Ink : Theme.Muted);
            Text(g, value, font, new Rectangle(box.X + P(12), y, w - P(24), lineH), Theme.Ink, TextFormatFlags.Right);
            y += lineH;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _tileValue.Dispose(); _tileLabel.Dispose(); _small.Dispose(); _smallBold.Dispose(); _pill.Dispose(); _title.Dispose(); _icon.Dispose(); }
        base.Dispose(disposing);
    }
}
