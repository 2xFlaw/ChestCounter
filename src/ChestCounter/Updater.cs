namespace ChestCounter;
public sealed record UpdateInfo(Version Version, string Tag, string AssetUrl, long Size, string? Sha256, string PageUrl);

/// <summary>
/// Updates from the latest GitHub release of 2xFlaw/ChestCounter (asset "ChestCounter.exe").
/// The download is checked against the release's size and SHA-256 before it replaces the running exe:
/// ChestCounter.exe is renamed to ChestCounter.old.exe (Windows allows renaming a running exe),
/// the new file takes its name, and the old one is deleted on the next start.
/// </summary>
public static class Updater
{
    private const string AssetName = "ChestCounter.exe";
    private static readonly string LatestUrl = $"https://api.github.com/repos/{AppInfo.Repo}/releases/latest";

    private static HttpClient Client(TimeSpan timeout)
    {
        var http = new HttpClient { Timeout = timeout };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ChestCounter/" + AppInfo.VersionText);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return http;
    }

    /// <summary>The latest release when it is newer than this build, otherwise null. Never throws.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var http = Client(TimeSpan.FromSeconds(20));
            using var doc = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync(LatestUrl));
            var root = doc.RootElement;
            if (root.TryGetProperty("draft", out var draft) && draft.ValueKind == System.Text.Json.JsonValueKind.True) return null;
            if (root.TryGetProperty("prerelease", out var pre) && pre.ValueKind == System.Text.Json.JsonValueKind.True) return null;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!TryParse(tag, out var version) || version <= Current()) return null;

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (!string.Equals(asset.GetProperty("name").GetString(), AssetName, StringComparison.OrdinalIgnoreCase)) continue;
                var url = asset.GetProperty("browser_download_url").GetString();
                if (string.IsNullOrEmpty(url) || !url.StartsWith("https://github.com/" + AppInfo.Repo + "/", StringComparison.OrdinalIgnoreCase)) return null;
                var size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var n) ? n : 0;
                string? sha = null;
                if (asset.TryGetProperty("digest", out var d) && d.GetString() is { } digest && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    sha = digest[7..].Trim().ToLowerInvariant();
                }
                var page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? AppInfo.RepoUrl + "/releases" : AppInfo.RepoUrl + "/releases";
                return new UpdateInfo(version, tag, url, size, sha, page);
            }
        }
        catch (Exception ex)
        {
            // Offline, rate-limited or an unexpected answer: try again later.
            ErrorLog.Write("Update check", ex.GetType().Name + ": " + ex.Message + (ex.InnerException != null ? " / " + ex.GetBaseException().Message : ""));
        }
        return null;
    }

    /// <summary>Downloads and swaps in the new exe. Returns null on success or a short error message.</summary>
    public static string? Install(UpdateInfo update)
    {
        var exe = AppInfo.ExePath;
        if (exe == null)
        {
            return L.T("renombra el programa a ChestCounter.exe para poder actualizarlo", "rename the program to ChestCounter.exe so it can update itself");
        }
        var dir = Path.GetDirectoryName(exe)!;
        var fresh = Path.Combine(dir, "ChestCounter.new.exe");
        var old = Path.Combine(dir, "ChestCounter.old.exe");
        try
        {
            TryDelete(fresh);
            using (var http = Client(TimeSpan.FromMinutes(10)))
            using (var response = http.GetAsync(update.AssetUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                using var input = response.Content.ReadAsStream();
                using var output = new FileStream(fresh, FileMode.Create, FileAccess.Write, FileShare.None);
                input.CopyTo(output);
            }

            var length = new FileInfo(fresh).Length;
            if (length < 1_000_000 || (update.Size > 0 && length != update.Size))
            {
                TryDelete(fresh);
                return L.T("la descarga está incompleta", "the download is incomplete");
            }
            if (update.Sha256 != null)
            {
                using var stream = File.OpenRead(fresh);
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
                if (hash != update.Sha256)
                {
                    stream.Dispose();
                    TryDelete(fresh);
                    return L.T("la descarga no coincide con la publicada", "the download does not match the release");
                }
            }

            TryDelete(old);
            if (File.Exists(old))
            {
                // Still in use by a previous run: use another name, it is cleaned up later.
                old = Path.Combine(dir, $"ChestCounter.old-{DateTime.UtcNow:yyyyMMddHHmmss}.exe");
            }
            File.Move(exe, old);
            try
            {
                File.Move(fresh, exe);
            }
            catch
            {
                File.Move(old, exe); // put the running version back
                throw;
            }
            return null;
        }
        catch (Exception ex)
        {
            TryDelete(fresh);
            ErrorLog.Write("Update install", ex);
            return ex.Message;
        }
    }

    /// <summary>Deletes the previous exe left by an update (it may still be closing, so it retries for a while).</summary>
    public static void CleanupOld()
    {
        var exe = AppInfo.ExePath;
        if (exe == null) return;
        var dir = Path.GetDirectoryName(exe)!;
        new Thread(() =>
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var left = 0;
                try
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "ChestCounter.old*.exe").Append(Path.Combine(dir, "ChestCounter.new.exe")))
                    {
                        if (!File.Exists(f)) continue;
                        if (!TryDelete(f)) left++;
                    }
                }
                catch
                {
                    return;
                }
                if (left == 0) return;
                Thread.Sleep(1000);
            }
        }) { IsBackground = true, Name = "Limpieza de actualización" }.Start();
    }

    private static Version Current() => new(AppInfo.Version.Major, AppInfo.Version.Minor, Math.Max(0, AppInfo.Version.Build));

    private static bool TryParse(string tag, out Version version)
    {
        version = new Version(0, 0, 0);
        var t = tag.Trim().TrimStart('v', 'V');
        var cut = t.IndexOfAny(new[] { '-', '+', ' ' });
        if (cut >= 0) t = t[..cut];
        if (!Version.TryParse(t, out var v)) return false;
        version = new Version(v.Major, v.Minor, Math.Max(0, v.Build));
        return true;
    }

    private static bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
