namespace ChestCounter;

/// <summary>
/// Facts about the Treasure Hours. The schedule itself lives in 2xflaw-odds.json; this class only knows which
/// Treasure Hours have boosts that are not announced yet.
/// </summary>
public static class TreasureHours
{
    /// <summary>
    /// Treasure Hours that start on or after this time are confirmed but have no announced boosts, so they count as
    /// normal odds and show a grey question mark. The Nov 28 and Nov 29 Treasure Hours now use the same boosts as the
    /// others, so this is set far in the future (year 2100) and nothing is unconfirmed. To mark a future event as
    /// not announced again, move this date back to the start of that event.
    /// </summary>
    public static readonly DateTime UnconfirmedFromUtc = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static bool IsUnconfirmed(TreasureWindow w) => w.StartUtc >= UnconfirmedFromUtc;

    // ---- Golden Week ---------------------------------------------------------------------------------------------
    // Announced by Epic: from Nov 28 every week's treasure rewards are back in chests and, until the season ends,
    // Gold Outfit odds are 2x (1 in 1,562,500 -> 1 in 781,250) and Gold Vehicle odds 5x (1 in 7,813 -> 1 in 1,563).
    // Pickaxe/Back Bling, Jam Track, Loading Screen, Gizmo and Pirate Shop Access keep their normal odds.

    /// <summary>Golden Week start (the Nov 28 update, 14:00 UTC like the other weekly changes).</summary>
    public static readonly DateTime GoldenWeekStartUtc = new(2026, 11, 28, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>Golden Week end: the Chapter 7 end event on Dec 5, after which chests stop giving treasure.</summary>
    public static readonly DateTime GoldenWeekEndUtc = new(2026, 12, 5, 19, 0, 0, DateTimeKind.Utc);

    public static bool IsGoldenWeek(DateTime utc) => utc >= GoldenWeekStartUtc && utc < GoldenWeekEndUtc;

    /// <summary>Golden Week multiplier on the normal odds of a prize at a moment (1 outside Golden Week).</summary>
    public static double Golden(DateTime? utc, string key) =>
        utc is { } t && IsGoldenWeek(t) ? key switch { "gold-outfit" => 2, "vehicle-decal" => 5, _ => 1 } : 1;

    /// <summary>
    /// Multiplier on the normal odds at a moment: the Treasure Hour boost inside a Treasure Hour (announced against
    /// the normal odds, so it already includes Golden Week), otherwise the Golden Week multiplier, otherwise 1.
    /// </summary>
    public static double BoostAt(TreasureWindow? w, DateTime? utc, string key) =>
        w != null && !IsUnconfirmed(w) ? Boost(w, key) : Golden(utc, key);

    // ---- Announced Treasure Hour boosts ----------------------------------------------------------------------------
    // Treasure Hours whose boosts Epic announced differently from the usual 7.5x / 1.5x / 10x. These win over the
    // numbers saved in 2xflaw-odds.json, so players with an older file get the right odds without editing it.
    private static readonly Dictionary<DateTime, (double Gold, double Shop, double Vehicle)> Announced = new()
    {
        // Golden Week Treasure Hour (Nov 28, both sessions): Gold Outfit 1 in 104,167 (15x),
        // Vehicle & Decal 1 in 781 (10x), Pirate Shop Access 1 in 91,667 (1.5x).
        [new DateTime(2026, 11, 28, 19, 0, 0, DateTimeKind.Utc)] = (15, 1.5, 10),
        [new DateTime(2026, 11, 29, 2, 0, 0, DateTimeKind.Utc)] = (15, 1.5, 10),
    };

    public static (double Gold, double Shop, double Vehicle)? AnnouncedBoosts(DateTime startUtc) =>
        Announced.TryGetValue(startUtc.ToUniversalTime(), out var b) ? b : null;

    /// <summary>Boost a Treasure Hour gives to a prize: 1 when there is none or when it is not announced.</summary>
    public static double Boost(TreasureWindow? w, string key)
    {
        if (w == null || IsUnconfirmed(w))
        {
            return 1;
        }
        return key switch
        {
            "gold-outfit" => w.Gold,
            "pirate-shop-access" => w.Shop,
            "vehicle-decal" => w.Vehicle,
            _ => 1,
        };
    }
}
