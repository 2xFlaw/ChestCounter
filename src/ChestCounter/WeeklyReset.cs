using System.Globalization;

namespace ChestCounter;

/// <summary>Sunday 14:00 UTC reset for weekly prizes; Pirate Shop ownership is permanent.</summary>
public static class WeeklyReset
{
    public static readonly TimeSpan TimeOfDay = TimeSpan.FromHours(14);

    private static DateTime AsUtc(DateTime t) =>
        t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

    public static DateTime Start(DateTime now)
    {
        var utc = AsUtc(now);
        var sunday = utc.Date.AddDays(-(int)utc.DayOfWeek).Add(TimeOfDay);
        return sunday > utc ? sunday.AddDays(-7) : sunday;
    }

    public static DateTime Next(DateTime now) => Start(now).AddDays(7);
    public static TimeSpan Remaining(DateTime now) => Next(now) - AsUtc(now);

    /// <summary>Only Pirate Shop uses an all-time key; other rewards retain their existing weekly keys.</summary>
    public static string OwnedKey(string rewardKey, DateTime now) => rewardKey == PirateShopChances.Key
        ? rewardKey + "@all-time" : $"{rewardKey}@{Start(now):yyyyMMdd}";

    /// <summary>Migrate the latest dated Pirate Shop choice, including an explicit unchecked state, once.</summary>
    public static bool MigratePirateOwnership(Config config, DateTime now)
    {
        var stable = OwnedKey(PirateShopChances.Key, now);
        if (config.OwnedRewards.ContainsKey(stable)) return false;
        var prefix = PirateShopChances.Key + "@";
        var legacy = config.OwnedRewards.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)
            && DateTime.TryParseExact(pair.Key.Substring(prefix.Length), "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) && date.Date <= AsUtc(now).Date)
            .OrderByDescending(pair => pair.Key, StringComparer.Ordinal).ToList();
        if (legacy.Count == 0) return false;
        config.OwnedRewards[stable] = legacy[0].Value;
        return true;
    }
}
