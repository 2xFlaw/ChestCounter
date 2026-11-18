using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
namespace ChestCounter;

// Separate, explicitly enabled personal snapshots. They never enter the leaderboard tables.
public static class PersonalCloud
{
    public sealed class Options { public string Source { get; set; } = Guid.NewGuid().ToString("N"); public string? Consent { get; set; } }
    private static string folder="";
    private static Options options=new();
    private static readonly SemaphoreSlim Busy=new(1,1);
    private static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(30)};
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    private static System.Threading.Timer? timer;
    private static bool installed;
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static void Start(string path)
    {
        if(timer!=null)return;
        folder=path;
        try {options=System.Text.Json.JsonSerializer.Deserialize<Options>(File.ReadAllText(Path.Combine(folder,"2xflaw-personal-cloud.json")))??new();}catch{options=new();}
        timer=new System.Threading.Timer(async _=>{await Sync(false);},null,TimeSpan.FromSeconds(15),TimeSpan.FromMinutes(1));
        if(installed||!OperatingSystem.IsWindows())return;
        installed=true;
        Application.Idle+=(_,_)=>
        {
            foreach(var form in Application.OpenForms.OfType<OnlineForm>().ToArray())
            {
                var panel=form.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                if(panel==null||panel.Controls.OfType<Button>().Any(b=>Equals(b.Tag,"personal-cloud")))continue;
                panel.AutoScroll=true;
                var button=new Button{AutoSize=true,Tag="personal-cloud",Text="Sync full personal history (includes manual additions)"};
                var stop=new Button{AutoSize=true,Text="Stop personal-history sync"};
                var status=new Label{AutoSize=false,Width=440,Height=60,Text="Optional: manual entries and imported replays stay out of leaderboard rankings. Sync uses this PC as your personal-history source."};
                panel.Controls.Add(button);panel.Controls.Add(stop);panel.Controls.Add(status);
                panel.Controls.SetChildIndex(button,0);panel.Controls.SetChildIndex(stop,1);panel.Controls.SetChildIndex(status,2);
                button.Click+=async(_,_)=>
                {
                    var s=OnlineSync.Load();
                    if(string.IsNullOrEmpty(s.Token)||!s.Enabled){status.Text="Link this PC and enable sync first.";return;}
                    if(MessageBox.Show(form,"Upload all existing eligible replay history, imported replay statistics and manual chest amounts? Notes, names, Epic IDs and replay files are not uploaded. This replaces the website starting-total contribution, not adds to it. Future edits and deletions sync while enabled.","Sync personal history",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
                    options.Consent=Hash(s.Token);Save();button.Enabled=false;status.Text="Syncing history...";
                    var error=await Sync(true);
                    if(!form.IsDisposed){button.Enabled=true;status.Text=error??"History synced. Refresh My stats. Personal changes now sync every minute while app sync is enabled.";}
                };
                stop.Click+=(_,_)=>{options.Consent=null;Save();status.Text="Personal-history uploads stopped. The last website snapshot remains until you clear it there.";};
            }
        };
    }
    private static void Save(){Directory.CreateDirectory(folder);var p=Path.Combine(folder,"2xflaw-personal-cloud.json");File.WriteAllText(p+".tmp",JsonSerializer.Serialize(options));File.Move(p+".tmp",p,true);}
    private static T? Read<T>(string name)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(folder,name)));
    private static async Task Send(string route,object payload,string token,string api)
    {
        var current=OnlineSync.Load();
        if(!current.Enabled||current.Token!=token||options.Consent!=Hash(token))throw new InvalidOperationException("Personal sync was stopped or the linked profile changed.");
        using var req=new HttpRequestMessage(HttpMethod.Post,api.TrimEnd('/')+route){Content=JsonContent.Create(payload,options:Json)};
        req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        using var resp=await Http.SendAsync(req);
        if(!resp.IsSuccessStatusCode){var message=await resp.Content.ReadAsStringAsync();throw new InvalidOperationException("Personal sync: "+message);}
    }
    private static async Task<string?> Sync(bool history)
    {
        if(!await Busy.WaitAsync(0))return history?"A sync is already running. Try again shortly.":null;
        try
        {
            var s=OnlineSync.Load();
            if(!s.Enabled||string.IsNullOrEmpty(s.Token)||options.Consent!=Hash(s.Token))return history?"Enable sync and approve personal-history upload first.":null;
            var data=Read<TrackerData>("data.json")??throw new InvalidOperationException("No replay history available.");
            var config=File.Exists(Path.Combine(folder,"config.json"))?Read<Config>("config.json")??new():new Config();
            var ledger=File.Exists(Path.Combine(folder,"2xflaw-ledger.json"))?Read<PersonalLedger>("2xflaw-ledger.json")??throw new InvalidOperationException("Cannot read manual ledger."):new PersonalLedger();
            var key=Profiles.Current(data,config)?.Key??throw new InvalidOperationException("Select a profile in the app first.");
            var now=DateTime.UtcNow;
            bool Eligible(MatchRecord m)=>m.IsBattleRoyale&&Profiles.Key(m)==key&&(m.MatchStartUtc==null||m.MatchStartUtc.Value.ToUniversalTime()<=now)&&(config.CountFrom is not {} from||m.MatchStartUtc is {} at&&at.ToLocalTime().Date>=from.Date);
            string Id(MatchRecord m)=>!string.IsNullOrWhiteSpace(m.SessionId)?"session:"+m.SessionId:$"fallback:{key}|{m.MatchStartUtc:O}|{Path.GetFileName(m.ReplayFile)}";
            var all=data.Matches.Where(Eligible).Select(m=>(match:m,origin:"replay")).Concat(ledger.Imports.Select(i=>i.Match).Where(Eligible).Select(m=>(match:m,origin:"imported"))).GroupBy(x=>Id(x.match)).Select(g=>g.First()).ToList();
            var matches=all.Select(x=>new {id=Hash(Id(x.match)),startedAt=x.match.MatchStartUtc?.ToUniversalTime().ToString("O"),mode=x.match.Playlist??"",origin=x.origin,opened=Math.Max(0,x.match.ChestsOpened-x.match.Chests.Count(c=>c.Destroyed)),destroyed=x.match.Chests.Count(c=>c.Destroyed),team=config.CountTeammateChests?Math.Max(0,x.match.Teammates?.Sum(t=>t.ChestsSeen)??0):0}).OrderBy(x=>x.id).ToArray();
            var entries=ledger.Entries.Where(e=>e.Profile==key&&e.AtUtc.ToUniversalTime()<=now&&(config.CountFrom is not {} from||e.AtUtc.ToLocalTime().Date>=from.Date)).Select(e=>new{id=e.Id.ToString(),at=e.AtUtc.ToUniversalTime().ToString("O"),amount=Math.Max(0,e.Mine)+(config.CountTeammateChests?Math.Max(0,e.Squad):0)}).OrderBy(e=>e.id).ToArray();
            if(matches.Length>10000||entries.Length>10000)throw new InvalidOperationException("Personal history exceeds the 10,000-record safety limit; nothing was truncated or replaced.");
            if(history)
            {
                var recorded=data.Matches.Where(Eligible).Where(m=>m.MatchStartUtc!=null&&!string.IsNullOrEmpty(m.SessionId)&&m.SessionId.Length<=80).GroupBy(m=>m.SessionId).Select(g=>g.First()).ToList();
                foreach(var batch in recorded.Chunk(50))
                {
                    var payload=batch.Select(m=>new{sessionId=m.SessionId,startedAt=m.MatchStartUtc!.Value.ToUniversalTime().ToString("O"),mode=m.Playlist??"",opened=Math.Max(0,m.ChestsOpened-m.Chests.Count(c=>c.Destroyed)),destroyed=m.Chests.Count(c=>c.Destroyed),chestTypes=m.Chests.GroupBy(c=>c.Kind).ToDictionary(g=>g.Key,g=>g.Count()),chestTimes=m.Chests.Select(c=>(int)c.TimeSeconds).ToArray()}).ToArray();
                    await Send("/api/matches",new{matches=payload},s.Token,s.ApiBase);
                    await Task.Delay(250);
                }
            }
            await Send("/api/personal/snapshot",new{source=options.Source,scope=Hash(key),matches,entries},s.Token,s.ApiBase);
            return null;
        }
        catch(Exception ex){ErrorLog.Write("Personal cloud sync",ex);return ex.Message;}
        finally{Busy.Release();}
    }
}
