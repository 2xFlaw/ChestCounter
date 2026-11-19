using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Card under the weekly odds that points Pirate Shop owners to the Claim All add-on on chestcounter.io/claim.
/// It only opens the page in the browser: the app never signs in to Epic or touches the Item Shop itself.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class ClaimAllBanner : Control, IPaintsBehindChildren
{
    private const int Pad = 18;
    private const int IconSize = 60;
    private const int Gap = 16;
    private const int ButtonWidth = 210;
    private const int ButtonHeight = 40;

    private readonly Font _tag = Fonts.Body(7.5f, FontStyle.Bold);
    private readonly Font _title = Theme.Text(11.5f, FontStyle.Bold);
    private readonly Font _sub = Theme.Text(8.5f);
    private readonly GlowButton _button;
    private readonly ToolTip _tip = new();
    private Bitmap? _icon;

    public ClaimAllBanner(string rewardsFolder)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
        try { _icon = RewardPictures.Load(rewardsFolder, "pirate-shop-access", IconSize, 14); } catch { _icon = null; }

        _button = new GlowButton
        {
            Text = ClaimAllText.T("Claim All"),
            Size = new Size(ButtonWidth, ButtonHeight),
            Radius = 12,
            Font = Theme.Text(9.5f, FontStyle.Bold),
            BackColor = Theme.Accent,
            ForeColor = Theme.AccentInk,
            UseMnemonic = false,
            ShowSelectedBar = false,
        };
        _button.FlatAppearance.BorderColor = Theme.Accent;
        _button.Click += (_, _) => Open();
        Controls.Add(_button);
        Click += (_, _) => Open();

        var tip = ClaimAllText.T("Opens chestcounter.io/claim in your browser");
        _tip.SetToolTip(this, tip);
        _tip.SetToolTip(_button, tip);
        Height = HeightFor(760);
    }

    private static string Title => ClaimAllText.T("Already own the Pirate Shop Access Token? Claim the whole Item Shop with one button!");
    private static string Sub => ClaimAllText.T("Free for Chrome, Edge, Firefox and Safari. Never spends V-Bucks.");
    private static string Caption => Fonts.Track(ClaimAllText.T("Pirate Shop Claim All").ToUpper(L.Culture));

    private const TextFormatFlags Wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
    private const TextFormatFlags Line = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

    private int TextWidth(int width) => Math.Max(200, width - Pad * 2 - IconSize - Gap - ButtonWidth - Gap);

    /// <summary>Height needed at a given width so the headline can wrap onto as many lines as the language needs.</summary>
    public int HeightFor(int width)
    {
        var w = TextWidth(width);
        var tag = TextRenderer.MeasureText(Caption, _tag, new Size(w, 100), Line).Height;
        var title = TextRenderer.MeasureText(Title, _title, new Size(w, 400), Wrap).Height;
        var sub = TextRenderer.MeasureText(Sub, _sub, new Size(w, 200), Wrap).Height;
        return Math.Max(IconSize + Pad * 2, Pad + tag + 4 + title + 4 + sub + Pad);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        _button.Location = new Point(Width - Pad - ButtonWidth, (Height - ButtonHeight) / 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Bg);
        PaintSurface(g);

        if (_icon != null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_icon, new Rectangle(Pad, (Height - IconSize) / 2, IconSize, IconSize));
        }

        var x = Pad + IconSize + Gap;
        var w = TextWidth(Width);
        var tagH = TextRenderer.MeasureText(Caption, _tag, new Size(w, 100), Line).Height;
        var titleH = TextRenderer.MeasureText(Title, _title, new Size(w, 400), Wrap).Height;
        var subH = TextRenderer.MeasureText(Sub, _sub, new Size(w, 200), Wrap).Height;
        var y = (Height - (tagH + 4 + titleH + 4 + subH)) / 2;
        TextRenderer.DrawText(g, Caption, _tag, new Rectangle(x, y, w, tagH), Theme.Amber, Line);
        y += tagH + 4;
        TextRenderer.DrawText(g, Title, _title, new Rectangle(x, y, w, titleH), Theme.Ink, Wrap);
        y += titleH + 4;
        TextRenderer.DrawText(g, Sub, _sub, new Rectangle(x, y, w, subH), Theme.Muted, Wrap);
    }

    /// <summary>The card itself, without the icon or text. The button paints this behind its rounded corners.</summary>
    public void PaintSurface(Graphics g)
    {
        var smoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var path = Shape.Rounded(card, 16))
        {
            using var fill = new LinearGradientBrush(card, Color.FromArgb(46, 16, 44), Color.FromArgb(24, 14, 32), LinearGradientMode.Horizontal);
            g.FillPath(fill, path);
            using var glow = new LinearGradientBrush(card, Color.FromArgb(70, 208, 0, 128), Color.FromArgb(0, 208, 0, 128), LinearGradientMode.Horizontal);
            g.FillPath(glow, path);
            using var edge = new Pen(Theme.CardEdge, 1f);
            g.DrawPath(edge, path);
        }
        g.SmoothingMode = smoothing;
    }

    private void Open()
    {
        try { Process.Start(new ProcessStartInfo(ClaimAllText.PageUrl) { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Chest Counter"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon?.Dispose();
            _tag.Dispose();
            _title.Dispose();
            _sub.Dispose();
            _tip.Dispose();
        }
        base.Dispose(disposing);
    }
}
