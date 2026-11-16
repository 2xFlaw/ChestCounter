using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// The weekly report: a single self-contained web page (banner, avatar, prize pictures, fonts and data are inside the file)
/// with your goal, the odds table, the Treasure Hour planner and schedule, this week's chests and your matches.
/// It is created by the app (Weekly tools > Weekly report) and opened in your browser; countdowns, the Treasure Hour
/// state and the planner keep working live there.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WeeklyReport
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void Open(Store store, Monitor monitor, IWin32Window? owner = null)
    {
        try
        {
            var path = Write(store, monitor);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex)
        {
            ErrorLog.Write("Weekly report", ex);
            MessageBox.Show(owner, ex.Message, "Report error");
        }
    }

    private static T? Read<T>(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;
        }
        catch
        {
            return default;
        }
    }

    private static DateTime AsUtc(DateTime d) =>
        d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static string Iso(DateTime d) => AsUtc(d).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static long Epoch(DateTime d) => new DateTimeOffset(AsUtc(d)).ToUnixTimeMilliseconds();

    internal static string DataUri(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
    }

    /// <summary>Your banner, the same picture the app shows on the Dashboard (falls back to the embedded file).</summary>
    private static string Banner()
    {
        try
        {
            Image? raw = null;
            try
            {
                // Not disposed on purpose: the app may share this picture with the Dashboard.
                raw = PersonalBrand.Banner();
            }
            catch
            {
                // use the embedded file below
            }
            if (raw == null)
            {
                using var stream = typeof(WeeklyReport).Assembly.GetManifestResourceStream("ChestCounter.Assets.2xflaw-banner.jpg");
                if (stream == null)
                {
                    return "";
                }
                var copy = new MemoryStream();
                stream.CopyTo(copy);
                raw = new Bitmap(copy);
            }
            var width = Math.Min(1600, raw.Width);
            var height = (int)Math.Round(raw.Height * (width / (double)raw.Width));
            using var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(raw, 0, 0, width, height);
            }
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 82L);
            using var output = new MemoryStream();
            bitmap.Save(output, codec, parameters);
            return "<img class=\"banner\" alt=\"\" src=\"data:image/jpeg;base64," + Convert.ToBase64String(output.ToArray()) + "\">";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Your round avatar, shown next to your name in the header.</summary>
    private static string Avatar()
    {
        try
        {
            var raw = PersonalBrand.Avatar();
            using var bitmap = new Bitmap(96, 96);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(raw, 0, 0, 96, 96);
            }
            return "<img class=\"avatar\" alt=\"\" src=\"" + DataUri(bitmap) + "\">";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Anton and Inter (the fonts embedded in the exe) as web fonts, so the page looks like the app.</summary>
    internal static string FontFaces()
    {
        var assembly = typeof(WeeklyReport).Assembly;
        var css = new StringBuilder();
        foreach (var name in assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("ChestCounter.Assets.fonts.", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
        {
            string family;
            int weight;
            if (name.Contains("Anton", StringComparison.OrdinalIgnoreCase))
            {
                family = "CC Display";
                weight = 400;
            }
            else if (name.Contains("Inter", StringComparison.OrdinalIgnoreCase))
            {
                family = "CC Inter";
                weight = name.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? 700 : 400;
            }
            else
            {
                continue;
            }
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                continue;
            }
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            css.Append("@font-face{font-family:\"").Append(family).Append("\";font-weight:").Append(weight)
                .Append(";src:url(data:font/ttf;base64,").Append(Convert.ToBase64String(buffer.ToArray()))
                .Append(") format(\"truetype\");font-display:swap}\n");
        }
        return css.ToString();
    }

    public static string Write(Store store, Monitor monitor)
    {
        var now = DateTime.UtcNow;
        var snap = WeeklyChanceModel.Compute(store, monitor, now);
        var cache = PersonalReadCache.Load(store);
        var profile = Profiles.Current(cache, monitor.Config);
        var key = profile?.Key ?? "";
        var ledger = Read<PersonalLedger>(Path.Combine(store.Folder, "2xflaw-ledger.json")) ?? new PersonalLedger();
        var odds = Read<PersonalOdds>(Path.Combine(store.Folder, "2xflaw-odds.json")) ?? PersonalOdds.Seed();
        var start = snap.WeekStart;
        var end = start.AddDays(7);

        var all = cache.Matches.Concat(ledger.Imports.Select(x => x.Match))
            .Where(m => Profiles.Key(m) == key && m.IsBattleRoyale)
            .GroupBy(m => string.IsNullOrEmpty(m.SessionId) ? $"{m.ReplayFile}|{m.MatchStartUtc:O}" : m.SessionId)
            .Select(g => g.First()).ToList();
        var inWeek = all.Where(m => m.MatchStartUtc is { } t && t < end && t.AddSeconds(m.DurationSeconds ?? 0) >= start).ToList();
        var entriesWeek = ledger.Entries.Where(e => e.Profile == key && e.AtUtc >= start && e.AtUtc <= now).ToList();

        // Chests per day since the reset.
        var daily = new int[7];
        void AddAt(DateTime? when, int count)
        {
            if (when is not { } time || time < start || time >= end)
            {
                return;
            }
            var index = Math.Clamp((int)Math.Floor((time - start).TotalDays), 0, 6);
            daily[index] += count;
        }
        foreach (var m in inWeek)
        {
            if (m.Chests.Count > 0)
            {
                foreach (var c in m.Chests)
                {
                    AddAt(m.MatchStartUtc!.Value.AddSeconds(c.TimeSeconds), 1);
                }
            }
            else
            {
                AddAt(m.MatchStartUtc, PersonalWeek.PersonalCount(m, start, end));
            }
        }
        foreach (var e in entriesWeek)
        {
            AddAt(e.AtUtc, e.Mine);
        }

        // Treasure Hours come only from the odds file. The last two have no announced boosts yet (a grey question mark).
        var windows = odds.Windows
            .Where(w => w.EndUtc >= start)
            .OrderBy(w => w.StartUtc)
            .Take(10)
            .Select(w => new
            {
                start = Iso(w.StartUtc),
                end = Iso(w.EndUtc),
                gold = w.Gold,
                shop = w.Shop,
                vehicle = w.Vehicle,
                unknown = TreasureHours.IsUnconfirmed(w),
            }).ToList();

        // Prize pictures are embedded, so the page works anywhere.
        var folder = Path.Combine(store.Folder, "rewards");
        var prizes = new List<object>();
        foreach (var p in snap.Prizes)
        {
            using var picture = RewardPictures.Load(folder, p.Key, 150, 14);
            prizes.Add(new { key = p.Key, name = p.Name, den = p.Den, chance = p.Chance, owned = p.Owned, img = DataUri(picture) });
        }

        string? pick = null;
        try
        {
            var goalPath = Path.Combine(store.Folder, "2xflaw-goal.txt");
            var text = File.Exists(goalPath) ? File.ReadAllText(goalPath).Trim() : "";
            pick = text.Length == 0 ? null : text;
        }
        catch
        {
            // no remembered goal
        }

        var matches = all.OrderByDescending(m => m.MatchStartUtc).Take(100).Select(m => new
        {
            start = m.MatchStartUtc is { } started ? Iso(started) : null,
            mode = Pretty.Playlist(m.Playlist),
            place = Pretty.Placement(m),
            win = m.Placement == 1,
            chests = m.ChestsOpened,
            matesTotal = m.Teammates?.Sum(tm => tm.ChestsSeen) ?? 0,
            mates = (m.Teammates ?? new()).Select(tm => new { name = tm.Name, chests = tm.ChestsSeen }).ToList(),
            treasures = m.TreasureFinds.Select(f => f.DisplayName).ToList(),
        }).ToList();

        var manual = ledger.Entries.Where(e => e.Profile == key).OrderByDescending(e => e.AtUtc)
            .Select(e => new { at = Iso(e.AtUtc), mine = e.Mine, squad = e.Squad, note = e.Note }).ToList();

        var data = new
        {
            profile = profile?.Name ?? "",
            generatedUtc = Epoch(now),
            resetUtc = Epoch(WeeklyReset.Next(now)),
            chests = snap.Chests,
            pick,
            prizes,
            windows,
            daily = daily.Select((c, i) => new { start = Iso(start.AddDays(i)), chests = c }).ToList(),
            kpis = new
            {
                mine = snap.Chests,
                mates = inWeek.Sum(m => m.Teammates?.Sum(tm => tm.ChestsSeen) ?? 0) + entriesWeek.Sum(e => e.Squad),
                manual = entriesWeek.Sum(e => e.Mine),
            },
            matches,
            manual,
            odds = new { source = odds.Source, captured = Iso(odds.CapturedUtc), status = odds.Status },
        };

        var html = WeeklyReportTemplate.Html
            .Replace("/*FONTS*/", FontFaces())
            .Replace("@@BANNER@@", Banner())
            .Replace("@@AVATAR@@", Avatar())
            .Replace("@@DATA@@", JsonSerializer.Serialize(data, Json));

        var path = Path.Combine(store.Folder, "2xflaw-report.html");
        File.WriteAllText(path, html, new UTF8Encoding(false));
        return path;
    }
}
