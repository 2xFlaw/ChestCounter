using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChestCounter;

/// <summary>Online sync settings, kept in their own file (2xflaw-online.json) so config.json is untouched.</summary>
public sealed class OnlineSettings
{
    /// <summary>Off by default. Nothing is sent until the player links this PC and turns it on.</summary>
    public bool Enabled { get; set; }
    public string? Token { get; set; }
    public string ApiBase { get; set; } = "https://chestcounter.io";
    /// <summary>Only matches that started on or after this time are uploaded (null = also the existing history).</summary>
    public DateTime? SinceUtc { get; set; }
    public List<string> Synced { get; set; } = new();
    public DateTime? LastSyncUtc { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Optional upload of finished matches to the website. Sends only: match id, start time, mode, chest counts,
/// chest types and chest times. Never player names, Epic ids, teammates or replay files.
/// Runs on its own low-priority thread, one small request at most once a minute, 10 s timeout.
/// </summary>
public static class OnlineSync
{
    private static readonly object Gate = new();
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions Camel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private static string folder = "";
    private static Thread? worker;
    private static volatile bool wake;

    private static string FilePath => Path.Combine(folder, "2xflaw-online.json");

    /// <summary>Called when the data folder is bound. Starts the thread only if sync is already on.</summary>
    public static void Start(string path)
    {
        lock (Gate)
        {
            folder = path;
        }
        if (File.Exists(FilePath) && Load().Enabled)
        {
            EnsureThread();
        }
    }

    private static void EnsureThread()
    {
        lock (Gate)
        {
            if (worker != null)
            {
                return;
            }
            worker = new Thread(Loop) { IsBackground = true, Name = "Online sync", Priority = ThreadPriority.BelowNormal };
            worker.Start();
        }
    }

    public static OnlineSettings Load()
    {
        lock (Gate)
        {
            try
            {
                return File.Exists(FilePath) ? JsonSerializer.Deserialize<OnlineSettings>(File.ReadAllText(FilePath)) ?? new() : new();
            }
            catch
            {
                return new();
            }
        }
    }

    private static void Save(OnlineSettings s)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(folder);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(s, Pretty));
            File.Move(tmp, FilePath, overwrite: true);
        }
    }

    private static void Update(Action<OnlineSettings> change)
    {
        lock (Gate)
        {
            var s = Load();
            change(s);
            Save(s);
        }
    }

    public static bool IsLinked => !string.IsNullOrEmpty(Load().Token);

    public static void SetEnabled(bool on)
    {
        Update(s =>
        {
            s.Enabled = on && !string.IsNullOrEmpty(s.Token);
        });
        if (on)
        {
            EnsureThread();
            wake = true;
        }
    }

    /// <summary>Forgets the link on this PC and stops uploading. Data already on the website is deleted from the website.</summary>
    public static void Unlink() => Update(s =>
    {
        s.Enabled = false;
        s.Token = null;
        s.Synced.Clear();
    });

    private static string ErrorOf(JsonElement json, HttpResponseMessage resp) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty("error", out var e) ? e.GetString() ?? $"HTTP {(int)resp.StatusCode}" : $"HTTP {(int)resp.StatusCode}";

    private static void SaveToken(string? token, bool includeHistory)
    {
        Update(x =>
        {
            x.Token = token;
            x.Enabled = true;
            x.SinceUtc = includeHistory ? null : DateTime.UtcNow;
            x.Synced.Clear();
            x.LastError = null;
        });
        EnsureThread();
        wake = true;
    }

    private static void OpenBrowser(string url, string apiBase)
    {
        if (!url.StartsWith(apiBase + "/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to open an unexpected address.");
        }
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    /// <summary>Exchanges the one-time code from the website for a token. Returns an error message, or null on success.</summary>
    public static async Task<string?> LinkAsync(string code, bool includeHistory)
    {
        try
        {
            var s = Load();
            using var resp = await Http.PostAsJsonAsync(s.ApiBase.TrimEnd('/') + "/api/link/claim", new { code = code.Trim(), optIn = true }, Camel);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (!resp.IsSuccessStatusCode)
            {
                return ErrorOf(json, resp);
            }
            SaveToken(json.GetProperty("token").GetString(), includeHistory);
            return null;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Online link", ex);
            return ex.Message;
        }
    }

    /// <summary>
    /// Opens the website so the player can approve this PC with one click, then picks up the device token automatically.
    /// The private secret never leaves this PC; only the public code is put in the browser address.
    /// </summary>
    public static async Task<string?> StartLinkAsync(bool includeHistory)
    {
        try
        {
            var baseUrl = Load().ApiBase.TrimEnd('/');
            using var startResp = await Http.PostAsync(baseUrl + "/api/pc/pair/start", null);
            var start = await startResp.Content.ReadFromJsonAsync<JsonElement>();
            if (!startResp.IsSuccessStatusCode)
            {
                return ErrorOf(start, startResp);
            }
            var code = start.GetProperty("code").GetString()!;
            var secret = start.GetProperty("secret").GetString()!;
            OpenBrowser(baseUrl + "/link.html?code=" + Uri.EscapeDataString(code), baseUrl);

            var deadline = DateTime.UtcNow.AddMinutes(10);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(2000);
                using var pollResp = await Http.PostAsJsonAsync(baseUrl + "/api/pc/pair/poll", new { code, secret }, Camel);
                var poll = await pollResp.Content.ReadFromJsonAsync<JsonElement>();
                if (!pollResp.IsSuccessStatusCode)
                {
                    return ErrorOf(poll, pollResp);
                }
                var status = poll.TryGetProperty("status", out var st) ? st.GetString() : null;
                if (status == "linked")
                {
                    SaveToken(poll.GetProperty("token").GetString(), includeHistory);
                    return null;
                }
                if (status == "expired")
                {
                    return L.T("La solicitud caducó. Inténtalo de nuevo.", "The link request expired. Try again.");
                }
            }
            return L.T("Tiempo agotado. Inténtalo de nuevo.", "Linking timed out. Try again.");
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Online auto link", ex);
            return ex.Message;
        }
    }

    /// <summary>
    /// Opens the website already signed in as this PC's profile. The server checks this PC's device token and returns a
    /// one-time address that works for two minutes; the device token itself is never put in the address.
    /// </summary>
    public static async Task<string?> OpenWebAsync(string path)
    {
        try
        {
            var s = Load();
            if (string.IsNullOrEmpty(s.Token))
            {
                return L.T("Este PC no está vinculado todavía.", "This PC is not linked yet.");
            }
            var baseUrl = s.ApiBase.TrimEnd('/');
            using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/api/pc/web-session")
            {
                Content = JsonContent.Create(new { next = path }, options: Camel),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
            using var resp = await Http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (!resp.IsSuccessStatusCode)
            {
                return resp.StatusCode == HttpStatusCode.Unauthorized
                    ? L.T("Este PC fue desvinculado en la web. Vincúlalo de nuevo.", "This PC was unlinked on the website. Link it again.")
                    : ErrorOf(json, resp);
            }
            OpenBrowser(json.GetProperty("url").GetString()!, baseUrl);
            return null;
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Online open web", ex);
            return ex.Message;
        }
    }

    private static void Loop()
    {
        var next = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            Thread.Sleep(2000);
            if (DateTime.UtcNow < next && !wake)
            {
                continue;
            }
            wake = false;
            try
            {
                var s = Load();
                next = DateTime.UtcNow.Add(s.Enabled && !string.IsNullOrEmpty(s.Token) ? SyncOnce(s) : TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Online sync", ex);
                next = DateTime.UtcNow.AddMinutes(5);
            }
        }
    }

    private static TimeSpan SyncOnce(OnlineSettings s)
    {
        var minute = TimeSpan.FromSeconds(60);
        var dataPath = Path.Combine(folder, "data.json");
        var configPath = Path.Combine(folder, "config.json");
        if (!File.Exists(dataPath))
        {
            return minute;
        }
        var data = JsonSerializer.Deserialize<TrackerData>(File.ReadAllText(dataPath));
        if (data == null)
        {
            return minute;
        }
        var config = (File.Exists(configPath) ? JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath)) : null) ?? new Config();
        var key = Profiles.Current(data, config)?.Key;
        var done = new HashSet<string>(s.Synced);
        var since = s.SinceUtc ?? DateTime.MinValue;

        var batch = data.Matches
            .Where(m => m.IsBattleRoyale && m.MatchStartUtc is { } t && t.ToUniversalTime() >= since
                && !string.IsNullOrEmpty(m.SessionId) && !done.Contains(m.SessionId)
                && (key == null || Profiles.Key(m) == key))
            .OrderBy(m => m.MatchStartUtc)
            .Take(50)
            .ToList();
        if (batch.Count == 0)
        {
            return minute;
        }

        var payload = batch.Select(m =>
        {
            var destroyed = m.Chests.Count(c => c.Destroyed);
            return new
            {
                sessionId = m.SessionId,
                startedAt = m.MatchStartUtc!.Value.ToUniversalTime().ToString("O"),
                mode = m.Playlist ?? "",
                opened = Math.Max(0, m.ChestsOpened - destroyed),
                destroyed,
                chestTypes = m.Chests.GroupBy(c => c.Kind).ToDictionary(g => g.Key, g => g.Count()),
                chestTimes = m.Chests.Select(c => (int)c.TimeSeconds).ToArray(),
            };
        }).ToList();

        using var req = new HttpRequestMessage(HttpMethod.Post, s.ApiBase.TrimEnd('/') + "/api/matches")
        {
            Content = JsonContent.Create(new { matches = payload }, options: Camel),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
        using var resp = Http.Send(req);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            Update(x => { x.Enabled = false; x.Token = null; x.LastError = "This PC was unlinked on the website."; });
            return TimeSpan.FromMinutes(5);
        }
        if (!resp.IsSuccessStatusCode)
        {
            Update(x => x.LastError = $"Server answered {(int)resp.StatusCode}");
            return TimeSpan.FromMinutes(5);
        }

        var ids = batch.Select(m => m.SessionId).ToList();
        Update(x =>
        {
            x.Synced.AddRange(ids);
            if (x.Synced.Count > 5000)
            {
                x.Synced.RemoveRange(0, x.Synced.Count - 5000);
            }
            x.LastSyncUtc = DateTime.UtcNow;
            x.LastError = null;
        });
        return batch.Count == 50 ? TimeSpan.FromSeconds(5) : minute;
    }
}
