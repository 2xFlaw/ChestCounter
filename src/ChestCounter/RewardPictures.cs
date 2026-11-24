using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>Loads the reward pictures as square bitmaps with rounded corners (drawn at 2x for sharpness).</summary>
[SupportedOSPlatform("windows")]
public static class RewardPictures
{
    public static readonly (string Key, string Name)[] All =
    {
        ("gold-outfit", "Weekly Gold Outfit"),
        ("pirate-shop-access", "Pirate Shop Access"),
        ("vehicle-decal", "Weekly Vehicle & Decal"),
        ("pickaxe-back-bling", "Weekly Pickaxe or Back Bling"),
        ("jam-track", "Weekly Jam Track"),
        ("loading-screen", "Weekly Loading Screen"),
    };

    /// <summary>
    /// A picture in the rewards folder (png/jpg, same name as the key) wins; otherwise the one built into the exe;
    /// otherwise a neutral placeholder. Never returns null. The caller owns (and disposes) the bitmap.
    /// </summary>
    public static Bitmap Load(string folder, string key, int size, int radius)
    {
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
        {
            var path = Path.Combine(folder, key + extension);
            if (!File.Exists(path))
            {
                continue;
            }
            try
            {
                return Render(File.ReadAllBytes(path), size, radius);
            }
            catch
            {
                // unreadable: try the built-in one
            }
        }

        // This week's Treasure Hunt picture for the slot (the rewards folder sits inside the app's data folder).
        try
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(folder));
            if (parent != null && TreasureHunt.CurrentBytes(parent, key) is { } current)
            {
                return Render(current, size, radius);
            }
        }
        catch
        {
            // fall back to the built-in slot picture
        }

        try
        {
            var assembly = typeof(RewardPictures).Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n =>
                n.EndsWith("." + key + ".png", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith("." + key + ".jpg", StringComparison.OrdinalIgnoreCase));
            if (name != null)
            {
                using var stream = assembly.GetManifestResourceStream(name);
                if (stream != null)
                {
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    return Render(buffer.ToArray(), size, radius);
                }
            }
        }
        catch
        {
            // fall through to the placeholder
        }

        return Placeholder(size, radius);
    }

    private static Bitmap Placeholder(int size, int radius)
    {
        var s = size * 2;
        var bitmap = new Bitmap(s, s);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using (var path = Shape.Rounded(new RectangleF(0, 0, s, s), radius * 2))
        using (var fill = new SolidBrush(Theme.Panel))
        {
            g.FillPath(fill, path);
        }
        using var font = new Font("Segoe UI", s * 0.42f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Theme.Line);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("\u2605", font, brush, new RectangleF(0, 0, s, s), format);
        return bitmap;
    }

    private static Bitmap Render(byte[] data, int size, int radius)
    {
        using var raw = Image.FromStream(new MemoryStream(data));
        using var source = new Bitmap(raw);
        var s = size * 2;

        var opaque = source.GetPixel(0, 0).A == 255
            && source.GetPixel(source.Width - 1, 0).A == 255
            && source.GetPixel(0, source.Height - 1).A == 255
            && source.GetPixel(source.Width - 1, source.Height - 1).A == 255;

        var result = new Bitmap(s, s);
        using var g = Graphics.FromImage(result);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (opaque)
        {
            // Square crop from the centre, rounded corners.
            var side = Math.Min(source.Width, source.Height);
            var src = new RectangleF((source.Width - side) / 2f, (source.Height - side) / 2f, side, side);
            using var clip = Shape.Rounded(new RectangleF(0, 0, s, s), radius * 2);
            g.SetClip(clip);
            g.DrawImage(source, new RectangleF(0, 0, s, s), src, GraphicsUnit.Pixel);
            return result;
        }

        // Transparent cut-out: keep its shape, centred inside the square.
        var scale = Math.Min(s / (double)source.Width, s / (double)source.Height);
        var w = (int)(source.Width * scale);
        var h = (int)(source.Height * scale);
        g.DrawImage(source, (s - w) / 2, (s - h) / 2, w, h);
        return result;
    }
}
