using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// The chance that a single chest gives any Gold Outfit you still need or the Pirate Shop Access token, during
/// Golden Week: once at the Golden Week rate and once inside the Golden Week Treasure Hour. Both prizes are separate
/// rolls with tiny rates, so "either one" is the sum of their rates (the same way Epic adds up "any of five" items).
/// With all five Gold Outfits and the token needed that is 1 in 73,138 per chest, and 1 in 16,975 in the Treasure Hour.
/// </summary>
public sealed record JackpotOdds(int GoldLeft, int GoldTotal, bool PirateFound, double Rate, double HourRate, double HourGold, double HourShop)
{
    public bool AllFound => GoldLeft == 0 && PirateFound;

    // Normal per-chest odds (Epic): one Gold Outfit 1 in 1,562,500, Pirate Shop Access 1 in 137,500.
    private const double GoldDen = 1562500;
    private const double PirateDen = 137500;

    /// <summary>Null once Golden Week is over (chests stop giving treasure after the end event).</summary>
    public static JackpotOdds? Compute(IReadOnlyList<HuntWeek> weeks, IReadOnlyDictionary<string, bool> owned, TreasureWindow? live, DateTime now)
    {
        if (now >= TreasureHours.GoldenWeekEndUtc)
        {
            return null;
        }
        // Before it starts, show what Golden Week will bring: the pool as it will be at the start.
        var at = now < TreasureHours.GoldenWeekStartUtc ? TreasureHours.GoldenWeekStartUtc : now;
        var pool = TreasureHours.Pool(weeks, owned, "gold-outfit", at);
        var pirateFound = owned.TryGetValue(WeeklyReset.OwnedKey("pirate-shop-access", now), out var v) && v;

        // The Golden Week Treasure Hour boosts, announced against the normal odds (so they already include the 2x).
        // A live Treasure Hour during Golden Week uses its own numbers, in case a later one is announced differently.
        double gold = 15, shop = 1.5;
        if (live != null && TreasureHours.IsGoldenWeek(now) && !TreasureHours.IsUnconfirmed(live))
        {
            gold = TreasureHours.Boost(live, "gold-outfit");
            shop = TreasureHours.Boost(live, "pirate-shop-access");
        }
        else if (TreasureHours.AnnouncedBoosts(new DateTime(2026, 11, 28, 19, 0, 0, DateTimeKind.Utc)) is { } announced)
        {
            gold = announced.Gold;
            shop = announced.Shop;
        }

        var pirate = pirateFound ? 0 : 1 / PirateDen;
        var rate = pool.Left * TreasureHours.Golden(TreasureHours.GoldenWeekStartUtc, "gold-outfit") / GoldDen + pirate;
        var hourRate = pool.Left * gold / GoldDen + pirate * shop;
        return new JackpotOdds(pool.Left, pool.Total, pirateFound, rate, hourRate, gold, shop);
    }
}

[SupportedOSPlatform("windows")]
internal sealed class JackpotPanel : Control
{
    private static readonly Color Gold = Color.FromArgb(255, 199, 92);

    private readonly Font _label = Fonts.Body(7.5f, FontStyle.Bold);
    private readonly Font _title = Fonts.Display(17f);
    private readonly Font _sub = Theme.Text(9f);
    private readonly Font _small = Theme.Text(8.5f);
    private readonly Font _value = Fonts.Display(21f);
    private JackpotOdds? _odds;
    private Bitmap? _goldPicture;
    private Bitmap? _piratePicture;

    public JackpotPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Height = WantedHeight;
    }

    public const int WantedHeight = 172;

    public void SetState(JackpotOdds? odds, Bitmap? gold, Bitmap? pirate)
    {
        _odds = odds;
        _goldPicture = gold;
        _piratePicture = pirate;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 300 || _odds is not { } odds)
        {
            return;
        }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var card = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var path = Shape.Rounded(card, 18))
        using (var fill = new LinearGradientBrush(card, Color.FromArgb(40, 28, 22), Theme.Card, LinearGradientMode.Horizontal))
        using (var edge = new Pen(Color.FromArgb(150, Gold), 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        // Two prize pictures, slightly overlapping.
        const int pic = 64;
        if (_goldPicture != null)
        {
            g.DrawImage(_goldPicture, new Rectangle(20, 22, pic, pic));
        }
        if (_piratePicture != null)
        {
            g.DrawImage(_piratePicture, new Rectangle(20 + pic - 14, 22 + pic - 30, pic - 6, pic - 6));
        }

        const int tile = 200;
        const int gap = 12;
        var tileX2 = Width - 20 - tile;
        var tileX1 = tileX2 - gap - tile;
        var textX = 20 + pic * 2 - 2;
        var textW = Math.Max(120, tileX1 - 16 - textX);

        ChanceText.ChipAt(g, WeeklyChanceMore.T("GOLDEN WEEK"), _label, textX, 18, Gold);
        ChanceText.Draw(g, WeeklyChanceMore.T("GOLD OUTFIT OR PIRATE TOKEN"), _title, new Rectangle(textX, 44, textW, 30), Theme.Ink);
        TextRenderer.DrawText(g, WeeklyChanceMore.T("Your chance that one chest gives any Gold Outfit you still need or the Pirate Shop Access token."), _sub,
            new Rectangle(textX, 78, textW, 54), Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        var status = WeeklyChanceMore.F("{0} of {1} Gold Outfits left", odds.GoldLeft, odds.GoldTotal) + "   \u00b7   " +
            (odds.PirateFound ? WeeklyChanceMore.T("Pirate token found") : WeeklyChanceMore.T("Pirate token still to find"));
        ChanceText.Draw(g, status, _small, new Rectangle(20, Height - 32, tileX1 - 36, 20), Theme.Muted);

        if (odds.AllFound)
        {
            var box = new Rectangle(tileX1, 22, tile * 2 + gap, Height - 44);
            Tile(g, box, Color.FromArgb(61, 220, 151));
            ChanceText.Draw(g, WeeklyChanceMore.T("You have every Gold Outfit and the Pirate token"), _sub, box, Theme.Mint, TextFormatFlags.HorizontalCenter);
            return;
        }

        DrawOdds(g, new Rectangle(tileX1, 22, tile, Height - 44), WeeklyChanceMore.T("ANY CHEST"), odds.Rate, Theme.Ink, Theme.CardEdge);
        DrawOdds(g, new Rectangle(tileX2, 22, tile, Height - 44), WeeklyChanceMore.F("TREASURE HOUR {0:0.#}\u00d7", odds.HourGold), odds.HourRate, Gold, Color.FromArgb(150, Gold));
    }

    private static void Tile(Graphics g, Rectangle box, Color edge)
    {
        using var path = Shape.Rounded(box, 14);
        using var fill = new SolidBrush(Color.FromArgb(120, Theme.Bg));
        using var pen = new Pen(edge, 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }

    private void DrawOdds(Graphics g, Rectangle box, string label, double rate, Color colour, Color edge)
    {
        Tile(g, box, edge);
        var inner = Rectangle.Inflate(box, -16, 0);
        ChanceText.Draw(g, Fonts.Track(label.ToUpper(L.Culture)), _label, new Rectangle(inner.X, box.Y + 14, inner.Width, 18), colour == Theme.Ink ? Theme.Muted : colour);
        var den = rate > 0 ? 1 / rate : 0;
        ChanceText.Draw(g, WeeklyChanceMore.F("1 in {0:N0}", Math.Round(den)), _value, new Rectangle(inner.X, box.Y + 36, inner.Width, 38), colour);
        ChanceText.Draw(g, WeeklyChanceMore.F("{0} per chest", ChanceText.OddsPct(rate)), _small, new Rectangle(inner.X, box.Y + 78, inner.Width, 20), Theme.Muted);
    }
}
