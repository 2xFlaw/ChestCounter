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
        alerts.Dock=DockStyle.Right;alerts.Width=144;alerts.Font=smallFont;alerts.ForeColor=Theme.Ink;alerts.Text=ToolText.T("Notify at start");alerts.Checked=preferences.Enabled;
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
        {double boost=TreasureHours.Boost(window,prize.Item1);if(boost>1)items.Add($"{ToolText.T(prize.Item2)} {boost.ToString("0.#",L.Culture)}x");}
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
                    try{notification??=new NotifyIcon{Icon=SystemIcons.Information,Text="Chest Counter"};notification.Visible=true;notification.ShowBalloonTip(8000,ToolText.T("Treasure Hour is live"),ToolText.F("{0}. Ends {1:HH:mm} local.",BoostText(live),live.EndUtc.ToLocalTime()),ToolTipIcon.Info);hideAt=now.AddSeconds(12);}
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
            text=ToolText.F("TREASURE HOUR LIVE - ends in {0:00}:{1:00}:{2:00}",(int)left.TotalHours,left.Minutes,left.Seconds)+"\n"+(confirmed?BoostText(live):ToolText.T("Boosts unconfirmed - normal odds apply"));
        }
        else if(next!=null)text=ToolText.F("Next Treasure Hour: {0:ddd dd MMM HH:mm} local",next.StartUtc.ToLocalTime())+"\n"+ToolText.T("Notifications are optional. Uses your local odds schedule.");
        else text=odds==null?ToolText.T("Treasure Hour schedule unavailable")+"\n"+ToolText.T("Check your local 2xflaw-odds.json file."):ToolText.T("No upcoming Treasure Hours")+"\n"+ToolText.T("Notifications are optional. Uses your local odds schedule.");
        // Golden Week (announced): replaces the second line while no Treasure Hour is live.
        if(live==null)
        {
            var first=text.Split('\n')[0];
            if(TreasureHours.IsGoldenWeek(now))text=first+"\n"+ToolText.T("GOLDEN WEEK - every treasure reward is back \u00b7 Gold Outfit 2x \u00b7 Gold Vehicle 5x \u00b7 2x Sprite Dust & XP");
            else if(now<TreasureHours.GoldenWeekStartUtc&&TreasureHours.GoldenWeekStartUtc-now<TimeSpan.FromDays(14))text=first+"\n"+ToolText.F("Golden Week starts {0:ddd dd MMM HH:mm} local: every treasure reward returns, Gold Outfit 2x, Gold Vehicle 5x",TreasureHours.GoldenWeekStartUtc.ToLocalTime());
        }
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
