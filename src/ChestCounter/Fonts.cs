using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>
/// The app's typefaces, embedded in the exe (Assets\fonts\*.ttf) so every player sees the same text:
/// Inter for body text and Anton for big titles and numbers. If a font cannot be loaded, Segoe UI is used instead.
/// Both embedded fonts are open source (SIL Open Font License).
/// On a PC that already has Burbank installed, big titles use it instead; it is never embedded or shipped.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Fonts
{
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

    private static readonly PrivateFontCollection Names = new();
    private static string? _body;
    private static string? _display;
    private static FontStyle _displayStyle = FontStyle.Regular;
    private static bool _loaded;

    /// <summary>Call once at startup, before the first window is created.</summary>
    public static void Load()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;
        try
        {
            var assembly = typeof(Fonts).Assembly;
            foreach (var resource in assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("ChestCounter.Assets.fonts.", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream == null)
                {
                    continue;
                }
                var data = new byte[stream.Length];
                stream.ReadExactly(data);

                // This memory must stay alive for the whole run, so it is never freed.
                var pointer = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, pointer, data.Length);
                uint count = 0;
                AddFontMemResourceEx(pointer, (uint)data.Length, IntPtr.Zero, ref count);
                Names.AddMemoryFont(pointer, data.Length);
            }

            _body = Check(Names.Families.FirstOrDefault(f => f.Name.StartsWith("Inter", StringComparison.OrdinalIgnoreCase))?.Name, FontStyle.Regular);
            _display = Check(Names.Families.FirstOrDefault(f => f.Name.StartsWith("Anton", StringComparison.OrdinalIgnoreCase))?.Name, FontStyle.Regular);
var burbank = Names.Families.FirstOrDefault(f => f.Name.StartsWith("Burbank", StringComparison.OrdinalIgnoreCase));
if (burbank != null)
{
    foreach (var style in new[] { FontStyle.Bold, FontStyle.Regular })
    {
        if (Check(burbank.Name, style) is { } found)
        {
            _display = found;
            _displayStyle = style;
            break;
        }
    }
}
        }
        catch
        {
            _body = null;
            _display = null;
        }

        // Only on a PC that has Burbank installed (nothing is shipped): use it for the big titles.
        foreach (var (name, style) in new[]
        {
            ("Burbank Big Condensed", FontStyle.Bold),
            ("Burbank Big Regular", FontStyle.Bold),
            ("Burbank Big Regular Bold", FontStyle.Regular),
        })
        {
            if (Check(name, style) is { } installed)
            {
                _display = installed;
                _displayStyle = style;
                break;
            }
        }
    }

    /// <summary>Only keep a font name if Windows really resolves it (otherwise text would silently use a default font).</summary>
    private static string? Check(string? name, FontStyle style)
    {
        if (name == null)
        {
            return null;
        }
        try
        {
            using var probe = new Font(name, 10f, style);
            return string.Equals(probe.Name, name, StringComparison.OrdinalIgnoreCase) ? name : null;
        }
        catch
        {
            return null;
        }
    }

    public static bool HasBody => _body != null;

    public static bool HasDisplay => _display != null;

    /// <summary>Body text: Inter, or Segoe UI as a fallback.</summary>
    public static Font Body(float size, FontStyle style = FontStyle.Regular) =>
        new(_body ?? "Segoe UI", size, style);

    /// <summary>Big titles and numbers: Anton (or Burbank when installed), or bold Segoe UI as a fallback.</summary>
    public static Font Display(float size) =>
        _display != null ? new Font(_display, size, _displayStyle) : new Font("Segoe UI", size * 0.92f, FontStyle.Bold);

    /// <summary>Wide-spaced capitals for small labels (the website style): puts a hair space between the letters.</summary>
    public static string Track(string text)
    {
        if (_body == null || text.Length < 2)
        {
            return text;
        }
        var builder = new System.Text.StringBuilder(text.Length * 2);
        for (var i = 0; i < text.Length; i++)
        {
            builder.Append(text[i]);
            if (i < text.Length - 1 && text[i] != ' ' && text[i + 1] != ' ')
            {
                builder.Append('\u200A');
            }
        }
        return builder.ToString();
    }
}
