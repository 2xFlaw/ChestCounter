using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace ChestCounter;

public sealed class PersonalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public string Profile { get; set; } = "";
    public int Mine { get; set; }
    public int Squad { get; set; }
    public string Note { get; set; } = "";
}
public sealed class PersonalImport
{
    public string Hash { get; set; } = "";
    public MatchRecord Match { get; set; } = new();
}
public sealed class PersonalLedger
{
    public List<PersonalEntry> Entries { get; set; } = new();
    public List<PersonalImport> Imports { get; set; } = new();
    public Dictionary<string, List<string>> Claimed { get; set; } = new();
}
public sealed class TreasureWindow
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    // Announced boosts (TreasureHours.AnnouncedBoosts) win over the saved numbers.
    private double _gold = 7.5, _shop = 1.5, _vehicle = 10;
    public double Gold { get => TreasureHours.AnnouncedBoosts(StartUtc)?.Gold ?? _gold; set => _gold = value; }
    public double Shop { get => TreasureHours.AnnouncedBoosts(StartUtc)?.Shop ?? _shop; set => _shop = value; }
    public double Vehicle { get => TreasureHours.AnnouncedBoosts(StartUtc)?.Vehicle ?? _vehicle; set => _vehicle = value; }
}
public sealed class PersonalOdds
{
    public string Source { get; set; } = "https://fortnite.bot/treasure";
    public DateTime CapturedUtc { get; set; } = new(2026,11,14,1,34,0,DateTimeKind.Utc);
    public string Status { get; set; } = "Dated snapshot. Event times are entered by you and the rates are checked against the source. Nothing is fetched automatically.";
    public List<TreasureWindow> Windows { get; set; } = new();
    public static PersonalOdds Seed()
    {
        var x = new PersonalOdds();
        foreach (int day in new[] {14,21,28})
        {
            var start = new DateTime(2026,11,day,19,0,0,DateTimeKind.Utc);
            x.Windows.Add(new() { StartUtc=start, EndUtc=start.AddHours(2) });
            start = new DateTime(2026,11,day+1,2,0,0,DateTimeKind.Utc);
            x.Windows.Add(new() { StartUtc=start, EndUtc=start.AddHours(2) });
        }
        return x;
    }
}
public sealed class PersonalTools : Form
{
    private readonly Store store;
    private readonly Monitor monitor;
    private readonly PersonalLedger ledger;
    private readonly PersonalOdds odds;
    private readonly DataGridView grid = new() { Dock=DockStyle.Fill, AutoGenerateColumns=true, AllowUserToAddRows=false, ReadOnly=true, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label status = new() { AutoSize=true, ForeColor=Color.White, Padding=new Padding(8) };
    private readonly ComboBox profiles = new() { Width=220, DropDownStyle=ComboBoxStyle.DropDownList };
    private readonly DateTimePicker when = new() { Width=185, Format=DateTimePickerFormat.Custom, CustomFormat="yyyy-MM-dd HH:mm" };
    private readonly NumericUpDown mine = new() { Maximum=1000000, Width=85 };
    private readonly NumericUpDown squad = new() { Maximum=1000000, Width=85 };
    private readonly TextBox note = new() { Width=200, PlaceholderText=ToolText.T("Note / source") };
    private readonly CheckedListBox claimed = new() { Width=230, Height=115, CheckOnClick=true };
    // Saved ticks use the English key; only the displayed text is translated.
    private sealed record ClaimItem(string Key) { public override string ToString() => WeeklyChanceText.T(Key); }
    private static readonly string[] Names = {"Weekly Gold Outfit","Pirate Shop Access","Weekly Vehicle & Decal","Weekly Pickaxe or Back Bling","Weekly Jam Track","Weekly Loading Screen","Any gizmo"};
    private static readonly double[] Den = {1562500,137500,7813,568,391,272,127};
    private string LedgerPath => Path.Combine(store.Folder,"2xflaw-ledger.json");
    private string OddsPath => Path.Combine(store.Folder,"2xflaw-odds.json");
    private string Key => (profiles.SelectedItem as Profile)?.Key ?? "";
    private string ClaimKey => Key+"|"+Week(DateTime.UtcNow).ToString("O");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented=true };
    public PersonalTools(Store store, Monitor monitor)
    {
        this.store=store; this.monitor=monitor;
        ledger=Read<PersonalLedger>(LedgerPath) ?? new();
        odds=Read<PersonalOdds>(OddsPath) ?? PersonalOdds.Seed();
        if (!File.Exists(OddsPath)) Save(OddsPath,odds);
        Text=ToolText.T("2xFlaw's Chest Counter - Weekly tools"); Size=new(1120,720); BackColor=Theme.Bg; ForeColor=Theme.Ink; Font=Theme.Text(10);
        var top=new FlowLayoutPanel { Dock=DockStyle.Top, Height=150, AutoScroll=true, Padding=new Padding(8) };
        void Caption(string text) => top.Controls.Add(new Label { Text=ToolText.T(text), AutoSize=true, Padding=new Padding(4,8,0,0) });
        void Button(string text, Action action) { var b=new Button { Text=ToolText.T(text), AutoSize=true }; Theme.Style(b); b.Click+=(_,_)=>action(); top.Controls.Add(b); }
        profiles.DisplayMember="Name"; profiles.DataSource=Profiles.List(PersonalReadCache.Load(store),monitor.Config);
        top.Controls.Add(profiles); Caption("Local date/time");top.Controls.Add(when);Caption("Your chests");top.Controls.Add(mine);Caption("Teammates only");top.Controls.Add(squad);top.Controls.Add(note);
        Button("Add dated entry",()=> { ledger.Entries.Add(new() { Profile=Key, AtUtc=when.Value.ToUniversalTime(), Mine=(int)mine.Value, Squad=(int)squad.Value, Note=note.Text }); Persist(); });
        Button("Update selected entry",()=> { if(grid.CurrentRow?.DataBoundItem is PersonalEntry e) { e.AtUtc=when.Value.ToUniversalTime(); e.Mine=(int)mine.Value; e.Squad=(int)squad.Value; e.Note=note.Text; Persist(); } });
        Button("Use current week",()=>when.Value=Week(DateTime.UtcNow).AddMinutes(1).ToLocalTime());
        Button("Delete selected entry",()=> { if(grid.CurrentRow?.DataBoundItem is PersonalEntry e && MessageBox.Show(this,ToolText.T("Delete selected manual entry?"),ToolText.T("Confirm"),MessageBoxButtons.YesNo)==DialogResult.Yes) {ledger.Entries.Remove(e);Persist();} });
        Button("Import replay(s)",()=> {using var f=new OpenFileDialog {Filter="Fortnite replay|*.replay",Multiselect=true};if(f.ShowDialog()==DialogResult.OK) _=Import(f.FileNames);});
        Button("Weekly report / chances",()=>WeeklyReport.Open(store,monitor,this));
        Button("Transparent overlay",()=>new PersonalOverlay(Snapshot).Show(this));
        Button("Open odds JSON",()=>Process.Start(new ProcessStartInfo(OddsPath){UseShellExecute=true}));
        Button("Save claimed prizes",()=> {ledger.Claimed[ClaimKey]=claimed.CheckedItems.Cast<ClaimItem>().Select(i=>i.Key).ToList();Persist();});
        foreach(var n in Names.Take(6)) claimed.Items.Add(new ClaimItem(n));
        top.Controls.Add(claimed);
        Controls.Add(grid);Controls.Add(top);Controls.Add(new Panel { Dock=DockStyle.Bottom, Height=42, Controls={status} });
        profiles.SelectedIndexChanged+=(_,_)=>RefreshEntries();
        AllowDrop=true; DragEnter+=(_,e)=> {if(e.Data?.GetDataPresent(DataFormats.FileDrop)==true)e.Effect=DragDropEffects.Copy;};
        DragDrop+=(_,e)=> {if(e.Data?.GetData(DataFormats.FileDrop) is string[] f) _=Import(f);};
        // Readable column headings instead of the property names (AtUtc, Mine…); the internal id is not shown.
        grid.DataBindingComplete+=(_,_)=> { foreach(DataGridViewColumn c in grid.Columns) { switch(c.DataPropertyName) { case "Id": c.Visible=false; break; case "AtUtc": c.HeaderText=ToolText.T("Date (UTC)"); break; case "Profile": c.HeaderText=ToolText.T("Profile"); break; case "Mine": c.HeaderText=ToolText.T("Your chests"); break; case "Squad": c.HeaderText=ToolText.T("Teammates only"); break; case "Note": c.HeaderText=ToolText.T("Note"); break; } } };
        grid.SelectionChanged+=(_,_)=> { if(grid.CurrentRow?.DataBoundItem is PersonalEntry e) { when.Value=e.AtUtc.ToLocalTime();mine.Value=Math.Clamp(e.Mine,0,1000000);squad.Value=Math.Clamp(e.Squad,0,1000000);note.Text=e.Note; } };
        RefreshEntries();
    }
    private static T? Read<T>(string path)
    {
        if(!File.Exists(path))return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
    }
    private static void Save<T>(string path,T value)
    {
        if(File.Exists(path)) File.Copy(path,path+".bak",true);
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(value,Json)); File.Move(path+".tmp",path,true);
    }
    private void Persist() { Save(LedgerPath,ledger); monitor.RefreshPersonalStatistics(); RefreshEntries(); }
    private void RefreshEntries()
    {
        grid.DataSource=ledger.Entries.Where(e=>e.Profile==Key).OrderByDescending(e=>e.AtUtc).ToList();
        var c=ledger.Claimed.GetValueOrDefault(ClaimKey) ?? new();
        for(int i=0;i<claimed.Items.Count;i++)claimed.SetItemChecked(i,c.Contains(((ClaimItem)claimed.Items[i]).Key));
        status.Text=ToolText.T("Manual additions feed dashboard totals and daily charts. Dates below are UTC. Drop .replay files here.");
    }
        public static DateTime Week(DateTime utc) => WeeklyReset.Start(utc);
    private List<MatchRecord> Matches()
    {
        return PersonalReadCache.Load(store).Matches.Concat(ledger.Imports.Select(x=>x.Match))
            .Where(m=>Profiles.Key(m)==Key && m.IsBattleRoyale)
            .GroupBy(m=>string.IsNullOrEmpty(m.SessionId)?$"{m.ReplayFile}|{m.MatchStartUtc:O}":m.SessionId)
            .Select(g=>g.First()).ToList();
    }
    private async Task Import(string[] paths)
    {
        foreach(var path in paths.Where(p=>Path.GetExtension(p).Equals(".replay",StringComparison.OrdinalIgnoreCase)))
        {
            status.Text=ToolText.F("Parsing {0} - please wait",Path.GetFileName(path));
            try
            {
                var result=await Task.Run(()=> {
                    using var f=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
                    var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(f));f.Position=0;
                    var match=new ReplayAnalyzer().Analyze(f,path,null);return new PersonalImport {Hash=hash,Match=match};
                });
                if(ledger.Imports.Any(x=>x.Hash==result.Hash) || (!string.IsNullOrEmpty(result.Match.SessionId) && PersonalReadCache.Load(store).Matches.Any(m=>m.SessionId==result.Match.SessionId)))
                    status.Text=ToolText.F("Duplicate skipped: {0}",Path.GetFileName(path));
                else {ledger.Imports.Add(result);Save(LedgerPath,ledger); monitor.RefreshPersonalStatistics();status.Text=ToolText.F("Imported {0}: {1} personal chests. Stored in ledger and projected into dashboard totals.",result.Match.PlayerName,result.Match.ChestsOpened);}
                var k=Key;profiles.DataSource=Profiles.List(new TrackerData {Matches=PersonalReadCache.Load(store).Matches.Concat(ledger.Imports.Select(x=>x.Match)).ToList()});
                foreach(var p in profiles.Items.Cast<Profile>())if(p.Key==k){profiles.SelectedItem=p;break;}
            }
            catch(Exception ex){status.Text=ToolText.F("Import failed: {0}",ex.Message);ErrorLog.Write("Personal replay import",ex);}
        }
    }
    private (int total,int last,int week,int squad) Snapshot()
    {
        var m=Matches();var start=Week(DateTime.UtcNow);var e=ledger.Entries.Where(e=>e.Profile==Key).ToList();
        return (m.Sum(x=>x.ChestsOpened)+e.Sum(x=>x.Mine),m.OrderByDescending(x=>x.MatchStartUtc).FirstOrDefault()?.ChestsOpened??0,
            m.Sum(x=>PersonalWeek.PersonalCount(x,start,DateTime.UtcNow))+e.Where(x=>x.AtUtc>=start&&x.AtUtc<DateTime.UtcNow).Sum(x=>x.Mine),
            m.Where(x=>x.MatchStartUtc>=start).Sum(x=>x.Teammates?.Sum(t=>t.ChestsSeen)??0)+e.Where(x=>x.AtUtc>=start&&x.AtUtc<DateTime.UtcNow).Sum(x=>x.Squad));
    }
    private void Report()
    {
        try
        {
            var o=Read<PersonalOdds>(OddsPath)??odds;var now=DateTime.UtcNow;var start=Week(now);var end=start.AddDays(7);var all=Matches();
            var matches=all.Where(m=>m.MatchStartUtc is {} t && t<end && t.AddSeconds(m.DurationSeconds??0)>=start).ToList();
            var events=new List<(DateTime? time,int count)>();
            foreach(var m in matches)
            {
                if(m.Chests.Count>0)events.AddRange(m.Chests.Select(c=>((DateTime?)m.MatchStartUtc!.Value.AddSeconds(c.TimeSeconds),1)).Where(e=>e.Item1>=start&&e.Item1<end));
                else events.Add((null,PersonalWeek.PersonalCount(m,start,end)));
            }
            var entries=ledger.Entries.Where(e=>e.Profile==Key&&e.AtUtc>=start&&e.AtUtc<=now).ToList();
            events.AddRange(entries.Select(e=>((DateTime?)e.AtUtc,e.Mine)));
            int personal=events.Sum(x=>x.count), mates=matches.Sum(m=>m.Teammates?.Sum(t=>t.ChestsSeen)??0)+entries.Sum(e=>e.Squad);
            static string H(object? x)=>System.Net.WebUtility.HtmlEncode(x?.ToString()??"");
            var b=new StringBuilder("<!doctype html><meta charset='utf-8'><meta name='viewport' content='width=device-width'><title>2xFlaw's Chest Counter</title><style>body{background:#0b0e14;color:#e6eaf2;font:16px Segoe UI,sans-serif;max-width:1150px;margin:30px auto;padding:15px}h1,h2,a{color:#ff66b2}table{width:100%;border-collapse:collapse;background:#151a24}td,th{padding:12px;text-align:left;border-bottom:1px solid #343d50}.card{background:#151a24;border-radius:14px;padding:20px;margin:16px 0}.muted{color:#b0bacb}.banner{width:100%;border-radius:16px}strong{color:#ff80b8}</style>");
            b.Append("<img class='banner' src='2xflaw-banner.jpg' alt='2xFlaw'><h1>2xFlaw's Chest Counter</h1>");
            b.Append($"<p>Profile: {H((profiles.SelectedItem as Profile)?.Name)} | Generated {H(now.ToLocalTime())}. Reopen the report to refresh the statistics.</p>");
            b.Append($"<div class='card'><strong>{personal:N0}</strong> of your chests this week | <strong>≥ {mates:N0}</strong> teammate chests (seen or added by hand) | Team ≥ {personal+mates:N0}<p>Added by hand: {entries.Sum(e=>e.Mine):N0}. Teammates are only partly visible in replays, so their counts are a minimum.</p><p>Weekly reset: <span data-date='{end:O}'></span> — <span data-count='{end:O}'></span></p></div>");
            b.Append("<h2>Your chances</h2><p>The estimated chance of at least one drop, treating each chest as an independent try. It isn't a guarantee or the odds for your next chest. Claimed prizes are ticked by hand per profile and week, so check which rewards are on offer each week. Squad eligibility and exact teammate opening times are unknown.</p><table><tr><th>Reward</th><th>Next chest now</th><th>Personal this week</th><th>Squad illustration</th><th>Status</th></tr>");
            var live=o.Windows.FirstOrDefault(w=>now>=w.StartUtc&&now<w.EndUtc);
            var claims=ledger.Claimed.GetValueOrDefault(ClaimKey)??new();
            for(int i=0;i<Names.Length;i++)
            {
                double Boost(TreasureWindow? w)=> w==null?1:i==0?w.Gold:i==1?w.Shop:i==2?w.Vehicle:1;
                double noDrop=1;
                foreach(var e in events){var w=e.time is {} t?o.Windows.FirstOrDefault(w=>t>=w.StartUtc&&t<w.EndUtc):null;noDrop*=Math.Pow(1-Math.Clamp(Boost(w)/Den[i],0,1),e.count);}
                var own=(1-noDrop)*100;var team=(1-noDrop*Math.Pow(1-1/Den[i],mates))*100;
                bool earned=claims.Contains(Names[i]);
                b.Append($"<tr><td>{H(Names[i])}</td><td data-den='{Den[i]}' data-index='{i}' data-claimed='{earned.ToString().ToLowerInvariant()}'>{(earned?"Claimed":"1 in "+(Den[i]/Boost(live)).ToString("N1"))}</td><td>{(earned?"Claimed":own.ToString("0.####")+"%")}</td><td>{team:0.####}%*</td><td>{(earned?"User-confirmed claimed":"Not confirmed claimed")}</td></tr>");
            }
            b.Append("</table><p class='muted'>*Squad illustration combines your timestamp-adjusted probability with teammates at BASE rates, assuming all remain eligible. It is not a verified squad reward probability. Missing personal chest timestamps use base odds. Manual entries use their entered time. Only confirmed normal-match eligible chests should be included.</p>");
            b.Append("<h2>Treasure Hours</h2><div id='event'></div><table><tr><th>Starts (your time)</th><th>Ends</th><th>Gold / Shop / Vehicle</th></tr>");
            foreach(var w in o.Windows.OrderBy(w=>w.StartUtc))b.Append($"<tr><td data-date='{w.StartUtc:O}'></td><td data-date='{w.EndUtc:O}'></td><td>{w.Gold}× / {w.Shop}× / {w.Vehicle}×</td></tr>");
            b.Append($"</table><p>Source: <a href='{H(o.Source)}'>Fortnite.bot</a> | Captured: <span data-date='{o.CapturedUtc:O}'></span><br>{H(o.Status)}<br>Cache age: {(now-o.CapturedUtc).TotalHours:0.0} hours. Treat as stale after 24 hours; countdowns use cached confirmed times only.</p>");
            b.Append("<h2>Personal treasures and team evidence</h2><p>The current replay model stores personal treasure finds and teammate chest counts, not reliably attributed teammate treasure finds. Team treasure ownership is unavailable, not zero.</p><table><tr><th>Match</th><th>Personal chests</th><th>Teammates observed</th><th>Personal treasures</th></tr>");
            foreach(var m in all.OrderByDescending(m=>m.MatchStartUtc).Take(100))b.Append($"<tr><td>{H(m.MatchStartUtc?.ToLocalTime())}</td><td>{m.ChestsOpened}</td><td>≥ {m.Teammates?.Sum(t=>t.ChestsSeen)??0}<br>{H(string.Join(", ",m.Teammates?.Select(t=>$"{t.Name}: {t.ChestsSeen}")??Enumerable.Empty<string>()))}</td><td>{H(string.Join(", ",m.TreasureFinds.Select(t=>t.DisplayName)))}</td></tr>");
            b.Append("</table><h2>Manual history</h2><table><tr><th>Local time</th><th>Personal</th><th>Teammates only</th><th>Note</th></tr>");
            foreach(var e in ledger.Entries.Where(e=>e.Profile==Key).OrderByDescending(e=>e.AtUtc))b.Append($"<tr><td>{H(e.AtUtc.ToLocalTime())}</td><td>{e.Mine}</td><td>{e.Squad}</td><td>{H(e.Note)}</td></tr>");
            b.Append("</table><script>const windows="+JsonSerializer.Serialize(o.Windows)+";function duration(ms){let s=Math.max(0,Math.floor(ms/1000));return Math.floor(s/86400)+'d '+Math.floor(s%86400/3600)+'h '+Math.floor(s%3600/60)+'m '+s%60+'s'}function tick(){const n=Date.now();document.querySelectorAll('[data-date]').forEach(e=>e.textContent=new Date(e.dataset.date).toLocaleString());document.querySelectorAll('[data-count]').forEach(e=>e.textContent=duration(new Date(e.dataset.count)-n));const a=windows.find(w=>n>=Date.parse(w.StartUtc)&&n<Date.parse(w.EndUtc)),next=windows.find(w=>Date.parse(w.StartUtc)>n);document.querySelectorAll('[data-den]').forEach(e=>{let i=+e.dataset.index,m=!a?1:i===0?a.Gold:i===1?a.Shop:i===2?a.Vehicle:1;e.textContent=e.dataset.claimed==='true'?'Claimed':'1 in '+(+e.dataset.den/m).toLocaleString(undefined,{maximumFractionDigits:1});});document.getElementById('event').textContent=a?'ACTIVE — ends in '+duration(Date.parse(a.EndUtc)-n):next?'Next session starts in '+duration(Date.parse(next.StartUtc)-n):'No confirmed upcoming event in cached schedule';}tick();setInterval(tick,1000);</script>");
            using(var image=typeof(PersonalTools).Assembly.GetManifestResourceStream("ChestCounter.Assets.2xflaw-banner.jpg"))
            { if(image!=null) { using var target=File.Create(Path.Combine(store.Folder,"2xflaw-banner.jpg"));image.CopyTo(target); } }
            var path=Path.Combine(store.Folder,"2xflaw-report.html");File.WriteAllText(path,b.ToString(),Encoding.UTF8);Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
        }catch(Exception ex){MessageBox.Show(this,ex.Message,ToolText.T("Report error"));}
    }
}
public sealed class PersonalOverlay : Form
{
    private readonly System.Windows.Forms.Timer timer=new() {Interval=3000};
    public PersonalOverlay(Func<(int total,int last,int week,int squad)> snapshot)
    {
        FormBorderStyle=FormBorderStyle.None; TopMost=true; ShowInTaskbar=false;BackColor=Color.Fuchsia;TransparencyKey=Color.Fuchsia;Size=new(390,130);StartPosition=FormStartPosition.Manual;Location=new(30,100);
        var label=new Label {Dock=DockStyle.Fill,ForeColor=Color.FromArgb(255,102,178),BackColor=Color.Transparent,Font=new Font("Segoe UI",15,FontStyle.Bold)};Controls.Add(label);
        var menu=new ContextMenuStrip();menu.Items.Add(ToolText.T("Close"),null,(_,_)=>Close());ContextMenuStrip=menu;label.ContextMenuStrip=menu;
        Point origin=default;bool drag=false;label.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left){origin=e.Location;drag=true;}};label.MouseMove+=(_,e)=>{if(drag)Location=new Point(Left+e.X-origin.X,Top+e.Y-origin.Y);};label.MouseUp+=(_,_)=>drag=false;
        void Update(){try{var x=snapshot();label.Text=ToolText.F("2xFlaw · CHESTS {0:N0}\nPrevious {1}  · Week {2:N0}\nSquad week ≥ {3:N0}",x.total,x.last,x.week,x.week+x.squad);}catch{label.Text=ToolText.T("Waiting for saved replay data…");}}
        timer.Tick+=(_,_)=>Update();FormClosed+=(_,_)=>timer.Dispose();Update();timer.Start();
    }
}
