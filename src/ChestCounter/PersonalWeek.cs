namespace ChestCounter;
public static class PersonalWeek
{
    public static int PersonalCount(MatchRecord match,DateTime startUtc,DateTime endUtc)
    {
        if(match.MatchStartUtc is not {} time)return 0;
        if(match.Chests.Count>0)return match.Chests.Count(c=>time.AddSeconds(c.TimeSeconds)>=startUtc&&time.AddSeconds(c.TimeSeconds)<endUtc);
        // Without chest timestamps, a match crossing the reset cannot be split reliably.
        var end=time.AddSeconds(match.DurationSeconds??0);
        return time>=startUtc&&time<endUtc&&end<endUtc?match.ChestsOpened:0;
    }
}
public static class PersonalReadCache
{
    private static readonly object Gate=new();
    private static string? path;
    private static DateTime stamp;
    private static long length=-1;
    private static TrackerData? cached;
    public static TrackerData Load(Store store)
    {
        lock(Gate)
        {
            var info=new FileInfo(store.DataPath);
            var now=info.Exists?info.LastWriteTimeUtc:DateTime.MinValue;
            var size=info.Exists?info.Length:0;
            if(cached!=null&&path==store.DataPath&&stamp==now&&length==size)return cached;
            var next=store.LoadData();cached=next;path=store.DataPath;stamp=now;length=size;return next;
        }
    }
}
