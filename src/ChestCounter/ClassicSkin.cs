using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;

namespace ChestCounter;

/// <summary>
/// Brings the classic report (report.html) in line with the Weekly report: the same pink 2xFlaw colours (always dark),
/// Anton and Inter fonts, glass cards, pill tabs, a sparkling banner, and a Treasure Hour strip with the weekly reset
/// countdown. It only post-processes the finished page, so the report's own code stays untouched.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClassicSkin
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static string? _fonts;
    private static Dictionary<string, string>? _images;

    public static string Apply(string html, Store store)
    {
        try
        {
            var odds = Read<PersonalOdds>(Path.Combine(store.Folder, "2xflaw-odds.json")) ?? PersonalOdds.Seed();
            var windows = odds.Windows.OrderBy(w => w.StartUtc).Select(w => new
            {
                start = Iso(w.StartUtc),
                end = Iso(w.EndUtc),
                gold = w.Gold,
                shop = w.Shop,
                vehicle = w.Vehicle,
                unknown = TreasureHours.IsUnconfirmed(w),
            }).ToList();
            var json = JsonSerializer.Serialize(new { windows, images = Images(store) }, Json);

            _fonts ??= WeeklyReport.FontFaces();

            // 1) style: appended after the report's own style
            var style = html.IndexOf("</style>", StringComparison.Ordinal);
            if (style >= 0)
            {
                html = html.Insert(style, _fonts + SkinCss);
            }

            // 2) banner: wrapped so sparkles can twinkle over it
            var alt = html.IndexOf("alt=\"2xFlaw banner\"", StringComparison.Ordinal);
            if (alt >= 0)
            {
                var open = html.LastIndexOf("<img", alt, StringComparison.Ordinal);
                var close = html.IndexOf('>', alt) + 1;
                if (open >= 0 && close > open)
                {
                    html = html[..open] + "<div class=\"bannerwrap\">" + html[open..close] + "<div class=\"sparkles\" id=\"ccSp\"></div></div>" + html[close..];
                }
            }

            // 3) Treasure Hour strip under the header
            var header = html.IndexOf("</header>", StringComparison.Ordinal);
            if (header >= 0)
            {
                header += "</header>".Length;
                html = html.Insert(header, "<div class=\"cc-hour\" id=\"ccHour\"></div>");
            }

            // 4) its script
            var body = html.LastIndexOf("</body>", StringComparison.Ordinal);
            if (body >= 0)
            {
                html = html.Insert(body, "<script>" + SkinScript.Replace("@@DATA@@", json) + "</script>");
            }
            return html;
        }
        catch
        {
            return html;
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

    private static string Iso(DateTime d)
    {
        var utc = d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : DateTime.SpecifyKind(d, DateTimeKind.Utc);
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The three Treasure Hour prize pictures, made once per run.</summary>
    private static Dictionary<string, string> Images(Store store)
    {
        if (_images != null)
        {
            return _images;
        }
        var folder = Path.Combine(store.Folder, "rewards");
        var result = new Dictionary<string, string>();
        foreach (var key in new[] { "gold-outfit", "pirate-shop-access", "vehicle-decal" })
        {
            using var picture = RewardPictures.Load(folder, key, 40, 8);
            result[key] = WeeklyReport.DataUri(picture);
        }
        _images = result;
        return result;
    }

    private const string SkinCss = """"

:root,:root:not([data-theme="dark"]),:root[data-theme="light"]{color-scheme:dark;--bg:#0e0912;--bg2:#140b19;--card:#190f1e;--card2:#211428;--ink:#ece6f2;--muted:#8f8499;--faint:#5b4d68;--line:#2b1b35;--line2:#3f2a52;--cyan:#ff66b2;--cyan-soft:rgba(255,102,178,.14);--blue:#d00080;--amber:#ffb84d;--amber-soft:rgba(255,184,77,.14);--rose:#ff80aa;--rose-soft:rgba(255,128,170,.13);--shadow:0 1px 2px rgba(0,0,0,.4),0 14px 40px rgba(0,0,0,.32);--ccd:"CC Display","Anton","Impact","Arial Narrow",sans-serif;--ccb:"CC Inter","Inter","Segoe UI Variable Text","Segoe UI",system-ui,-apple-system,sans-serif}
html{background:var(--bg)}
body{background:radial-gradient(1200px 560px at 12% -12%,rgba(208,0,128,.22),transparent 60%),radial-gradient(900px 480px at 100% 0,rgba(255,184,77,.08),transparent 55%),var(--bg);font-family:var(--ccb)}
h1,h2{font-family:var(--ccd);font-weight:400;text-transform:uppercase;letter-spacing:.01em}
.kpi .v,.mini-kpi b{font-family:var(--ccd);font-weight:400;letter-spacing:.01em}
.kpi.hero .v{text-shadow:0 0 30px rgba(255,102,178,.4)}
.kpi .l,.eyebrow,th,.board h3{letter-spacing:.14em}
.card,.kpi,.mate,.board,.loading{background:linear-gradient(180deg,rgba(255,255,255,.035),transparent 55%),var(--card);border-radius:20px}
.kpi.hero{background:linear-gradient(135deg,rgba(255,102,178,.16),transparent 62%),var(--card)}
.logo{background:linear-gradient(135deg,var(--blue),var(--cyan));color:#fff;box-shadow:0 0 22px rgba(255,102,178,.45)}
.upd{background:var(--card2)}
.tab{border-radius:12px;text-transform:uppercase;letter-spacing:.1em;font-size:12px;font-weight:800}
.tab[aria-selected="true"]{background:linear-gradient(180deg,var(--cyan),var(--blue));color:#fff;box-shadow:0 0 0 1px rgba(255,102,178,.5),0 6px 22px rgba(208,0,128,.35)}
.tab[aria-selected="true"] .c{background:rgba(255,255,255,.22);color:#fff}
th{background:var(--card2)}
tbody tr:hover td{background:rgba(255,255,255,.03)}
.track i,.teambar .tb i,.board .bar i{background:linear-gradient(90deg,var(--blue),var(--cyan))}
.board li.first .bar i{background:linear-gradient(90deg,#ff8a3d,var(--amber))}
.bannerwrap{position:relative;margin-bottom:18px}
.bannerwrap>img{display:block;width:100%!important;height:auto;max-height:340px;object-fit:cover;border-radius:24px!important;box-shadow:var(--shadow);margin:0!important}
.bannerwrap .sparkles{position:absolute;inset:0;pointer-events:none;overflow:hidden;border-radius:24px}
.sparkles i{position:absolute;color:#fff;font-style:normal;opacity:0;animation:cctw 3.4s ease-in-out infinite}
@keyframes cctw{0%,100%{opacity:0;transform:scale(.4) rotate(0)}45%,55%{opacity:.95;transform:scale(1) rotate(35deg)}}
footer{border-top-color:var(--line)}
.cc-hour{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:10px 18px;margin:0 0 22px;padding:14px 18px;border-radius:18px;border:1px solid var(--line);background:linear-gradient(180deg,rgba(255,255,255,.035),transparent 55%),var(--card);box-shadow:var(--shadow)}
.cc-hour.live{border-color:rgba(255,184,77,.6);background:linear-gradient(90deg,rgba(255,184,77,.16),var(--card));box-shadow:0 0 0 1px rgba(255,184,77,.15),0 0 38px rgba(255,184,77,.12)}
.cc-chip{display:inline-flex;align-items:center;gap:7px;padding:5px 12px;border-radius:99px;font-size:10.5px;font-weight:800;letter-spacing:.14em;text-transform:uppercase;color:var(--c,var(--cyan));background:color-mix(in srgb,var(--c,var(--cyan)) 14%,transparent);border:1px solid color-mix(in srgb,var(--c,var(--cyan)) 45%,transparent);white-space:nowrap}
.cc-chip.amber{--c:var(--amber)}.cc-chip.mute{--c:var(--muted)}.cc-chip.rose{--c:var(--rose)}
.cc-chip i{width:7px;height:7px;border-radius:50%;background:currentColor;display:block;animation:ccpulse 1.8s ease-out infinite}
@keyframes ccpulse{0%{box-shadow:0 0 0 0 color-mix(in srgb,currentColor 60%,transparent)}70%{box-shadow:0 0 0 8px transparent}100%{box-shadow:0 0 0 0 transparent}}
.cc-t{font:400 20px/1.1 var(--ccd);letter-spacing:.02em;text-transform:uppercase;margin-top:8px}
.cc-sub{color:var(--muted);font-size:12.5px;margin-top:2px}
.cc-bts{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}
.cc-bt{display:inline-flex;align-items:center;gap:7px;padding:4px 12px 4px 4px;border-radius:12px;background:var(--card2);border:1px solid var(--line2);font-weight:800;font-size:13px;color:var(--mint)}
.cc-bt img{width:28px;height:28px;border-radius:8px;display:block}
.cc-bt.cc-unk{color:var(--muted)}
.cc-q{display:inline-grid;place-items:center;width:28px;height:28px;border-radius:8px;background:#3a3345;color:#c4bad1;font:400 19px/1 var(--ccd)}
.cc-r{text-align:right}
.cc-r .cc-l{font-size:10.5px;font-weight:700;letter-spacing:.16em;text-transform:uppercase;color:var(--muted)}
.cc-r .cc-n{font:400 34px/1 var(--ccd);color:var(--cyan);letter-spacing:.03em;margin-top:4px;text-shadow:0 0 24px rgba(255,102,178,.4)}
.cc-hour.live .cc-r .cc-n.cc-hn{color:var(--amber)}
@media (prefers-reduced-motion:reduce){.sparkles i,.cc-chip i{animation:none}}

"""";

    private const string SkinScript = """"

(function(){
var D=@@DATA@@;
function $(i){return document.getElementById(i)}
function pad(n){return String(n).padStart(2,"0")}
function cd(ms){var s=Math.max(0,Math.floor(ms/1000));return {d:Math.floor(s/86400),h:Math.floor(s%86400/3600),m:Math.floor(s%3600/60),s:s%60,t:s}}
function nextReset(now){var d=new Date(now),t=Date.UTC(d.getUTCFullYear(),d.getUTCMonth(),d.getUTCDate(),14),r=t+((7-d.getUTCDay())%7)*864e5;if(r<=now)r+=7*864e5;return r}
function fmt(ms){return new Date(ms).toLocaleString("en-GB",{weekday:"short",day:"numeric",month:"short",hour:"2-digit",minute:"2-digit",hour12:false})}
function fmtEnd(ms){return new Date(ms).toLocaleString("en-GB",{hour:"2-digit",minute:"2-digit",hour12:false})}
D.windows.forEach(function(w){w.s=Date.parse(w.start);w.e=Date.parse(w.end)});
var NAMES={"gold-outfit":"Weekly Gold Outfit","pirate-shop-access":"Pirate Shop Access","vehicle-decal":"Weekly Vehicle & Decal"};
function badges(w){
  if(w.unknown)return '<div class="cc-bts"><span class="cc-bt cc-unk" title="Boosts are not announced yet"><span class="cc-q">?</span>Boosts not announced yet</span></div>';
  return '<div class="cc-bts">'+[["gold-outfit",w.gold],["pirate-shop-access",w.shop],["vehicle-decal",w.vehicle]].map(function(x){
    return '<span class="cc-bt" title="'+NAMES[x[0]]+'"><img alt="" src="'+(D.images[x[0]]||"")+'">'+x[1]+'&times;</span>'}).join("")+'</div>';
}
var el=$("ccHour"), shown="";
function paint(){
  if(!el)return;
  var now=Date.now(),live=null,next=null;
  D.windows.forEach(function(w){if(!live&&now>=w.s&&now<w.e)live=w;if(!next&&w.s>now)next=w});
  var key=(live?"L"+live.s:"")+"|"+(next?"N"+next.s:"");
  if(key!==shown){
    shown=key;
    var w=live||next, final=w&&w.unknown;
    el.className="cc-hour"+(live?" live":"");
    el.innerHTML='<div>'+
      (live?'<span class="cc-chip amber"><i></i>Treasure Hour live</span>':w?'<span class="cc-chip">Next Treasure Hour</span>':'<span class="cc-chip mute">No more Treasure Hours</span>')+
      (final?' <span class="cc-chip rose">Final</span>':'')+
      (w?'<div class="cc-t">'+fmt(w.s)+' \u2013 '+fmtEnd(w.e)+'</div>'+badges(w):'<div class="cc-sub" style="margin-top:8px">The last Treasure Hour has ended.</div>')+
    '</div><div class="cc-r"><div class="cc-l">Weekly reset in</div><div class="cc-n" id="ccReset"></div>'+(w?'<div class="cc-l" style="margin-top:12px">'+(live?'Hour ends in':'Hour starts in')+'</div><div class="cc-n cc-hn" id="ccHn"></div>':'')+'</div>';
  }
  var r=cd(nextReset(now)-now);$("ccReset").textContent=r.d+"d "+pad(r.h)+"h "+pad(r.m)+"m "+pad(r.s)+"s";
  var hn=$("ccHn");if(hn){var w2=live||next,c=cd((live?w2.e:w2.s)-now);hn.textContent=live?pad(Math.floor(c.t/3600))+":"+pad(c.m)+":"+pad(c.s):c.d+"d "+pad(c.h)+"h "+pad(c.m)+"m "+pad(c.s)+"s"}
}
var sp=$("ccSp");
if(sp){for(var i=0;i<12;i++){var e=document.createElement("i");e.textContent="\u2726";var a=(Math.sin(i*5.1+3)+1)/2,b=(Math.sin(i*9.9+7)+1)/2;e.style.left=(a*100)+"%";e.style.top=(b*100)+"%";e.style.fontSize=(10+((i*7)%14))+"px";e.style.animationDelay=(-(i*0.53)%3.2)+"s";e.style.animationDuration=(2.6+(i%4)*0.5)+"s";sp.appendChild(e)}}
paint();setInterval(paint,1000);
})();

"""";
}
