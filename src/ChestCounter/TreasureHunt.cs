using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ChestCounter;

public sealed record HuntItem(string Slot, string Name, string Image);

public sealed record HuntWeek(int Number, DateTime StartUtc, IReadOnlyList<HuntItem> Items)
{
    public DateTime EndUtc => StartUtc.AddDays(7);
}

/// <summary>
/// The Treasure Hunt rewards, week by week. A built-in copy ships with the app; chestcounter.io/rewards/weeks.json
/// can add new weeks without an app update (cached in the data folder, pictures cached in rewards/hunt).
/// Found ticks use the same keys as Weekly chances: "slot@yyyyMMdd" for the week's start, so ticking this week's
/// items still hides them there. The Pirate Shop Token keeps its all-time key.
/// </summary>
public static partial class TreasureHunt
{
    public static readonly string[] Slots = { "gold-outfit", "vehicle-decal", "pickaxe-back-bling", "jam-track", "loading-screen" };
    public const string PirateImage = "pirate-shop-access.png";
    private const string CacheFile = "2xflaw-hunt.json";

    private static HuntWeek Week(int number, int day, params string[] names)
    {
        var start = new DateTime(2026, 11, day, 14, 0, 0, DateTimeKind.Utc);
        string[] files = { "outfit", "car", "pickaxe", "jam", "loading" };
        return new HuntWeek(number, start, Slots.Select((slot, i) => new HuntItem(slot, names[i], $"w{number}-{files[i]}.png")).ToArray());
    }

    public static readonly IReadOnlyList<HuntWeek> BuiltIn = new[]
    {
        Week(1, 1, "Solid Gold Rugahand", "Gold G-Wagon", "Solid Gold Gauntlets", "NO TRESPASSING", "Plunder in Paradise"),
        Week(2, 8, "Solid Gold Lil Uzi Vert", "Gold Cadillac CT5-V", "Diamond and Gold", "London", "So Kawaii"),
        Week(3, 15, "Solid Gold Miku", "Gold Corvette ZR1", "Miku's Takarabako", "PPPP", "May Fortune Find You"),
        Week(4, 22, "Solid Gold Kasane Teto", "Gold Lamborghini Urus SE", "Rogue Wave and Tidewalker", "Sea Shanty", "Oscar's Wraps"),
        // The final week (Golden Week). Pictures not released yet use a question-mark picture; weeks.json on the website
        // can switch them to the real ones later without an app update.
        new HuntWeek(5, new DateTime(2026, 11, 29, 14, 0, 0, DateTimeKind.Utc), new[]
        {
            new HuntItem("gold-outfit", "Solid Gold Captain X", "w5-outfit.png"),
            new HuntItem("vehicle-decal", "Gold G-Wagon v2", "soon-vehicle.png"),
            new HuntItem("pickaxe-back-bling", "Roasted Meat", "w5-pickaxe.png"),
            new HuntItem("jam-track", "I Follow Rivers", "soon-jam.png"),
            new HuntItem("loading-screen", "Jones and the Straw Hats", "soon-loading.png"),
        }),
    };

    public static string OwnedKey(HuntWeek week, string slot) => $"{slot}@{week.StartUtc:yyyyMMdd}";
    public static string PirateKey => WeeklyReset.OwnedKey(PirateShopChances.Key, DateTime.UtcNow);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,60}\\.(png|jpg)$")]
    private static partial Regex ImageName();

    [GeneratedRegex("^[a-z0-9-]{1,40}@(all-time|\\d{8})$")]
    private static partial Regex KeyPattern();

    public static bool IsSyncKey(string key) => KeyPattern().IsMatch(key);

    // ---- week list ---------------------------------------------------------------------------------------------

    private sealed class WireItem { public string? Slot { get; set; } public string? Name { get; set; } public string? Image { get; set; } }
    private sealed class WireWeek { public int Week { get; set; } public string? Start { get; set; } public List<WireItem>? Items { get; set; } }
    private sealed class WireFile { public int Version { get; set; } public List<WireWeek>? Weeks { get; set; } }

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Parses weeks.json; null if anything is off, so a bad file can never break the window.</summary>
    public static IReadOnlyList<HuntWeek>? Parse(string text)
    {
        try
        {
            var file = JsonSerializer.Deserialize<WireFile>(text, Json);
            if (file?.Weeks == null || file.Version != 1 || file.Weeks.Count is 0 or > 20)
            {
                return null;
            }
            var weeks = new List<HuntWeek>();
            foreach (var w in file.Weeks)
            {
                if (w.Week is < 1 or > 99 || w.Items == null || w.Items.Count != Slots.Length
                    || !DateTime.TryParse(w.Start, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var start))
                {
                    return null;
                }
                var items = new List<HuntItem>();
                for (var i = 0; i < Slots.Length; i++)
                {
                    var it = w.Items[i];
                    if (it.Slot != Slots[i] || string.IsNullOrWhiteSpace(it.Name) || it.Name.Length > 60 || it.Image == null || !ImageName().IsMatch(it.Image))
                    {
                        return null;
                    }
                    items.Add(new HuntItem(it.Slot, it.Name.Trim(), it.Image));
                }
                weeks.Add(new HuntWeek(w.Week, DateTime.SpecifyKind(start, DateTimeKind.Utc), items));
            }
            weeks.Sort((a, b) => a.Number.CompareTo(b.Number));
            return weeks.Select(x => x.Number).Distinct().Count() == weeks.Count ? weeks : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The cached list from the website when it is valid and at least as long as the built-in one.</summary>
    public static IReadOnlyList<HuntWeek> Load(Store store)
    {
        try
        {
            var path = Path.Combine(store.Folder, CacheFile);
            if (File.Exists(path) && Parse(File.ReadAllText(path)) is { } cached && cached.Count >= BuiltIn.Count)
            {
                return cached;
            }
        }
        catch
        {
            // fall back to the built-in list
        }
        return BuiltIn;
    }

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static string ApiBase()
    {
        var b = OnlineSync.Load().ApiBase;
        return Uri.TryCreate(b, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps ? b.TrimEnd('/') : "https://chestcounter.io";
    }

    /// <summary>
    /// Downloads weeks.json and any missing pictures. Returns the new list when it changed, otherwise null.
    /// Never throws. Pictures are only fetched from the website's /rewards/ folder, at most 2 MB each.
    /// </summary>
    public static async Task<IReadOnlyList<HuntWeek>?> Refresh(Store store, IReadOnlyList<HuntWeek> current)
    {
        try
        {
            var baseUrl = ApiBase();
            var text = await Http.GetStringAsync(baseUrl + "/rewards/weeks.json");
            var weeks = Parse(text);
            if (weeks == null || weeks.Count < BuiltIn.Count)
            {
                return null;
            }
            var folder = Path.Combine(store.Folder, "rewards", "hunt");
            Directory.CreateDirectory(folder);
            foreach (var item in weeks.SelectMany(w => w.Items))
            {
                if (HasEmbedded(item.Image) || File.Exists(Path.Combine(folder, item.Image)))
                {
                    continue;
                }
                try
                {
                    using var resp = await Http.GetAsync(baseUrl + "/rewards/" + item.Image, HttpCompletionOption.ResponseHeadersRead);
                    if (!resp.IsSuccessStatusCode || resp.Content.Headers.ContentLength > 2_000_000)
                    {
                        continue;
                    }
                    var bytes = await resp.Content.ReadAsByteArrayAsync();
                    if (bytes.Length > 2_000_000)
                    {
                        continue;
                    }
                    using (var check = Image.FromStream(new MemoryStream(bytes)))
                    {
                        // decodes, so it is a real picture
                    }
                    var tmp = Path.Combine(folder, item.Image + ".tmp");
                    await File.WriteAllBytesAsync(tmp, bytes);
                    File.Move(tmp, Path.Combine(folder, item.Image), overwrite: true);
                }
                catch
                {
                    // a missing picture shows a placeholder
                }
            }
            var path = Path.Combine(store.Folder, CacheFile);
            var old = File.Exists(path) ? File.ReadAllText(path) : null;
            if (old != text)
            {
                File.WriteAllText(path + ".tmp", text);
                File.Move(path + ".tmp", path, overwrite: true);
            }
            return Same(weeks, current) ? null : weeks;
        }
        catch
        {
            return null;
        }
    }

    private static bool Same(IReadOnlyList<HuntWeek> a, IReadOnlyList<HuntWeek> b) =>
        a.Count == b.Count && a.Zip(b).All(p => p.First.Number == p.Second.Number && p.First.StartUtc == p.Second.StartUtc
            && p.First.Items.SequenceEqual(p.Second.Items));

    // ---- pictures ----------------------------------------------------------------------------------------------

    private static bool HasEmbedded(string image) =>
        typeof(TreasureHunt).Assembly.GetManifestResourceNames().Contains("ChestCounter.Assets.rewards." + image, StringComparer.OrdinalIgnoreCase);

    private static byte[]? Bytes(Store store, string image)
    {
        try
        {
            var cached = Path.Combine(store.Folder, "rewards", "hunt", image);
            if (File.Exists(cached))
            {
                return File.ReadAllBytes(cached);
            }
            var assembly = typeof(TreasureHunt).Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.Equals("ChestCounter.Assets.rewards." + image, StringComparison.OrdinalIgnoreCase));
            if (name == null)
            {
                return null;
            }
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                return null;
            }
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The picture, square, at <paramref name="px"/> pixels: photos fill the square, cut-outs keep a margin. Null if missing.</summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap? Picture(Store store, string image, int px)
    {
        var data = Bytes(store, image);
        if (data == null)
        {
            return null;
        }
        try
        {
            using var raw = Image.FromStream(new MemoryStream(data));
            using var source = new Bitmap(raw);
            // Photos and covers (opaque a little way in from each corner, so rounded corners still count) fill the tile.
            int ix = Math.Max(1, source.Width / 16), iy = Math.Max(1, source.Height / 16);
            var opaque = source.GetPixel(ix, iy).A >= 250 && source.GetPixel(source.Width - 1 - ix, iy).A >= 250
                && source.GetPixel(ix, source.Height - 1 - iy).A >= 250 && source.GetPixel(source.Width - 1 - ix, source.Height - 1 - iy).A >= 250;
            var result = new Bitmap(px, px, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(result);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (opaque)
            {
                var side = Math.Min(source.Width, source.Height);
                var src = new RectangleF((source.Width - side) / 2f, (source.Height - side) / 2f, side, side);
                g.DrawImage(source, new RectangleF(0, 0, px, px), src, GraphicsUnit.Pixel);
            }
            else
            {
                var box = px * 0.86f;
                var scale = Math.Min(box / source.Width, box / source.Height);
                var w = source.Width * scale;
                var h = source.Height * scale;
                g.DrawImage(source, new RectangleF((px - w) / 2, (px - h) / 2, w, h));
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>A dimmed black-and-white copy for items not found yet.</summary>
    [SupportedOSPlatform("windows")]
    public static Bitmap Locked(Bitmap colour)
    {
        var result = new Bitmap(colour.Width, colour.Height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(result);
        const float k = 0.62f;
        var matrix = new ColorMatrix(new[]
        {
            new[] { 0.30f * k, 0.30f * k, 0.30f * k, 0, 0 },
            new[] { 0.59f * k, 0.59f * k, 0.59f * k, 0, 0 },
            new[] { 0.11f * k, 0.11f * k, 0.11f * k, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new[] { 0.02f, 0.02f, 0.025f, 0, 1 },
        });
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix);
        g.DrawImage(colour, new Rectangle(0, 0, colour.Width, colour.Height), 0, 0, colour.Width, colour.Height, GraphicsUnit.Pixel, attributes);
        return result;
    }

    // ---- sync with the website ---------------------------------------------------------------------------------

    /// <summary>Changes not yet saved on the website (key -> value and time), kept in 2xflaw-hunt-pending.json.</summary>
    public sealed class Pending
    {
        public Dictionary<string, PendingChange> Changes { get; set; } = new();
    }

    public sealed class PendingChange
    {
        [JsonPropertyName("v")] public bool V { get; set; }
        [JsonPropertyName("t")] public long T { get; set; }
    }

    private sealed class OwnedReply { public Dictionary<string, bool>? Owned { get; set; } }

    private static readonly object PendingGate = new();
    private static string PendingPath(Store store) => Path.Combine(store.Folder, "2xflaw-hunt-pending.json");

    private static Pending ReadPending(Store store)
    {
        try
        {
            var p = PendingPath(store);
            return File.Exists(p) ? JsonSerializer.Deserialize<Pending>(File.ReadAllText(p)) ?? new() : new();
        }
        catch
        {
            return new();
        }
    }

    private static void WritePending(Store store, Pending pending)
    {
        try
        {
            var p = PendingPath(store);
            File.WriteAllText(p + ".tmp", JsonSerializer.Serialize(pending));
            File.Move(p + ".tmp", p, overwrite: true);
        }
        catch
        {
            // retried next time
        }
    }

    /// <summary>Remembers a tick so it reaches the website even if this PC is offline right now.</summary>
    public static void Remember(Store store, string key, bool value)
    {
        if (!IsSyncKey(key))
        {
            return;
        }
        lock (PendingGate)
        {
            var pending = ReadPending(store);
            pending.Changes[key] = new PendingChange { V = value, T = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            WritePending(store, pending);
        }
    }

    public static bool CanSync
    {
        get
        {
            var s = OnlineSync.Load();
            return s.Enabled && !string.IsNullOrEmpty(s.Token);
        }
    }

    /// <summary>
    /// Sends waiting ticks and returns the website's full list (newest change wins on the server), or null when
    /// sync is off or the website cannot be reached. Never throws. Only "found" ticks are sent.
    /// </summary>
    public static async Task<Dictionary<string, bool>?> Sync(Store store, IReadOnlyDictionary<string, bool> local)
    {
        try
        {
            var s = OnlineSync.Load();
            if (!s.Enabled || string.IsNullOrEmpty(s.Token))
            {
                return null;
            }
            var baseUrl = Uri.TryCreate(s.ApiBase, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps ? s.ApiBase.TrimEnd('/') : "https://chestcounter.io";
            Pending pending;
            lock (PendingGate)
            {
                pending = ReadPending(store);
            }
            using (var get = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/api/owned"))
            {
                get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
                using var resp = await Http.SendAsync(get);
                if (!resp.IsSuccessStatusCode)
                {
                    return null;
                }
                var server = (await resp.Content.ReadFromJsonAsync<OwnedReply>(Json))?.Owned ?? new();
                // Ticks made in the app before sync existed: send them once, dated at the epoch so any website change wins.
                foreach (var (key, value) in local)
                {
                    if (IsSyncKey(key) && !server.ContainsKey(key) && !pending.Changes.ContainsKey(key))
                    {
                        pending.Changes[key] = new PendingChange { V = value, T = 0 };
                    }
                }
                if (pending.Changes.Count == 0)
                {
                    return server;
                }
            }
            var sent = pending.Changes.Take(200).ToDictionary(p => p.Key, p => p.Value);
            using var post = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/api/owned") { Content = JsonContent.Create(new { set = sent }) };
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
            using var reply = await Http.SendAsync(post);
            if (!reply.IsSuccessStatusCode)
            {
                return null;
            }
            var merged = (await reply.Content.ReadFromJsonAsync<OwnedReply>(Json))?.Owned;
            lock (PendingGate)
            {
                var now = ReadPending(store);
                foreach (var (key, change) in sent)
                {
                    if (now.Changes.TryGetValue(key, out var c) && c.T == change.T && c.V == change.V)
                    {
                        now.Changes.Remove(key);
                    }
                }
                WritePending(store, now);
            }
            return merged;
        }
        catch
        {
            return null;
        }
    }
}
