using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>
/// Full-colour emoji pictures built into the exe (Noto Color Emoji artwork, Apache License 2.0), so they look the same
/// on every PC instead of depending on the Windows emoji font, which draws them flat and grey in GDI.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Emoji
{
    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    public static Bitmap? Get(string key)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            Bitmap? bitmap = null;
            try
            {
                using var stream = typeof(Emoji).Assembly.GetManifestResourceStream("ChestCounter.Assets.emoji." + key + ".png");
                if (stream != null)
                {
                    using var source = new Bitmap(stream);
                    bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
                    using var g = Graphics.FromImage(bitmap);
                    g.DrawImage(source, 0, 0, source.Width, source.Height);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Emoji " + key, ex);
                bitmap = null;
            }
            Cache[key] = bitmap;
            return bitmap;
        }
    }

    /// <summary>Draws the emoji in the box (kept square and centred). Opacity 0-1. Does nothing when it is missing.</summary>
    public static void Draw(Graphics g, string key, RectangleF box, float opacity = 1f)
    {
        var image = Get(key);
        if (image == null || box.Width < 2 || box.Height < 2) return;
        var size = Math.Min(box.Width, box.Height);
        var target = new RectangleF(box.X + (box.Width - size) / 2, box.Y + (box.Height - size) / 2, size, size);
        var state = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        if (opacity >= 0.999f)
        {
            g.DrawImage(image, target);
        }
        else
        {
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Clamp(opacity, 0f, 1f) });
            g.DrawImage(image, Rectangle.Round(target), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
        }
        g.Restore(state);
    }
}
