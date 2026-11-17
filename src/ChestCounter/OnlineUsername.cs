using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChestCounter;

/// <summary>Sets the public username on the website from the app. Only used when the player presses Save.</summary>
internal static class OnlineUsername
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Returns an error message, or null on success.</summary>
    public static async Task<string?> SetAsync(string username)
    {
        try
        {
            var s = OnlineSync.Load();
            if (string.IsNullOrEmpty(s.Token))
            {
                return L.T("Vincula este PC primero.", "Link this PC first.");
            }
            using var req = new HttpRequestMessage(HttpMethod.Put, s.ApiBase.TrimEnd('/') + "/api/pc/username")
            {
                Content = JsonContent.Create(new { username = username.Trim() }),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
            using var resp = await Http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("error", out var e) ? e.GetString() : $"HTTP {(int)resp.StatusCode}";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
