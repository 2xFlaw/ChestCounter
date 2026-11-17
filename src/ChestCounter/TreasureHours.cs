namespace ChestCounter;

/// <summary>
/// Facts about the Treasure Hours. The schedule itself lives in 2xflaw-odds.json; this class only knows which
/// Treasure Hours have boosts that are not announced yet.
/// </summary>
public static class TreasureHours
{
    /// <summary>
    /// The last two Treasure Hours (Sat 28 Nov 19:00-21:00 and Sun 29 Nov 02:00-04:00 GMT) are confirmed, but what they
    /// boost is not announced. Until it is, they count as normal odds and show a grey question mark.
    /// When the boosts are announced, move this date past the end of the event (for example to year 2100).
    /// </summary>
    public static readonly DateTime UnconfirmedFromUtc = new(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc);

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
