using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>Round tick mark that the player clicks to say "I own this".</summary>
[SupportedOSPlatform("windows")]
public sealed class TickBadge : Control
{
    private bool _on;

    public TickBadge()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(30, 30);
        Cursor = Cursors.Hand;
    }

    public event EventHandler? Toggled;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool On
    {
        get => _on;
        set
        {
            if (_on != value)
            {
                _on = value;
                Invalidate();
            }
        }
    }

    protected override void OnClick(EventArgs e)
    {
        _on = !_on;
        Invalidate();
        Toggled?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Card);
        var r = new RectangleF(1.5f, 1.5f, Width - 3, Height - 3);
        if (_on)
        {
            using var fill = new SolidBrush(Theme.Mint);
            g.FillEllipse(fill, r);
            using var pen = new Pen(Color.FromArgb(10, 40, 25), 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(pen, new[]
            {
                new PointF(Width * 0.28f, Height * 0.52f),
                new PointF(Width * 0.44f, Height * 0.68f),
                new PointF(Width * 0.73f, Height * 0.34f),
            });
        }
        else
        {
            using var pen = new Pen(Theme.Line, 2f);
            g.DrawEllipse(pen, r);
        }
    }
}

/// <summary>
/// The six weekly rewards with a square picture and a tick for "owned" (ownership cannot be read from replays).
/// Ticks belong to the current week and clear themselves at the weekly reset (Sunday 14:00 GMT).
/// Owned rewards disappear from Weekly chances.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RewardsForm : Form
{
    private const int PictureSize = 168;
    private const int PictureRadius = 16;
    private const int CardWidth = 188;
    private const int CardHeight = 262;
    private const int BarWidth = 280;

    private readonly Monitor _monitor;
    private readonly List<Bitmap> _images = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
    private readonly Label _countdown;
    private readonly Panel _barFill;
    private double _fillNow;
    private double _fillTarget;

    public RewardsForm(Monitor monitor, Store store)
    {
        _monitor = monitor;
        Text = "Weekly rewards";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(660, 700);
        MinimumSize = new Size(460, 420);
        ShowInTaskbar = false;
        Icon = AppIcon.Load();

        var folder = Path.Combine(store.Folder, "rewards");
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch
        {
            // the checklist still works without pictures
        }

        var header = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Theme.Bg };
        var title = new Label { Text = "Weekly rewards", Font = Theme.Text(18f, FontStyle.Bold), ForeColor = Theme.Ink, AutoSize = true, Location = new Point(20, 10), BackColor = Theme.Bg };
        var counter = new Label { Font = Theme.Text(10f, FontStyle.Bold), ForeColor = Theme.Mint, AutoSize = true, Location = new Point(22, 46), BackColor = Theme.Bg };
        var barTrack = new Panel { Location = new Point(22, 74), Size = new Size(BarWidth, 6), BackColor = Theme.Panel };
        _barFill = new Panel { Location = new Point(0, 0), Size = new Size(0, 6), BackColor = Theme.Mint };
        barTrack.Controls.Add(_barFill);

        var open = new PillButton { Text = "Open pictures folder", Size = new Size(170, 32), Radius = 10, Font = Theme.Text(8.5f, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(660 - 20 - 170, 16) };
        open.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();
            }
            catch
            {
                // no file manager available
            }
        };

        _countdown = new Label
        {
            Font = Theme.Text(10f, FontStyle.Bold),
            ForeColor = Theme.Cyan,
            BackColor = Theme.Bg,
            AutoSize = false,
            Size = new Size(280, 20),
            Location = new Point(660 - 20 - 280, 56),
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
            Size = new Size(280, 18),
            Location = new Point(660 - 20 - 280, 78),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
        };
        header.Controls.Add(title);
        header.Controls.Add(counter);
        header.Controls.Add(barTrack);
        header.Controls.Add(open);
        header.Controls.Add(_countdown);
        header.Controls.Add(resetNote);

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Bg, Padding = new Padding(14, 6, 14, 14) };
        Controls.Add(flow);
        Controls.Add(header);

        var owned = _monitor.Config.OwnedRewards;
        bool IsOwned(string key) => owned.TryGetValue(WeeklyReset.OwnedKey(key, DateTime.UtcNow), out var v) && v;

        void UpdateCounter()
        {
            var count = RewardPictures.All.Count(r => IsOwned(r.Key));
            counter.Text = $"{count}/{RewardPictures.All.Length} owned";
            _fillTarget = count / (double)RewardPictures.All.Length;
            if (!_anim.Enabled)
            {
                _anim.Start();
            }
        }

        foreach (var (key, name) in RewardPictures.All)
        {
            var rewardKey = key;
            var card = new Card { Size = new Size(CardWidth, CardHeight), Radius = 16, Margin = new Padding(6) };

            var holder = new Card
            {
                Location = new Point((CardWidth - PictureSize) / 2, 10),
                Size = new Size(PictureSize, PictureSize),
                Radius = PictureRadius,
                BackColor = Theme.Panel,
            };
            var picture = RewardPictures.Load(folder, key, PictureSize, PictureRadius);
            _images.Add(picture);
            holder.Controls.Add(new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Image = picture, BackColor = Theme.Panel });
            card.Controls.Add(holder);

            card.Controls.Add(new Label
            {
                Text = name,
                UseMnemonic = false,
                Font = Theme.Text(9.5f, FontStyle.Bold),
                ForeColor = Theme.Ink,
                BackColor = Theme.Card,
                Location = new Point(14, 186),
                Size = new Size(CardWidth - 28, 36),
                AutoEllipsis = true,
            });

            var tick = new TickBadge { Location = new Point(14, 226), On = IsOwned(key), BackColor = Theme.Card };
            var state = new Label { Font = Theme.Text(9f, FontStyle.Bold), BackColor = Theme.Card, Location = new Point(52, 231), Size = new Size(CardWidth - 66, 20) };
            void ShowOwned()
            {
                state.Text = tick.On ? "Owned" : "Not owned";
                state.ForeColor = tick.On ? Theme.Mint : Theme.Muted;
            }
            ShowOwned();
            tick.Toggled += (_, _) =>
            {
                var ownedKey = WeeklyReset.OwnedKey(rewardKey, DateTime.UtcNow);
                _monitor.Change(c => c.OwnedRewards[ownedKey] = tick.On);
                ShowOwned();
                UpdateCounter();
            };
            card.Controls.Add(tick);
            card.Controls.Add(state);
            flow.Controls.Add(card);
        }

        _anim.Tick += (_, _) =>
        {
            _fillNow += (_fillTarget - _fillNow) * 0.2;
            if (Math.Abs(_fillTarget - _fillNow) < 0.002)
            {
                _fillNow = _fillTarget;
                _anim.Stop();
            }
            _barFill.Width = (int)Math.Round(BarWidth * _fillNow);
        };

        _clock.Tick += (_, _) => UpdateCountdown();
        UpdateCountdown();
        _clock.Start();
        UpdateCounter();
    }

    private void UpdateCountdown()
    {
        var left = WeeklyReset.Remaining(DateTime.UtcNow);
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }
        _countdown.Text = $"Resets in {left.Days}d {left.Hours:00}h {left.Minutes:00}m {left.Seconds:00}s";
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
            _anim.Stop();
            _anim.Dispose();
            foreach (var image in _images)
            {
                image.Dispose();
            }
            _images.Clear();
        }
        base.Dispose(disposing);
    }
}
