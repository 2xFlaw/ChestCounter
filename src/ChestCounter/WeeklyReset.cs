namespace ChestCounter;

/// <summary>
/// The weekly reset: every Sunday at 14:00 GMT (UTC). All the "week" maths goes through here so the rewards
/// window, the dashboard countdown and Weekly chances always agree.
/// </summary>
public static class WeeklyReset
{
    public static readonly TimeSpan TimeOfDay = TimeSpan.FromHours(14);

    private static DateTime AsUtc(DateTime t) =>
        t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

    /// <summary>Start (UTC) of the weekly period that contains <paramref name="now"/>: the last Sunday 14:00 GMT.</summary>
    public static DateTime Start(DateTime now)
    {
        var utc = AsUtc(now);
        var sunday = utc.Date.AddDays(-(int)utc.DayOfWeek).Add(TimeOfDay);
        return sunday > utc ? sunday.AddDays(-7) : sunday;
    }

    /// <summary>The next reset (UTC): the coming Sunday 14:00 GMT.</summary>
    public static DateTime Next(DateTime now) => Start(now).AddDays(7);

    /// <summary>Time left until the next reset.</summary>
    public static TimeSpan Remaining(DateTime now) => Next(now) - AsUtc(now);

    /// <summary>
    /// Key under which "I own this" is stored. It includes the week, so ticks clear themselves at every reset
    /// (each week has different prizes) and old weeks are simply ignored.
    /// </summary>
    public static string OwnedKey(string rewardKey, DateTime now) => $"{rewardKey}@{Start(now):yyyyMMdd}";
}
