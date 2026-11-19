using System.Text.Json;

namespace ChestCounter;

// Read-only history projection; weekly prize calculations remain in WeeklyChanceModel.
public static class PirateShopChances
{
    public const string Key = "pirate-shop-access";

    private static T Read<T>(string path, Func<T> fallback) where T : class =>
        File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? fallback() : fallback();

    public static ChanceSnapshot Apply(ChanceSnapshot weekly, Store store, Monitor monitor, DateTime now)
    {
        now = now.Kind == DateTimeKind.Local ? now.ToUniversalTime() : DateTime.SpecifyKind(now, DateTimeKind.Utc);
        var config = monitor.Config;
        if (WeeklyReset.MigratePirateOwnership(config, now)) store.SaveConfig(config);
        var cache = PersonalReadCache.Load(store);
        var profile = Profiles.Current(cache, config)?.Key ?? config.Profile ?? "";
        var ledger = Read(Path.Combine(store.Folder, "2xflaw-ledger.json"), () => new PersonalLedger());
        var odds = Read(Path.Combine(store.Folder, "2xflaw-odds.json"), PersonalOdds.Seed);
        var windows = odds.Windows.OrderBy(w => w.StartUtc).ToList();
        var events = new List<(DateTime? Time, int Count)>();
        void Add(DateTime? time, int count) { if (count > 0) events.Add((time, count)); }
        Add(null, Profiles.InitialChests(config, profile));
        var matches = cache.Matches.Concat(ledger.Imports.Select(x => x.Match))
            .Where(m => Profiles.Key(m) == profile && m.IsBattleRoyale && Profiles.Counted(m, config)
                && (m.MatchStartUtc == null || m.MatchStartUtc <= now))
            .GroupBy(m => string.IsNullOrEmpty(m.SessionId) ? $"{m.ReplayFile}|{m.MatchStartUtc:O}" : m.SessionId)
            .Select(g => g.First());
        foreach (var match in matches)
        {
            var total = Math.Max(0, Profiles.Adjust(match, config)?.ChestsOpened ?? 0);
            var stamped = Math.Min(total, match.Chests.Count);
            foreach (var chest in match.Chests.Take(stamped))
            {
                DateTime? time = match.MatchStartUtc?.AddSeconds(chest.TimeSeconds);
                if (time == null || time <= now) Add(time, 1);
            }
            // Estimated starting counts, missing timestamps, extra kinds and teammate contributions use base odds.
            Add(null, total - stamped);
        }
        foreach (var entry in ledger.Entries.Where(e => e.Profile == profile && e.AtUtc <= now
            && (config.CountFrom is not { } from || e.AtUtc.ToLocalTime().Date >= from.Date)))
        {
            Add(entry.AtUtc, Math.Max(0, entry.Mine));
            if (config.CountTeammateChests) Add(null, Math.Max(0, entry.Squad));
        }
        var pirate = weekly.Prizes.First(p => p.Key == Key);
        double noDrop = 1;
        foreach (var entry in events)
        {
            var window = entry.Time is { } time ? windows.FirstOrDefault(w => time >= w.StartUtc && time < w.EndUtc) : null;
            var rate = Math.Clamp(WeeklyChanceModel.BoostFor(window, Key) / pirate.Den, 0, 1);
            noDrop *= Math.Pow(1 - rate, entry.Count);
        }
        var rateNow = Math.Clamp(WeeklyChanceModel.BoostFor(weekly.Live, Key) / pirate.Den, 0, 1);
        var replacement = pirate with
        {
            Name = pirate.Name + " (all time)",
            Chance = 1 - noDrop,
            Next50 = 1 - noDrop * Math.Pow(1 - rateNow, WeeklyChanceModel.Lookahead),
            Owned = config.OwnedRewards.GetValueOrDefault(WeeklyReset.OwnedKey(Key, now)),
            AllTimeChests = events.Sum(e => e.Count),
        };
        return weekly with { Prizes = weekly.Prizes.Select(p => p.Key == Key ? replacement : p).ToList() };
    }
}
