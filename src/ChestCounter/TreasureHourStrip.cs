using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows.Forms;

namespace ChestCounter;

[SupportedOSPlatform("windows")]
internal sealed class TreasureHourStrip : UserControl
{
    public sealed class Preferences
    {
        public bool Enabled { get; set; }
        public HashSet<string> Seen { get; set; } = new();
    }
    private static readonly Dictionary<string,Preferences> Saved=new(StringComparer.OrdinalIgnoreCase);
    private readonly Store store;
    private readonly Func<bool> animations;
    private readonly string settingsPath;
    private readonly Preferences preferences;
    private readonly Label dot=new(),status=new();
    private readonly CheckBox alerts=new();
    private readonly System.Windows.Forms.Timer clock=new(){Interval=1000};
    private readonly Font textFont=Theme.Text(9f,FontStyle.Bold),smallFont=Theme.Text(8f);
    private PersonalOdds? odds;
    private DateTime readAt=DateTime.MinValue,hideAt=DateTime.MaxValue;
    private NotifyIcon? notification;
    private bool syncing,pulse;
    private readonly HashSet<string> attempted=new();

    public TreasureHourStrip(Store store,Func<bool> animations)
    {
        this.store=store;this.animations=animations;
        settingsPath=Path.Combine(store.Folder,"2xflaw-treasure-alerts.json");
        if(!Saved.TryGetValue(settingsPath,out var saved))
        {
            try{saved=File.Exists(settingsPath)?JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath)):null;}
            catch(Exception ex){ErrorLog.Write("Treasure Hour alert preferences",ex);}
            saved??=new Preferences();saved.Seen??=new HashSet<string>();Saved[settingsPath]=saved;
        }
        preferences=saved;
        Height=66;Margin=Padding.Empty;Padding=new Padding(12,6,12,6);BackColor=Theme.Card;
        dot.Dock=DockStyle.Left;dot.Width=22;dot.Font=textFont;dot.TextAlign=ContentAlignment.MiddleCenter;dot.Text="●";
        status.Dock=DockStyle.Fill;status.Font=textFont;status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;status.UseMnemonic=false;
        alerts.Dock=DockStyle.Right;alerts.Width=144;alerts.Font=smallFont;alerts.ForeColor=Theme.Ink;alerts.Text="Notify at start";alerts.Checked=preferences.Enabled;
        Controls.Add(status);Controls.Add(dot);Controls.Add(alerts);
        alerts.CheckedChanged+=(_,_)=>{if(syncing)return;bool old=preferences.Enabled;preferences.Enabled=alerts.Checked;if(!Save())preferences.Enabled=old;UpdateStatus();};
        clock.Tick+=(_,_)=>UpdateStatus();clock.Start();UpdateStatus();
    }
    private bool Save()
    {
        try{string temp=settingsPath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(preferences));File.Move(temp,settingsPath,true);return true;}
        catch(Exception ex){ErrorLog.Write("Save Treasure Hour alerts",ex);return false;}
    }
    private static string BoostText(TreasureWindow window)
    {
        var items=new List<string>();
        foreach(var prize in new[]{("gold-outfit","Gold outfit"),("pirate-shop-access","Pirate shop"),("vehicle-decal","Vehicle & decal")})
        {double boost=TreasureHours.Boost(window,prize.Item1);if(boost>1)items.Add($"{prize.Item2} {boost:0.#}x");}
        return string.Join(" | ",items);
    }
    private void UpdateStatus()
    {
        if(IsDisposed)return;
        var now=DateTime.UtcNow;
        if((now-readAt).TotalSeconds>=30||now<readAt)
        {
            readAt=now;
            try{string path=Path.Combine(store.Folder,"2xflaw-odds.json");odds=File.Exists(path)?JsonSerializer.Deserialize<PersonalOdds>(File.ReadAllText(path)):PersonalOdds.Seed();}
            catch(Exception ex){odds=null;ErrorLog.Write("Treasure Hour schedule",ex);}
        }
        var live=odds?.Windows.Where(w=>w.EndUtc>w.StartUtc).OrderBy(w=>w.StartUtc).FirstOrDefault(w=>now>=w.StartUtc&&now<w.EndUtc);
        var next=odds?.Windows.Where(w=>w.StartUtc>now&&w.EndUtc>w.StartUtc).OrderBy(w=>w.StartUtc).FirstOrDefault();
        if(notification!=null&&now>=hideAt){notification.Visible=false;hideAt=DateTime.MaxValue;}
        if(live!=null&&!TreasureHours.IsUnconfirmed(live)&&preferences.Enabled)
        {
            string id=live.StartUtc.ToUniversalTime().ToString("O");
            if(!preferences.Seen.Contains(id)&&attempted.Add(id))
            {
                preferences.Seen.Add(id);
                if(Save())
                {
                    try{notification??=new NotifyIcon{Icon=SystemIcons.Information,Text="Chest Counter"};notification.Visible=true;notification.ShowBalloonTip(8000,"Treasure Hour is live",$"{BoostText(live)}. Ends {live.EndUtc.ToLocalTime():HH:mm} local.",ToolTipIcon.Info);hideAt=now.AddSeconds(12);}
                    catch(Exception ex){ErrorLog.Write("Treasure Hour notification",ex);}
                }
                else preferences.Seen.Remove(id);
            }
        }
        var form=FindForm();if(form!=null&&(!form.Visible||form.WindowState==FormWindowState.Minimized))return;
        syncing=true;alerts.Checked=preferences.Enabled;syncing=false;
        string text;bool confirmed=live!=null&&!TreasureHours.IsUnconfirmed(live);
        if(live!=null)
        {
            var left=live.EndUtc-now;
            text=$"TREASURE HOUR LIVE - ends in {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}\n"+(confirmed?BoostText(live):"Boosts unconfirmed - normal odds apply");
        }
        else if(next!=null)text=$"Next Treasure Hour: {next.StartUtc.ToLocalTime():ddd dd MMM HH:mm} local\nNotifications are optional. Uses your local odds schedule.";
        else text=odds==null?"Treasure Hour schedule unavailable\nCheck your local 2xflaw-odds.json file.":"No upcoming Treasure Hours\nNotifications are optional. Uses your local odds schedule.";
        if(status.Text!=text)status.Text=text;
        pulse=!pulse;dot.ForeColor=confirmed?(animations()&&pulse?Color.FromArgb(170,119,55):Theme.Amber):Theme.Muted;
        status.ForeColor=confirmed?Theme.Amber:Theme.Muted;
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing){clock.Stop();clock.Dispose();notification?.Dispose();}
        base.Dispose(disposing);
        if(disposing){textFont.Dispose();smallFont.Dispose();}
    }
}
