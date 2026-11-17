using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace ChestCounter;

// Images remain owned by MainForm. Full banner is fitted, never cover-cropped.
public sealed class BrandHero : Control
{
    private readonly Image banner,avatar;
    private readonly Font nameFont=new("Segoe UI",22,FontStyle.Bold);
    private readonly Font detailFont=new("Segoe UI",9,FontStyle.Regular);
    private bool sizing;
    public BrandHero(Image banner,Image avatar)
    {
        this.banner=banner;this.avatar=avatar;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        BackColor=Theme.Card;
    }
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if(sizing||banner==null||Width<4)return;
        int desired=(int)Math.Ceiling((Width-2)*banner.Height/(double)banner.Width)+112;
        if(Height!=desired){sizing=true;Height=desired;sizing=false;}
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Match rounded exterior corners to their actual parent, not white.
        e.Graphics.Clear(Parent?.BackColor??Theme.Bg);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if(Width<4||Height<4)return;
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        var outer=new RectangleF(1,1,Width-2,Height-2);
        using var shape=Shape.Rounded(outer,18);
        var saved=g.Save();g.SetClip(shape);
        using(var fill=new SolidBrush(Theme.Card))g.FillRectangle(fill,ClientRectangle);
        float imageHeight=Math.Max(1,Height-112);
        var available=new RectangleF(1,1,Width-2,imageHeight);
        float scale=Math.Min(available.Width/banner.Width,available.Height/banner.Height);
        float bw=banner.Width*scale,bh=banner.Height*scale;
        var dest=new RectangleF(available.X+(available.Width-bw)/2,available.Y+(available.Height-bh)/2,bw,bh);
        using var attributes=new ImageAttributes();attributes.SetWrapMode(WrapMode.TileFlipXY);
        g.DrawImage(banner,Rectangle.Round(dest),0,0,banner.Width,banner.Height,GraphicsUnit.Pixel,attributes);
        float bandY=imageHeight+1;
        using(var divider=new Pen(Theme.CardEdge,1))g.DrawLine(divider,1,bandY,Width-1,bandY);
        var portrait=new RectangleF(24,bandY+19,72,72);
        using(var bg=new SolidBrush(Theme.Card))g.FillEllipse(bg,portrait);
        // Inset photo so the outer ring covers the anti-aliased mask boundary.
        var photo=new RectangleF(portrait.X+1.5f,portrait.Y+1.5f,portrait.Width-3,portrait.Height-3);
        var mask=g.Save();
        using(var circle=new GraphicsPath())
        {
            circle.AddEllipse(photo);g.SetClip(circle,CombineMode.Intersect);
            float avScale=Math.Max(photo.Width/avatar.Width,photo.Height/avatar.Height);
            float aw=avatar.Width*avScale,ah=avatar.Height*avScale;
            var ar=new RectangleF(photo.X+(photo.Width-aw)/2,photo.Y+(photo.Height-ah)/2,aw,ah);
            g.DrawImage(avatar,Rectangle.Round(ar),0,0,avatar.Width,avatar.Height,GraphicsUnit.Pixel,attributes);
        }
        g.Restore(mask);
        using(var ring=new Pen(Theme.Accent,2.5f))g.DrawEllipse(ring,portrait);
        using(var ink=new SolidBrush(Theme.Ink))g.DrawString("2xFlaw",nameFont,ink,112,bandY+19);
        using(var muted=new SolidBrush(Theme.AccentSoft))g.DrawString("PERSONAL CHEST TRACKER  /  REPLAY ANALYTICS",detailFont,muted,114,bandY+64);
        g.Restore(saved);
        using var edge=new Pen(Theme.CardEdge,1);g.DrawPath(edge,shape);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(disposing){nameFont.Dispose();detailFont.Dispose();}
    }
}
