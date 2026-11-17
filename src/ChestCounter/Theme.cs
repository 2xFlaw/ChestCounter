using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>2xFlaw dark-plum palette with sampled magenta accents; compatibility field names retained.</summary>
[SupportedOSPlatform("windows")]
internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(14, 9, 18);        // fondo
    public static readonly Color Card = Color.FromArgb(25, 15, 30);      // tarjetas
    public static readonly Color CardEdge = Color.FromArgb(69, 28, 60);  // borde de tarjeta
    public static readonly Color Panel = Color.FromArgb(43, 23, 45);     // botones y campos
    public static readonly Color PanelHover = Color.FromArgb(70, 28, 61);
    public static readonly Color Line = Color.FromArgb(83, 39, 72);
    public static readonly Color Ink = Color.FromArgb(249, 236, 246);
    public static readonly Color Muted = Color.FromArgb(183, 155, 178);
    public static readonly Color Cyan = Color.FromArgb(208, 0, 128);    // cofres en total
    public static readonly Color Amber = Color.FromArgb(255, 127, 180);   // partida actual, cofres
    public static readonly Color Mint = Color.FromArgb(61, 220, 151);    // en directo
    public static readonly Color Accent = Color.FromArgb(208, 0, 128);  // acciones principales (cian)
    public static readonly Color AccentHover = Color.FromArgb(240, 41, 154);
    public static readonly Color AccentInk = Color.FromArgb(255, 244, 251);   // texto sobre el cian
    public static readonly Color AccentSoft = Color.FromArgb(255, 164, 212); // avisos
    public static readonly Color Blue = Color.FromArgb(160, 0, 96);    // barras del gráfico
    public static readonly Color Rose = Color.FromArgb(255, 101, 183);   // tesoros
    public static readonly Color Coral = Color.FromArgb(255, 99, 99);    // desactivado

    // Nombres usados por otras ventanas (mismo significado que antes).
    public static Color Gold => Amber;
    public static Color Green => Mint;
    public static Color Red => Coral;

public static Font Text(float size = 9f, FontStyle style = FontStyle.Regular) => Fonts.Body(size, style);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Barra de título oscura y esquinas redondeadas de Windows 11 (en versiones antiguas se ignora).
    /// </summary>
    public static void Chrome(IntPtr hwnd)
    {
        try
        {
            int on = 1;
            DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int)); // nombre anterior de la misma opción
            int round = 2;                                        // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE
        }
        catch
        {
            // sin soporte: la ventana queda con el estilo normal
        }
    }

    /// <summary>Botón plano con el estilo de la aplicación (usado en ajustes).</summary>
    public static void Style(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Panel;
        b.ForeColor = Ink;
        b.Font = Text(9f);
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderColor = Line;
        b.FlatAppearance.MouseOverBackColor = PanelHover;
        b.FlatAppearance.MouseDownBackColor = Line;
    }
}
