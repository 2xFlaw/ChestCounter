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
