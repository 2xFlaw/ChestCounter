using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>
/// The app's typefaces, embedded in the exe (Assets\fonts\*.ttf) so every player sees the same text:
/// Onest for body text (Medium for normal text, Bold for emphasis) and Anton for big titles and numbers. Onest is a
/// friendly, modern UI typeface with a tall x-height, so small text stays easy to read; it also covers Cyrillic and
/// the accented Latin letters every app language needs. If a font cannot be loaded, Segoe UI is used instead.
/// Both embedded fonts are open source (SIL Open Font License).
/// On a PC that already has Burbank installed, big titles use it instead; it is never embedded or shipped.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Fonts
{
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

    private static readonly PrivateFontCollection Names = new();
    private static string? _body;      // normal text (Onest Medium)
    private static string? _bodyBold;  // bold text (Onest, Bold style)
    private static string? _bodySemi;  // semi-bold text (Onest SemiBold)
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
            // Embedded fonts could not be loaded: the names below may still resolve (installed fonts), else Segoe UI.
        }

        // Resolved by name, so this works for the embedded copies and for fonts installed on the PC alike.
        _bodyBold = Check("Onest", FontStyle.Bold);
        _body = Check("Onest Medium", FontStyle.Regular);
        _bodySemi = Check("Onest SemiBold", FontStyle.Regular);
        if (_body == null || _bodyBold == null)
        {
            _body = _bodyBold = _bodySemi = null;
        }
        _display ??= Check("Anton", FontStyle.Regular);

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
            if (Private(name) is { } family && family.IsStyleAvailable(style))
            {
                return name;
            }
            using var probe = new Font(name, 10f, style);
            return string.Equals(probe.Name, name, StringComparison.OrdinalIgnoreCase) ? name : null;
        }
        catch
        {
            return null;
        }
    }

    private static FontFamily? Private(string name)
    {
        try
        {
            return Names.Families.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    // Embedded fonts are built from their own family (always the right file); anything else by name.
    private static Font Make(string name, float size, FontStyle style, GraphicsUnit unit = GraphicsUnit.Point) =>
        Private(name) is { } family && family.IsStyleAvailable(style)
            ? new Font(family, size, style, unit)
            : new Font(name, size, style, unit);

    public static bool HasBody => _body != null;

    public static bool HasDisplay => _display != null;

    // Onest is a little wider and taller than the old body font at the same point size, so every size is scaled down
    // slightly: the text keeps fitting the same boxes, and its larger x-height still makes it read bigger and clearer.
    private const float BodyScale = 0.95f;

    /// <summary>Body text: Onest Medium (Onest Bold when bold), or Segoe UI as a fallback.</summary>
    public static Font Body(float size, FontStyle style = FontStyle.Regular)
    {
        if (_body == null || _bodyBold == null)
        {
            return new Font("Segoe UI", size, style);
        }
        return style.HasFlag(FontStyle.Bold)
            ? Make(_bodyBold, size * BodyScale, style)
            : Make(_body, size * BodyScale, style);
    }

    /// <summary>The same font in another style, keeping Onest's real bold face (not a synthesised one).</summary>
    public static Font WithStyle(Font font, FontStyle style)
    {
        if (_body == null || _bodyBold == null || !(font.Name == _body || font.Name == _bodyBold || font.Name == _bodySemi))
        {
            return new Font(font, style);
        }
        return Make(style.HasFlag(FontStyle.Bold) ? _bodyBold : _body, font.Size, style, font.Unit);
    }

    /// <summary>Semi-bold body text (row titles, labels): Onest SemiBold, or bold as a fallback.</summary>
    public static Font Semi(float size) =>
        _bodySemi != null ? Make(_bodySemi, size * BodyScale, FontStyle.Regular) : Body(size, FontStyle.Bold);

    /// <summary>Big titles and numbers: Anton (or Burbank when installed), or bold Segoe UI as a fallback.</summary>
    public static Font Display(float size) =>
        _display != null ? Make(_display, size, _displayStyle) : new Font("Segoe UI", size * 0.92f, FontStyle.Bold);

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
