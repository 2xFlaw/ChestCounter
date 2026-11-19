using System.Drawing.Drawing2D;

namespace ChestCounter;

/// <summary>
/// Dashboard chest statistics: Just you / With teammates switch, five coloured tiles and a chests-per-match
/// (or per-day) chart. Reads the saved history of the shown profile; never changes it.
/// </summary>
internal sealed class ChestStatsPanel : Control
{
    private sealed record Row(DateTime When, int Opened, int Broken, int Mates, int? Seconds);
    private sealed record Data(List<Row> Rows);

    private readonly Monitor _monitor;
    private readonly Store _store;
    private Data _data = new(new List<Row>());
    private bool _withMates;
    private bool _modeChosen;
    private bool _perDay;
    private string _signature = "";
    private int _loading;
    private readonly List<(Rectangle Box, Action Click)> _hits = new();
    private readonly Font _tileValue = Fonts.Display(24f);
    private readonly Font _tileLabel = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _small = Theme.Text(8.5f);
    private readonly Font _pill = Theme.Text(8.5f, FontStyle.Bold);
    private readonly Font _title = Fonts.Display(14f);

    private static readonly Color[] TileColours =
    {
        Theme.Accent, Color.FromArgb(176, 92, 160), Theme.AccentSoft, Theme.Amber, Theme.Mint,
    };
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
        Height = 470;
        Margin = new Padding(0, 12, 0, 12);
        Cursor = Cursors.Default;
    }

    private float S => DeviceDpi / 96f;
    private int P(float v) => (int)Math.Round(v * S);

    /// <summary>Reloads the history in the background when the profile, settings or match count changed.</summary>
    public void Refresh(MonitorState s)
    {
        var c = _monitor.Config;
        var sig = $"{s.CurrentKey}|{s.Stats.HistoryMatches}|{s.Stats.LastMatch?.Chests}|{c.CountTeammateChests}|{string.Join(",", c.ExtraKinds())}|{c.CountFrom:O}";
        if (sig == _signature || Interlocked.Exchange(ref _loading, 1) == 1) return;
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
            if (t.Result is not { } d || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _data = d;
                    if (!_modeChosen) _withMates = d.Rows.Any(r => r.Mates > 0);
                    Invalidate();
                }));
            }
            catch (InvalidOperationException) { }
        });
    }

    private int Total(Row r) => r.Opened + r.Broken + (_withMates ? r.Mates : 0);

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        foreach (var (box, click) in _hits)
            if (box.Contains(e.Location)) { click(); Invalidate(); return; }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hand = _hits.Any(h => h.Box.Contains(e.Location));
        if ((Cursor == Cursors.Hand) != hand) Cursor = hand ? Cursors.Hand : Cursors.Default;
    }

    private static GraphicsPath Round(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        int d = Math.Max(1, radius * 2);
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static void Text(Graphics g, string text, Font font, Rectangle box, Color colour, TextFormatFlags extra = 0) =>
        TextRenderer.DrawText(g, text, font, box, colour, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | extra);

    private int Pill(Graphics g, int x, int y, string text, string? badge, bool on, Action click)
    {
        var label = Fonts.Track(text.ToUpper(L.Culture));
        int w = TextRenderer.MeasureText(g, label, _pill, Size.Empty, TextFormatFlags.NoPadding).Width + P(24);
        int bw = badge == null ? 0 : TextRenderer.MeasureText(g, badge, _small, Size.Empty, TextFormatFlags.NoPadding).Width + P(14);
        var box = new Rectangle(x, y, w + bw + (badge == null ? 0 : P(6)), P(32));
        using (var path = Round(box, P(8)))
        using (var fill = new SolidBrush(on ? Theme.Accent : Theme.Panel)) g.FillPath(fill, path);
        Text(g, label, _pill, new Rectangle(x + P(12), y, w, box.Height), on ? Theme.AccentInk : Theme.Muted);
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        _hits.Clear();
        var rows = _data.Rows;
        int youOpened = rows.Sum(r => r.Opened), youBroken = rows.Sum(r => r.Broken), you = youOpened + youBroken;
        int mates = rows.Sum(r => r.Mates);
        var f = L.Culture;

        // Switch
        int x = Pill(g, 0, 0, StatsText.T("Just you"), you.ToString("N0", f), !_withMates, () => { _withMates = false; _modeChosen = true; });
        Pill(g, x + P(8), 0, StatsText.T("With teammates"), (you + mates).ToString("N0", f), _withMates, () => { _withMates = true; _modeChosen = true; });

        // Tiles
        int top = P(46), gap = P(12), tileH = P(118), count = 5;
        int tileW = (Width - gap * (count - 1)) / count;
        double avg = rows.Count > 0 ? rows.Average(Total) : 0;
        var best = rows.Count > 0 ? rows.MaxBy(Total) : null;
        var timed = rows.Where(r => r.Seconds != null).ToList();
        double minutes = timed.Sum(r => r.Seconds!.Value) / 60.0;
        double perMinute = minutes > 0 ? timed.Sum(Total) / minutes : 0;
        var tiles = new (string Value, string Label, string Foot, bool Dim)[]
        {
            (you.ToString("N0", f), StatsText.T("By you"), StatsText.F("{0:N0} opened · {1:N0} broken", youOpened, youBroken), false),
            (mates.ToString("N0", f), StatsText.T("Teammate chests"), StatsText.T("seen nearby (a minimum)"), !_withMates),
            (avg.ToString("0.0", f), StatsText.T("Per match"), StatsText.T("average"), false),
            ((best == null ? 0 : Total(best)).ToString("N0", f), StatsText.T("Best match"), best == null ? "—" : best.When.ToString("g", f), false),
            (perMinute.ToString("0.0", f), StatsText.T("Per minute"), StatsText.T("of match time"), false),
        };
        for (int i = 0; i < count; i++)
        {
            var box = new Rectangle(i * (tileW + gap), top, tileW, tileH);
            var colour = tiles[i].Dim ? Color.FromArgb(90, TileColours[i]) : TileColours[i];
            using (var path = Round(box, P(10)))
            {
                using (var fill = new LinearGradientBrush(box, Color.FromArgb(tiles[i].Dim ? 14 : 38, TileColours[i]), Theme.Card, 90f)) using (var card0 = new SolidBrush(Theme.Card)) { g.SetClip(path); g.FillRectangle(card0, box); g.FillRectangle(fill, box); }
                using (var stripe = new SolidBrush(colour)) g.FillRectangle(stripe, box.X, box.Y, box.Width, P(3));
                g.ResetClip();
                using var edge = new Pen(Theme.CardEdge); g.DrawPath(edge, path);
            }
            Text(g, tiles[i].Value, _tileValue, new Rectangle(box.X + P(12), box.Y + P(12), box.Width - P(20), P(42)), tiles[i].Dim ? Theme.Muted : colour);
            Text(g, Fonts.Track(tiles[i].Label.ToUpper(f)), _tileLabel, new Rectangle(box.X + P(12), box.Y + P(56), box.Width - P(20), P(18)), Theme.Ink);
            Text(g, tiles[i].Foot, _small, new Rectangle(box.X + P(12), box.Y + P(78), box.Width - P(20), P(18)), Theme.Muted);
            if (i == 0 && you > 0)
            {
                var split = new Rectangle(box.X + P(12), box.Bottom - P(14), box.Width - P(24), P(5));
                int ow = (int)(split.Width * (youOpened / (double)you));
                using (var a = new SolidBrush(YouBar)) g.FillRectangle(a, split.X, split.Y, ow, split.Height);
                using (var b = new SolidBrush(BrokenBar)) g.FillRectangle(b, split.X + ow, split.Y, split.Width - ow, split.Height);
            }
        }
        g.ResetClip();

        // Chart card
        var card = new Rectangle(0, top + tileH + P(14), Width - 1, Height - (top + tileH + P(14)) - 1);
        using (var path = Round(card, P(12)))
        {
            using var fill = new SolidBrush(Theme.Card); g.FillPath(fill, path);
            using var edge = new Pen(Theme.CardEdge); g.DrawPath(edge, path);
        }
        int cx = card.X + P(16);
        Text(g, Fonts.Track(StatsText.T(_perDay ? "Chests per day" : "Chests per match").ToUpper(f)), _title, new Rectangle(cx, card.Y + P(10), card.Width / 2, P(30)), Theme.Ink);
        int rx = card.Right - P(16);
        int pdW = TextRenderer.MeasureText(g, Fonts.Track(StatsText.T("Per day").ToUpper(f)), _pill, Size.Empty, TextFormatFlags.NoPadding).Width + P(24);
        int pmW = TextRenderer.MeasureText(g, Fonts.Track(StatsText.T("Per match").ToUpper(f)), _pill, Size.Empty, TextFormatFlags.NoPadding).Width + P(24);
        Pill(g, rx - pdW - pmW - P(8), card.Y + P(10), StatsText.T("Per match"), null, !_perDay, () => _perDay = false);
        Pill(g, rx - pdW, card.Y + P(10), StatsText.T("Per day"), null, _perDay, () => _perDay = true);

        // Series: you / teammates per bar
        List<(DateTime When, int You, int Mates, int Broken)> series = _perDay
            ? rows.GroupBy(r => r.When.Date).OrderBy(gr => gr.Key).Select(gr => (gr.Key, gr.Sum(r => r.Opened + r.Broken), _withMates ? gr.Sum(r => r.Mates) : 0, gr.Sum(r => r.Broken))).ToList()
            : rows.Select(r => (r.When, r.Opened + r.Broken, _withMates ? r.Mates : 0, r.Broken)).ToList();
        double seriesAvg = series.Count > 0 ? series.Average(s => s.You + s.Mates) : 0;
        Text(g, series.Count == 0 ? StatsText.T("No matches yet. They appear here once a replay has been analysed.")
                : StatsText.F(_perDay ? "Oldest to newest. The line marks your average of {0:0.0} per day." : "Oldest to newest. The line marks your average of {0:0.0} per match.", seriesAvg),
            _small, new Rectangle(cx, card.Y + P(42), card.Width - P(32), P(18)), Theme.Muted);
        int ly = card.Y + P(64);
        int lx = cx;
        foreach (var (key, c) in new[] { ("Opened", YouBar), ("Broken", BrokenBar), ("Teammates", MateBar) })
        {
            if (key == "Teammates" && !_withMates) continue;
            using (var b = new SolidBrush(c)) g.FillRectangle(b, lx, ly + P(4), P(10), P(10));
            var t = StatsText.T(key);
            Text(g, t, _small, new Rectangle(lx + P(14), ly, P(160), P(18)), Theme.Ink);
            lx += P(14) + TextRenderer.MeasureText(g, t, _small).Width + P(14);
        }

        var plot = new Rectangle(cx + P(30), ly + P(28), card.Width - P(62), card.Bottom - ly - P(52));
        if (series.Count == 0 || plot.Width < 20 || plot.Height < 20) return;
        int fit = Math.Max(1, plot.Width / Math.Max(P(5), 1));
        var shown = series.Skip(Math.Max(0, series.Count - fit)).ToList();
        int max = Math.Max(1, shown.Max(s => s.You + s.Mates));
        int scale = max <= 10 ? 10 : (int)(Math.Ceiling(max / 25.0) * 25);
        using (var grid = new Pen(Theme.Line))
        {
            foreach (var v in new[] { 0, scale / 2, scale })
            {
                int y = plot.Bottom - (int)(plot.Height * (v / (double)scale));
                g.DrawLine(grid, plot.X, y, plot.Right, y);
                Text(g, v.ToString("N0", f), _small, new Rectangle(cx - P(4), y - P(9), P(32), P(18)), Theme.Muted, TextFormatFlags.Right);
            }
        }
        float slot = plot.Width / (float)shown.Count;
        int bestIndex = shown.FindIndex(t => t.You + t.Mates == max);
        float bar = Math.Max(1f, slot * 0.72f);
        g.SmoothingMode = SmoothingMode.None;
        string? lastLabel = null; float lastLabelX = -999;
        for (int i = 0; i < shown.Count; i++)
        {
            var s = shown[i];
            float bx = plot.X + i * slot + (slot - bar) / 2;
            float hy = plot.Height * (s.You / (float)scale), hm = plot.Height * (s.Mates / (float)scale);
            float hb = plot.Height * (s.Broken / (float)scale);
            using (var b = new SolidBrush(YouBar)) g.FillRectangle(b, bx, plot.Bottom - hy + hb, bar, hy - hb);
            if (hb > 0) using (var b = new SolidBrush(BrokenBar)) g.FillRectangle(b, bx, plot.Bottom - hy, bar, hb);
            if (hm > 0) using (var b = new SolidBrush(MateBar)) g.FillRectangle(b, bx, plot.Bottom - hy - hm, bar, hm);
            if (i == bestIndex && bar >= P(3)) using (var pen = new Pen(Theme.Ink)) g.DrawRectangle(pen, bx - 1, plot.Bottom - hy - hm - 1, bar + 1, hy + hm + 1);
            var label = s.When.ToString("d MMM", f);
            if (label != lastLabel && bx - lastLabelX > P(60))
            {
                Text(g, label, _small, new Rectangle((int)bx, plot.Bottom + P(4), P(70), P(16)), Theme.Muted);
                lastLabel = label; lastLabelX = bx;
            }
        }
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int ay = plot.Bottom - (int)(plot.Height * (seriesAvg / scale));
        using (var avgPen = new Pen(Theme.Amber, Math.Max(1, P(1))) { DashStyle = DashStyle.Dash }) g.DrawLine(avgPen, plot.X, ay, plot.Right, ay);
        if (series.Count > shown.Count)
            Text(g, StatsText.F("Showing the latest {0:N0} of {1:N0}", shown.Count, series.Count), _small, new Rectangle(card.Right - P(260), ly, P(244), P(18)), Theme.Muted, TextFormatFlags.Right);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _tileValue.Dispose(); _tileLabel.Dispose(); _small.Dispose(); _pill.Dispose(); _title.Dispose(); }
        base.Dispose(disposing);
    }
}
