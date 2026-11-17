namespace ChestCounter;
public sealed record UpdateInfo(Version Version, string Tag, string AssetUrl, long Size, string? Sha256, string PageUrl);
public static class Updater
{
    public static Task<UpdateInfo?> CheckAsync() => Task.FromResult<UpdateInfo?>(null);
    public static string? Install(UpdateInfo update) => "Upstream updates disabled in this personal build.";
    public static void CleanupOld() { }
}
