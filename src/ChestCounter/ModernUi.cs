using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

internal static class Fx
{
    /// <summary>Blend two colours: 0 gives <paramref name="a"/>, 1 gives <paramref name="b"/>.</summary>
    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }
}

/// <summary>
/// Modern rounded button: soft gradient, smooth hover fade, pressed feel and an accent bar when "selected".
/// It is a normal Button, so existing code that sets BackColor, ForeColor and FlatAppearance.BorderColor keeps working:
/// the fill is BackColor, the outline is FlatAppearance.BorderColor, and a border equal to Theme.Accent means "selected".
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GlowButton : Button
{
    private readonly System.Windows.Forms.Timer _fade = new() { Interval = 16 };
    private float _hover;
    private float _target;
    private bool _down;

    public GlowButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Theme.Panel;
        ForeColor = Theme.Ink;
        Font = Theme.Text(9f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        _fade.Tick += (_, _) =>
        {
            var d = _target - _hover;
            if (Math.Abs(d) < 0.02f)
            {
                _hover = _target;
                _fade.Stop();
            }
            else
            {
                _hover += d * 0.28f;
            }
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 12;

    private void FadeTo(float target)
    {
        _target = target;
        if (!_fade.Enabled)
        {
            _fade.Start();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        FadeTo(1f);
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _down = false;
        FadeTo(0f);
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _down = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _down = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent) => pevent.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 4 || Height < 4)
        {
            return;
        }
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var selected = FlatAppearance.BorderColor.ToArgb() == Theme.Accent.ToArgb();
        var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        var baseColor = Enabled ? BackColor : Fx.Mix(BackColor, Theme.Bg, 0.5f);
        var fillColor = Fx.Mix(baseColor, Color.White, 0.07f * _hover);
        if (_down)
        {
            fillColor = Fx.Mix(fillColor, Color.Black, 0.16f);
        }

        using var path = Shape.Rounded(rect, Radius);
        using (var fill = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), Fx.Mix(fillColor, Color.White, 0.06f), fillColor, 90f))
        {
            g.FillPath(fill, path);
        }

        var border = FlatAppearance.BorderColor.IsEmpty ? Theme.CardEdge : FlatAppearance.BorderColor;
        border = Fx.Mix(border, Theme.Accent, 0.65f * _hover);
        using (var edge = new Pen(border, 1f))
        {
            g.DrawPath(edge, path);
        }

        if (selected)
        {
            var bar = new RectangleF(7, Height * 0.25f, 3f, Height * 0.5f);
            using var barPath = Shape.Rounded(bar, 1.5f);
            using var barFill = new SolidBrush(Theme.Accent);
            g.FillPath(barFill, barPath);
        }

        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
        flags |= TextAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.HorizontalCenter,
        };
        var textRect = new Rectangle(Padding.Left + (selected ? 6 : 0), _down ? 1 : 0, Math.Max(1, Width - Padding.Horizontal - (selected ? 6 : 0)), Height);
        TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? ForeColor : Theme.Muted, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fade.Stop();
            _fade.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Card with a soft top-to-bottom gradient, a thin highlight on its top edge and an optional small accent mark.
/// Same idea as Card, just a more modern surface. Put transparent labels on it.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GlassCard : Panel
{
    public GlassCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 16;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? AccentColor { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 6 || Height < 6)
        {
            return;
        }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using var path = Shape.Rounded(rect, Radius);
        using (var fill = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), Fx.Mix(BackColor, Color.White, 0.05f), BackColor, 90f))
        {
            g.FillPath(fill, path);
        }
        using (var edge = new Pen(Theme.CardEdge, 1f))
        {
            g.DrawPath(edge, path);
        }
        if (Width > Radius * 2 + 8)
        {
            using var sheen = new Pen(Color.FromArgb(46, 255, 255, 255), 1f);
            g.DrawLine(sheen, Radius, 1, Width - Radius, 1);
        }
        if (AccentColor is { } accent)
        {
            using var mark = Shape.Rounded(new RectangleF(16, 12, 26, 3), 1.5f);
            using var markFill = new SolidBrush(accent);
            g.FillPath(markFill, mark);
        }
    }
}

/// <summary>
/// Modern drop-down for choosing an account. It mimics the parts of ComboBox that MainForm uses
/// (DisplayMember, DataSource, Items, SelectedItem, SelectedIndexChanged), so it is a drop-in replacement.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ModernSelect : Control
{
    private System.Collections.IList _items = new List<object>();
    private object? _selected;
    private bool _hover;
    private readonly Font _caption = Fonts.Body(7f, FontStyle.Bold);
    private readonly Font _value = Fonts.Body(10f, FontStyle.Bold);

    public ModernSelect()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(250, 44);
        Cursor = Cursors.Hand;
        BackColor = Theme.Panel;
        ForeColor = Theme.Ink;
    }

    public event EventHandler? SelectedIndexChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DisplayMember { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? DataSource
    {
        get => _items;
        set
        {
            _items = value as System.Collections.IList ?? new List<object>();
            if (_selected != null && !_items.Contains(_selected))
            {
                _selected = null;
            }
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public System.Collections.IEnumerable Items => _items;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? SelectedItem
    {
        get => _selected;
        set
        {
            if (Equals(_selected, value))
            {
                return;
            }
            _selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string TextOf(object? item)
    {
        if (item == null)
        {
            return "";
        }
        if (DisplayMember.Length > 0)
        {
            var property = item.GetType().GetProperty(DisplayMember);
            if (property != null)
            {
                return property.GetValue(item)?.ToString() ?? "";
            }
        }
        return item.ToString() ?? "";
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left && _items.Count > 1 && ClientRectangle.Contains(e.Location))
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            BackColor = Theme.Panel,
            Font = Theme.Text(9.5f),
            Padding = new Padding(4),
            MinimumSize = new Size(Width, 0),
        };
        foreach (var item in _items.Cast<object>().ToList())
        {
            var captured = item;
            var entry = new ToolStripMenuItem(TextOf(item))
            {
                ForeColor = Equals(item, _selected) ? Theme.Cyan : Theme.Ink,
                Padding = new Padding(4, 4, 18, 4),
            };
            entry.Click += (_, _) => SelectedItem = captured;
            menu.Items.Add(entry);
        }
        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(this, new Point(0, Height + 4));
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 60 || Height < 20)
        {
            return;
        }
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var path = Shape.Rounded(rect, 12))
        using (var fill = new SolidBrush(_hover ? Theme.PanelHover : BackColor))
        using (var edge = new Pen(_hover ? Color.FromArgb(160, Theme.Accent) : Theme.CardEdge, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        var many = _items.Count > 1;
        var textWidth = Width - (many ? 48 : 28);
        TextRenderer.DrawText(g, Fonts.Track("ACCOUNT"), _caption, new Rectangle(14, 5, textWidth, 14), Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, TextOf(_selected), _value, new Rectangle(14, 19, textWidth, Height - 24), ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        if (many)
        {
            var cx = Width - 22f;
            var cy = Height / 2f;
            using var arrow = new SolidBrush(_hover ? Theme.Accent : Theme.Muted);
            g.FillPolygon(arrow, new[] { new PointF(cx - 5, cy - 2), new PointF(cx + 5, cy - 2), new PointF(cx, cy + 4) });
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _caption.Dispose();
            _value.Dispose();
        }
        base.Dispose(disposing);
    }
}
