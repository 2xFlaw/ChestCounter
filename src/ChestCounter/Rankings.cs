using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace ChestCounter;

/// <summary>Summary figures the dashboard compares with other players. Never names, IDs, matches or replays.</summary>
internal sealed record RankFigures(int Matches, int Total, int Mates, double Avg, double AvgAll, int Best, int BestAll, double? Pm, double? PmAll);

/// <summary>"Top X%" per figure, and how many players each figure was compared with.</summary>
internal sealed class RankResult
{
    public Dictionary<string, int> Top { get; set; } = new();
    public Dictionary<string, int> Players { get; set; } = new();
    public DateTime AtUtc { get; set; }
}

/// <summary>
/// Anonymous player rankings for the dashboard tiles. Works without linking a PC: the app sends its summary
/// figures under a random install ID and the website answers with the percentile of each figure. When the PC is
/// linked, its token is added so the website does not count the same player twice. Can be turned off on the dashboard.
/// </summary>
internal static class Rankings
{
    private sealed class State
    {
        public string Id { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string? Sent { get; set; }
        public RankResult? Last { get; set; }
    }

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly SemaphoreSlim Busy = new(1, 1);
    private static readonly object Gate = new();
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(30);
    private static State? _state;
    private static string _path = "";

    private static State Load(string folder)
    {
        lock (Gate)
        {
            if (_state != null) return _state;
            _path = Path.Combine(folder, "2xflaw-rankings.json");
            try { _state = File.Exists(_path) ? JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) : null; }
            catch (Exception ex) { ErrorLog.Write("Player rankings", ex); }
            _state ??= new State();
            if (_state.Id.Length != 32) { _state.Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(); Save(); }
            return _state;
        }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(_state));
            File.Move(_path + ".tmp", _path, true);
        }
        catch (Exception ex) { ErrorLog.Write("Player rankings", ex); }
    }

    public static bool Enabled(string folder) => Load(folder).Enabled;

    public static void SetEnabled(string folder, bool on)
    {
        var s = Load(folder);
        lock (Gate) { s.Enabled = on; s.Sent = null; if (!on) s.Last = null; Save(); }
        if (!on) _ = Forget(s.Id);
    }

    /// <summary>Turned off: asks the website to delete this player's figures (best effort; old ones expire anyway).</summary>
    private static async Task Forget(string id)
    {
        try
        {
            var online = OnlineSync.Load();
            using var req = new HttpRequestMessage(HttpMethod.Delete, online.ApiBase.TrimEnd('/') + "/api/benchmarks") { Content = JsonContent.Create(new { id }) };
            if (!string.IsNullOrEmpty(online.Token) && online.Token.Contains('.'))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", online.Token);
            using var resp = await Http.SendAsync(req);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or InvalidOperationException)
        {
        }
    }

    /// <summary>The last answer from the website (kept on disk, so the colours show straight away and offline).</summary>
    public static RankResult? Cached(string folder) => Load(folder) is { Enabled: true } s ? s.Last : null;

    /// <summary>True when these figures still need sending (they changed, or the last answer is getting old).</summary>
    public static bool Due(string folder, RankFigures f)
    {
        var s = Load(folder);
        return s.Enabled && (s.Sent != Key(f) || s.Last == null || DateTime.UtcNow - s.Last.AtUtc > Fresh);
    }

    private static string Key(RankFigures f) => JsonSerializer.Serialize(f);

    /// <summary>Sends the figures and returns the new rankings, or null when it is off, busy or offline.</summary>
    public static async Task<RankResult?> Fetch(string folder, RankFigures f)
    {
        var s = Load(folder);
        if (!s.Enabled || !await Busy.WaitAsync(0)) return null;
        try
        {
            var online = OnlineSync.Load();
            var body = new
            {
                id = s.Id, matches = f.Matches, total = f.Total, mates = f.Mates,
                avg = Math.Round(f.Avg, 3), avgAll = Math.Round(f.AvgAll, 3), best = f.Best, bestAll = f.BestAll,
                pm = f.Pm is double pm ? Math.Round(pm, 3) : (double?)null,
                pmAll = f.PmAll is double pa ? Math.Round(pa, 3) : (double?)null,
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, online.ApiBase.TrimEnd('/') + "/api/benchmarks") { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(online.Token) && online.Token.Contains('.'))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", online.Token);
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<RankResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (result == null) return null;
            result.AtUtc = DateTime.UtcNow;
            lock (Gate)
            {
                if (!s.Enabled) return null;
                s.Last = result; s.Sent = Key(f); Save();
            }
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return null;
        }
        finally { Busy.Release(); }
    }
}
