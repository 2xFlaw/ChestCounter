using System.Text.Json;
namespace ChestCounter;

// Read-only projection: original history and personal ledger are never merged on disk.
public static class PersonalIntegration
{
    private static readonly object Gate=new();
    private static string folder=Store.DefaultFolder();
    private static PersonalLedger cached=new();
    private static DateTime stamp=DateTime.MinValue;
    private static long bytes=-1;
    public static void Bind(string path) {lock(Gate){if(folder!=path){folder=path;stamp=DateTime.MinValue;bytes=-1;cached=new();}}}
    private static PersonalLedger Ledger()
    {
        lock(Gate)
        {
            try
            {
                var info=new FileInfo(Path.Combine(folder,"2xflaw-ledger.json"));
                if(!info.Exists){cached=new();stamp=DateTime.MinValue;bytes=-1;return cached;}
                if(info.LastWriteTimeUtc==stamp&&info.Length==bytes)return cached;
                using var f=File.Open(info.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                var next=JsonSerializer.Deserialize<PersonalLedger>(f);
                if(next!=null){cached=next;stamp=info.LastWriteTimeUtc;bytes=info.Length;}
            }
            catch(Exception ex){ErrorLog.Write("Personal ledger projection; retaining last valid snapshot",ex);}
            return cached;
        }
    }
    public static TrackerData Recorded(TrackerData original)
    {
        var imports=Ledger().Imports;
        if(imports.Count==0)return original;
        // Original history wins when a session has subsequently been scanned normally.
        var matches=original.Matches.Concat(imports.Select(x=>x.Match))
            .GroupBy(m=>!string.IsNullOrWhiteSpace(m.SessionId)?"session:"+m.SessionId:
                $"fallback:{Profiles.Key(m)}|{m.MatchStartUtc:O}|{Path.GetFileName(m.ReplayFile)}")
            .Select(g=>g.First()).ToList();
        return new TrackerData {Version=original.Version,AnalyzerVersion=original.AnalyzerVersion,
            CreatedUtc=original.CreatedUtc,Matches=matches,ProcessedFiles=original.ProcessedFiles};
    }
    public static List<PersonalEntry> Manual(string? profile,Config config)
        =>Ledger().Entries.Where(e=>e.Profile==(profile??"")&&e.AtUtc<=DateTime.UtcNow
          &&(config.CountFrom is not {} from ||e.AtUtc.ToLocalTime().Date>=from.Date)).ToList();
    public static int ManualTotal(string? profile,Config config)
        =>Manual(profile,config).Sum(e=>Math.Max(0,e.Mine)+(config.CountTeammateChests?Math.Max(0,e.Squad):0));
    public static void AddManualDays(Dictionary<DateTime,int> days,TrackerData data,Config config)
    {
        var key=Profiles.Current(data,config)?.Key;
        foreach(var e in Manual(key,config))
        {
            var day=e.AtUtc.ToLocalTime().Date;
            days[day]=days.GetValueOrDefault(day)+Math.Max(0,e.Mine)+(config.CountTeammateChests?Math.Max(0,e.Squad):0);
        }
    }
}
