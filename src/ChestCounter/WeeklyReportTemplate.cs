namespace ChestCounter;

/// <summary>The page the weekly report is built from. Placeholders are filled in by WeeklyReport.</summary>
internal static class WeeklyReportTemplate
{
    public const string Html = """"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="color-scheme" content="dark">
<title>2xFlaw's Chest Counter - Weekly chances</title>
<style>
/*FONTS*/
:root{
  --bg:#0e0912;--bg2:#140b19;--card:#190f1e;--card2:#211428;--panel:#26172f;--hover:#31203d;
  --ink:#ece6f2;--muted:#8f8499;--faint:#5b4d68;--line:#2b1b35;--line2:#3f2a52;
  --acc:#ff66b2;--acc2:#d00080;--amber:#ffb84d;--mint:#3ddc97;--rose:#ff80aa;--coral:#ff6363;
  --display:"CC Display","Anton","Impact","Arial Narrow",sans-serif;
  --body:"CC Inter","Inter","Segoe UI Variable Text","Segoe UI",system-ui,-apple-system,sans-serif;
  --shadow:0 1px 2px rgba(0,0,0,.4),0 14px 40px rgba(0,0,0,.32);
}
*{box-sizing:border-box}
html{background:var(--bg);scroll-behavior:smooth}
body{margin:0;background:radial-gradient(1200px 560px at 12% -12%,rgba(208,0,128,.22),transparent 60%),radial-gradient(900px 480px at 100% 0,rgba(255,184,77,.08),transparent 55%),var(--bg);color:var(--ink);font:14px/1.5 var(--body);-webkit-font-smoothing:antialiased}
main{max-width:1180px;margin:0 auto;padding:22px 18px 70px}
h1,h2,h3{margin:0;font-family:var(--display);font-weight:400;letter-spacing:.01em;text-transform:uppercase}
.num{font-variant-numeric:tabular-nums}
.muted{color:var(--muted)}
.lbl{font-size:10.5px;font-weight:700;letter-spacing:.16em;text-transform:uppercase;color:var(--muted)}
button{font:inherit;color:inherit}
:focus-visible{outline:2px solid var(--acc);outline-offset:2px;border-radius:10px}

/* ---------- chips and buttons ---------- */
.chip{display:inline-flex;align-items:center;gap:7px;padding:5px 12px;border-radius:99px;font-size:10.5px;font-weight:800;letter-spacing:.14em;text-transform:uppercase;color:var(--c,var(--acc));background:color-mix(in srgb,var(--c,var(--acc)) 14%,transparent);border:1px solid color-mix(in srgb,var(--c,var(--acc)) 45%,transparent);white-space:nowrap}
.chip.amber{--c:var(--amber)}.chip.mint{--c:var(--mint)}.chip.rose{--c:var(--rose)}.chip.mute{--c:var(--muted)}
.chip .dot{width:7px;height:7px;border-radius:50%;background:currentColor;animation:pulse 1.8s ease-out infinite}
@keyframes pulse{0%{box-shadow:0 0 0 0 color-mix(in srgb,currentColor 60%,transparent)}70%{box-shadow:0 0 0 8px transparent}100%{box-shadow:0 0 0 0 transparent}}
.btn{appearance:none;cursor:pointer;border:1px solid var(--line2);background:linear-gradient(180deg,#2e1d3a,var(--panel));padding:9px 16px;border-radius:12px;font-weight:700;font-size:12.5px;transition:transform .12s,border-color .15s,background .15s,box-shadow .15s}
.btn:hover{border-color:rgba(255,102,178,.75);box-shadow:0 0 0 3px rgba(255,102,178,.12)}
.btn:active{transform:translateY(1px) scale(.99)}
.btn.on{background:linear-gradient(180deg,var(--acc),var(--acc2));border-color:var(--acc);color:#fff}
.btn.on:hover{box-shadow:0 0 0 3px rgba(255,102,178,.28)}

/* ---------- cards ---------- */
.card{position:relative;background:linear-gradient(180deg,rgba(255,255,255,.035),transparent 55%),var(--card);border:1px solid var(--line);border-radius:20px;box-shadow:var(--shadow)}
.card::before{content:"";position:absolute;left:18px;right:18px;top:0;height:1px;background:linear-gradient(90deg,transparent,rgba(255,255,255,.22),transparent)}
section{margin-top:22px}
.sh{display:flex;flex-wrap:wrap;align-items:baseline;justify-content:space-between;gap:6px 16px;margin:0 0 12px}
.sh h2{font-size:22px}
.sh .meta{font-size:12.5px;color:var(--muted)}

/* ---------- hero ---------- */
.hero{position:relative;overflow:hidden;border-radius:24px;border:1px solid var(--line);box-shadow:var(--shadow);min-height:250px;background:linear-gradient(120deg,#2a0f2c,#1b0f2e 55%,#0e0912)}
.hero img.banner{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;object-position:center 30%;opacity:.9}
.hero .shade{position:absolute;inset:0;background:linear-gradient(90deg,rgba(14,9,18,.92) 0,rgba(14,9,18,.55) 55%,rgba(14,9,18,.15)),linear-gradient(0deg,rgba(14,9,18,.9),transparent 55%)}
.hero .in{position:relative;display:flex;flex-wrap:wrap;justify-content:space-between;align-items:flex-end;gap:18px;padding:30px 30px 26px;min-height:250px}
.hero h1{font-size:clamp(40px,7vw,74px);line-height:.95}
.hero .eyebrow{margin-bottom:8px;color:var(--acc)}
.hero .chips{display:flex;flex-wrap:wrap;gap:8px;margin-top:16px;align-items:center}
.avatar{width:38px;height:38px;border-radius:50%;object-fit:cover;border:2px solid var(--acc);box-shadow:0 0 18px rgba(255,102,178,.45)}
.resetbox{text-align:right}
.resetbox .big{font:400 clamp(30px,4.6vw,46px)/1 var(--display);color:var(--acc);margin:6px 0 4px;letter-spacing:.02em;text-shadow:0 0 28px rgba(255,102,178,.45)}
.sparkles{position:absolute;inset:0;pointer-events:none}
.sparkles i{position:absolute;color:#fff;font-style:normal;opacity:0;animation:tw 3.4s ease-in-out infinite}
@keyframes tw{0%,100%{opacity:0;transform:scale(.4) rotate(0)}45%,55%{opacity:.95;transform:scale(1) rotate(35deg)}}

/* ---------- treasure hour strip ---------- */
.hour{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:10px 18px;margin-top:14px;padding:14px 18px;border-radius:16px;border:1px solid var(--line);background:var(--card)}
.hour.live{border-color:rgba(255,184,77,.6);background:linear-gradient(90deg,rgba(255,184,77,.16),var(--card));box-shadow:0 0 0 1px rgba(255,184,77,.15),0 0 38px rgba(255,184,77,.12)}
.hour .t{font:400 22px/1 var(--display);letter-spacing:.02em;text-transform:uppercase}
.hour .cd{font:400 30px/1 var(--display);color:var(--acc);letter-spacing:.03em}
.hour.live .cd{color:var(--amber)}
.bts{display:flex;flex-wrap:wrap;gap:8px;margin-top:10px}
.bt{display:inline-flex;align-items:center;gap:7px;padding:4px 12px 4px 4px;border-radius:12px;background:var(--panel);border:1px solid var(--line2);font-weight:800;font-size:13px;color:var(--mint)}
.bt img{width:30px;height:30px;border-radius:8px;display:block}
.bt.unk{color:var(--muted)}
.q{display:inline-grid;place-items:center;width:30px;height:30px;border-radius:8px;background:#3a3345;color:#c4bad1;font:400 20px/1 var(--display)}
.unkbox{display:flex;align-items:center;gap:12px;color:var(--muted)}
.unkbox .q{width:44px;height:44px;border-radius:12px;font-size:28px}
.stale{display:none;margin-top:12px;padding:10px 16px;border-radius:14px;border:1px solid rgba(255,99,99,.5);background:rgba(255,99,99,.10);color:#ffb3b3;font-size:13px}

/* ---------- goal ---------- */
.goal{display:grid;grid-template-columns:230px 1fr;gap:26px;padding:24px;overflow:hidden;--acc2x:var(--acc)}
.goal.boost{background:linear-gradient(135deg,rgba(255,184,77,.14),transparent 55%),var(--card);border-color:rgba(255,184,77,.45);--acc2x:var(--amber)}
.goal:not(.boost){background:linear-gradient(135deg,rgba(255,102,178,.13),transparent 55%),var(--card)}
.pic{position:relative;width:230px;height:230px}
.pic img{width:100%;height:100%;border-radius:20px;display:block;border:2px solid color-mix(in srgb,var(--acc2x) 60%,transparent);box-shadow:0 14px 40px rgba(0,0,0,.5),0 0 40px color-mix(in srgb,var(--acc2x) 28%,transparent)}
.pic i{position:absolute;font-style:normal;color:#fff;opacity:0;animation:tw 3.2s ease-in-out infinite;pointer-events:none}
.goal.boost .pic i{color:var(--amber)}
.gtop{display:flex;flex-wrap:wrap;gap:8px;align-items:center}
.goal h3{font-size:clamp(26px,3.6vw,38px);line-height:1.02;margin:12px 0 2px}
.gpct{font:400 clamp(52px,8vw,84px)/1 var(--display);color:var(--acc2x);letter-spacing:.01em;margin:2px 0 0;text-shadow:0 0 30px color-mix(in srgb,var(--acc2x) 35%,transparent)}
.gstats{display:flex;flex-wrap:wrap;gap:4px 22px;margin-top:6px;color:var(--muted);font-size:13px}
.gstats b{color:var(--ink);font-weight:700}
.gproj{margin-top:6px;font-size:13px;font-weight:600}
.path{position:relative;margin:26px 8px 34px}
.track{height:10px;border-radius:99px;background:var(--panel);overflow:hidden}
.pfill{height:100%;width:0;border-radius:99px;background:linear-gradient(90deg,var(--acc2),var(--acc),var(--rose));position:relative;transition:width .9s cubic-bezier(.2,.8,.2,1)}
.pfill::after{content:"";position:absolute;inset:0;background:linear-gradient(100deg,transparent 30%,rgba(255,255,255,.55) 50%,transparent 70%);transform:translateX(-100%);animation:shim 2.6s ease-in-out infinite}
@keyframes shim{to{transform:translateX(100%)}}
.ms{position:absolute;top:50%;width:14px;height:14px;border-radius:50%;background:var(--card);border:2px solid var(--line2);transform:translate(-50%,-50%);transition:background .4s,border-color .4s}
.ms.hit{background:var(--mint);border-color:var(--mint)}
.ml{position:absolute;top:20px;transform:translateX(-50%);font-size:11px;color:var(--muted)}
.ml.hit{color:var(--mint)}
.mk{position:absolute;top:50%;width:18px;height:18px;border-radius:50%;background:var(--ink);border:3px solid var(--acc2x);transform:translate(-50%,-50%);box-shadow:0 0 0 6px color-mix(in srgb,var(--acc2x) 28%,transparent);transition:left .9s cubic-bezier(.2,.8,.2,1)}

/* ---------- planner ---------- */
.planner{padding:20px 24px}
.ptop{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:center;gap:8px 16px}
.pgrid{display:grid;grid-template-columns:1fr auto 1fr;gap:20px;align-items:center;margin-top:16px}
.chests{display:flex;flex-wrap:wrap;gap:8px;margin-top:8px}
.pv{font:400 38px/1.05 var(--display);letter-spacing:.01em}
.pv.hot{color:var(--amber)}
.arrow{font-size:26px;color:var(--faint)}

/* ---------- odds table ---------- */
.rows{padding:6px 10px 10px}
.rhead,.row{display:grid;grid-template-columns:minmax(220px,1.1fr) minmax(200px,1.6fr) 118px;gap:18px;align-items:center;padding:10px 14px}
.rhead{padding-top:16px;padding-bottom:6px}
.row{position:relative;border-radius:14px;cursor:pointer;border:1px solid transparent;transition:background .15s,border-color .15s}
.row+.row{margin-top:2px}
.row:hover{background:var(--hover)}
.row.sel{background:rgba(255,102,178,.12);border-color:rgba(255,102,178,.5)}
.row.gz{cursor:default}
.row .who{display:flex;gap:14px;align-items:center;min-width:0}
.row img{width:46px;height:46px;border-radius:12px;flex:none}
.row .nm{font-weight:800;font-size:15px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.row .sb{font-size:12px;color:var(--muted)}
.row .sb .hot{color:var(--amber);font-weight:700}
.bar{position:relative;height:9px;border-radius:99px;background:var(--panel)}
.bar .gh{position:absolute;top:0;bottom:0;border-radius:99px;border:1px dashed var(--acc);background:rgba(255,102,178,.22);opacity:0;transition:opacity .2s,left .7s,width .7s}
.bar .gh.hot{border-color:var(--amber);background:rgba(255,184,77,.30)}
.row.sel .gh,.row:hover .gh{opacity:1}
.bar .fl{position:absolute;left:0;top:0;bottom:0;border-radius:99px;background:linear-gradient(90deg,var(--acc2),var(--acc));width:0;transition:width .9s cubic-bezier(.2,.8,.2,1)}
.pc{text-align:right}
.pc .p{font:400 24px/1 var(--display);letter-spacing:.01em}
.row.sel .pc .p{color:var(--acc)}
.pc .q2{font-size:12px;color:var(--muted);height:16px;margin-top:3px;opacity:0;transition:opacity .2s}
.row.sel .q2,.row:hover .q2{opacity:1}
.empty{padding:28px;text-align:center;color:var(--mint);font:400 24px var(--display);text-transform:uppercase}

/* ---------- schedule ---------- */
.sched{display:grid;grid-template-columns:repeat(auto-fill,minmax(310px,1fr));gap:12px}
.win{padding:16px 18px}
.win .when{font-weight:800;font-size:15px}
.win .row2{display:flex;justify-content:space-between;align-items:center;gap:10px;margin-top:12px}
.win.live{border-color:rgba(255,184,77,.65);box-shadow:0 0 34px rgba(255,184,77,.14)}
.win.past{opacity:.5}

/* ---------- stats ---------- */
.kpis{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}
.kpi{padding:18px 20px}
.kpi .v{font:400 44px/1 var(--display);margin:8px 0 4px;color:var(--c,var(--ink))}
.kpi .s{font-size:12.5px;color:var(--muted)}
.days{display:flex;align-items:flex-end;gap:12px;height:180px;padding:22px 22px 14px}
.day{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;gap:8px;height:100%}
.day .b{width:100%;max-width:70px;border-radius:10px 10px 4px 4px;background:linear-gradient(180deg,var(--acc),var(--acc2));height:0;transition:height 1s cubic-bezier(.2,.8,.2,1);position:relative;min-height:3px}
.day .b.zero{background:var(--line2)}
.day .b span{position:absolute;top:-22px;left:50%;transform:translateX(-50%);font-size:12px;font-weight:800}
.day .l{font-size:11.5px;color:var(--muted)}
.day.today .b{background:linear-gradient(180deg,var(--amber),#ff8a3d)}

/* ---------- tables ---------- */
.tw{overflow-x:auto}
table{width:100%;border-collapse:collapse;font-variant-numeric:tabular-nums}
th,td{text-align:left;padding:11px 16px;border-bottom:1px solid var(--line);vertical-align:top}
th{font-size:10.5px;font-weight:800;letter-spacing:.14em;text-transform:uppercase;color:var(--muted);white-space:nowrap;position:sticky;top:0;background:var(--card2)}
tbody tr:hover td{background:rgba(255,255,255,.03)}
tr.win-row td{background:rgba(255,184,77,.08)}
.tools{display:flex;gap:10px;flex-wrap:wrap;align-items:center}
input[type=search]{background:var(--panel);border:1px solid var(--line2);border-radius:12px;color:var(--ink);padding:9px 14px;font:inherit;min-width:230px}
input[type=search]:focus{outline:2px solid var(--acc);border-color:transparent}
.note{margin-top:14px;font-size:12.5px;color:var(--muted);line-height:1.65}
footer{margin-top:36px;padding-top:20px;border-top:1px solid var(--line);color:var(--muted);font-size:12.5px;line-height:1.7}
footer h3{font-size:14px;letter-spacing:.12em;margin-bottom:6px;color:var(--ink)}
footer a{color:var(--acc)}
footer .made{margin-top:14px;font:400 18px var(--display);letter-spacing:.08em;text-transform:uppercase;color:var(--acc)}

@media (max-width:900px){
  .goal{grid-template-columns:1fr}.pic{margin:0 auto}
  .pgrid{grid-template-columns:1fr}.arrow{transform:rotate(90deg);text-align:center}
  .kpis{grid-template-columns:repeat(2,1fr)}
  .rhead{display:none}.row{grid-template-columns:1fr 100px}.row .bar{grid-column:1 / -1;order:3}
  .resetbox{text-align:left}
}
@media (prefers-reduced-motion:reduce){*{animation:none!important;transition:none!important}}
@media print{body{background:#fff;color:#000}.btn,.sparkles,.pic i{display:none}.card,.hour{box-shadow:none;border-color:#bbb;background:#fff}}
</style>
</head>
<body>
<main>

<header class="hero">
  @@BANNER@@
  <div class="shade"></div>
  <div class="sparkles" id="heroSparks"></div>
  <div class="in">
    <div>
      <div class="lbl eyebrow">2xFlaw's Chest Counter</div>
      <h1>Weekly chances</h1>
      <div class="chips">
        @@AVATAR@@
        <span class="chip" id="chipProfile"></span>
        <span class="chip mute" id="chipGen"></span>
        <button class="btn" id="btnTz" type="button" title="Show the times in GMT or in your own time zone"></button>
        <button class="btn" id="btnCopy" type="button">Copy summary</button>
      </div>
    </div>
    <div class="resetbox">
      <div class="lbl">Weekly reset in</div>
      <div class="big num" id="resetCd">--</div>
      <div class="muted" style="font-size:12.5px">Every Sunday at 14:00 GMT &middot; <span id="resetLocal"></span></div>
    </div>
  </div>
</header>

<div class="stale" id="stale"></div>
<div class="hour" id="hour"></div>

<section id="goalSec">
  <div class="sh"><h2>Your goal</h2><span class="meta">Defaults to the prize you are closest to. Click any prize in the table to aim for it.</span></div>
  <div class="card goal" id="goal"></div>
</section>

<section>
  <div class="sh"><h2>Treasure Hour planner</h2><span class="meta">What a number of chests is worth for your goal, with and without the Treasure Hour.</span></div>
  <div class="card planner" id="planner"></div>
</section>

<section>
  <div class="sh"><h2>Odds this week</h2><span class="meta" id="oddsMeta"></span></div>
  <div class="card rows" id="rows"></div>
</section>

<section>
  <div class="sh"><h2>Treasure Hours</h2><span class="meta" id="schedMeta"></span></div>
  <div class="sched" id="sched"></div>
  <p class="note" id="schedNote"></p>
</section>

<section>
  <div class="sh"><h2>This week</h2><span class="meta">Since the last reset (Sunday 14:00 GMT).</span></div>
  <div class="kpis" id="kpis"></div>
  <div class="card days" id="days" style="margin-top:12px"></div>
</section>

<section>
  <div class="sh"><h2>Matches</h2>
    <div class="tools"><input type="search" id="q" placeholder="Filter matches, treasures, teammates"><span class="meta" id="mcount"></span></div>
  </div>
  <div class="card tw"><table id="matches"><thead><tr><th>Match</th><th>Mode</th><th>Place</th><th>Your chests</th><th>Teammates seen</th><th>Treasures</th></tr></thead><tbody></tbody></table></div>
  <p class="note">Treasure ownership of teammates is not stored in replays, so it is unavailable, not zero. Teammate chest counts are a minimum: Fortnite only records what happens near you.</p>
</section>

<section id="manualSec">
  <div class="sh"><h2>Manual history</h2><span class="meta">Entries added by hand in Weekly tools.</span></div>
  <div class="card tw"><table id="manual"><thead><tr><th>When</th><th>Your chests</th><th>Teammates only</th><th>Note</th></tr></thead><tbody></tbody></table></div>
</section>

<footer>
  <h3>How the numbers work</h3>
  <p>For every chest you opened since the weekly reset, a prize has a 1-in-N chance. Chests opened inside a Treasure Hour use the boosted rate. The big percentage is the chance of at least one drop so far: <b>1 &minus; &prod;(1 &minus; rate)</b>, so it only rises when you open chests. The planner and the dashed bars show what extra chests would be worth. Except for Gizmos, you cannot get the same prize twice, so prizes you tick as owned in Weekly rewards are hidden.</p>
  <p>Odds are Epic's published per-chest rates (<span id="oddsSource"></span>), constant except during Treasure Hours. <span id="oddsStatus"></span> This is a probability, not a promise.</p>
  <div class="made">Made by 2xFlaw</div>
</footer>
</main>

<script id="data" type="application/json">@@DATA@@</script>
<script>
(function(){
"use strict";
var D=JSON.parse(document.getElementById("data").textContent);
function $(s){return document.querySelector(s)}
function store(k,d){try{var v=localStorage.getItem(k);return v===null?d:JSON.parse(v)}catch(e){return d}}
function save(k,v){try{localStorage.setItem(k,JSON.stringify(v))}catch(e){}}
function esc(s){return String(s==null?"":s).replace(/[&<>"']/g,function(c){return {"&":"&amp;","<":"&lt;",">":"&gt;","\"":"&quot;","'":"&#39;"}[c]})}
function n0(n){return Number(n).toLocaleString("en-US",{maximumFractionDigits:0})}
function pct(c){var v=c*100;if(v<=0)return "0%";return (v>=10?v.toFixed(1):v>=1?v.toFixed(2):v.toFixed(4))+"%"}
function after(c,den,boost,n){var p=Math.min(boost/den,0.999999);return 1-(1-c)*Math.exp(n*Math.log1p(-p))}
function toHalf(c,den){return c>=0.5?0:Math.log(0.5/(1-c))/Math.log1p(-1/den)}
function pad(n){return String(n).padStart(2,"0")}

var pick=store("cc.pick",D.pick||null), plan=store("cc.plan",100), tz=store("cc.tz","local");
var W=D.windows.map(function(w){w.s=Date.parse(w.start);w.e=Date.parse(w.end);return w}).sort(function(a,b){return a.s-b.s});
var CHESTS=[25,50,100,200,400];
var IMG={}, NAME={};
D.prizes.forEach(function(p){IMG[p.key]=p.img;NAME[p.key]=p.name});
var shownGoal=0, prevFill={}, state="";

// The boosts of the last two Treasure Hours are not announced: they count as normal odds (1x).
function boostOf(w,k){return !w||w.unknown?1:k==="gold-outfit"?w.gold:k==="pirate-shop-access"?w.shop:k==="vehicle-decal"?w.vehicle:1}
function windows(){var now=Date.now();return {live:W.find(function(w){return now>=w.s&&now<w.e})||null,next:W.find(function(w){return w.s>now})||null}}
function fmt(ms,opt){var o=Object.assign({},opt);if(tz==="utc")o.timeZone="UTC";return new Date(ms).toLocaleString("en-GB",o)}
function whenText(w){
  var a=fmt(w.s,{weekday:"short",day:"numeric",month:"short",hour:"2-digit",minute:"2-digit",hour12:false});
  var b=fmt(w.e,{hour:"2-digit",minute:"2-digit",hour12:false});
  return a+" \u2013 "+b+(tz==="utc"?" GMT":"");
}
function cd(ms){var s=Math.max(0,Math.floor(ms/1000));var d=Math.floor(s/86400),h=Math.floor(s%86400/3600),m=Math.floor(s%3600/60),x=s%60;return {d:d,h:h,m:m,s:x,total:s}}
function badges(w){
  if(w.unknown)return '<div class="bts"><span class="bt unk" title="Boosts are not announced yet"><span class="q">?</span>Boosts not announced yet</span></div>';
  return '<div class="bts">'+[["gold-outfit",w.gold],["pirate-shop-access",w.shop],["vehicle-decal",w.vehicle]].map(function(x){
    return '<span class="bt" title="'+esc(NAME[x[0]]||x[0])+'"><img alt="" src="'+(IMG[x[0]]||"")+'">'+x[1]+'&times;</span>'}).join("")+'</div>';
}

function sparkle(host,count,seed){
  host.innerHTML="";
  for(var i=0;i<count;i++){
    var e=document.createElement("i");e.textContent="\u2726";
    var r=(Math.sin(seed*13.7+i*5.1)+1)/2, q=(Math.sin(seed*7.3+i*9.9)+1)/2;
    e.style.left=(r*100)+"%";e.style.top=(q*100)+"%";
    e.style.fontSize=(10+((i*7)%14))+"px";
    e.style.animationDelay=(-(i*0.53)%3.2)+"s";e.style.animationDuration=(2.6+(i%4)*0.5)+"s";
    host.appendChild(e);
  }
}

function countUp(el,from,to,fmtFn){
  if(window.matchMedia&&matchMedia("(prefers-reduced-motion: reduce)").matches){el.textContent=fmtFn(to);return}
  var t0=performance.now(),dur=900;
  function step(t){var k=Math.min(1,(t-t0)/dur),e=1-Math.pow(1-k,3);el.textContent=fmtFn(from+(to-from)*e);if(k<1)requestAnimationFrame(step)}
  requestAnimationFrame(step);
}

function ordered(live){
  var P=D.prizes.map(function(p){var q=Object.assign({},p);q.boost=boostOf(live,p.key);return q});
  var open=P.filter(function(p){return !p.owned});
  var main=open.filter(function(p){return p.key!=="gizmo"}).sort(function(a,b){return (b.chance-a.chance)||(a.den-b.den)});
  var giz=open.filter(function(p){return p.key==="gizmo"});
  return {list:main.concat(giz),cands:main,hidden:P.length-open.length,all:P};
}

function render(){
  var w=windows(), o=ordered(w.live);
  var goal=pick?o.cands.find(function(p){return p.key===pick}):null;
  var auto=!goal; if(!goal)goal=o.cands[0]||null;

  // ----- hour strip -----
  var hour=$("#hour");
  if(w.live){hour.className="hour live";hour.innerHTML='<div><span class="chip amber"><span class="dot"></span>Treasure Hour live</span>'+(w.live.unknown?' <span class="chip rose">Final</span>':'')+'<div class="t" style="margin-top:8px">'+(w.live.unknown?'Boosts not announced yet':'Boosted odds are on now')+'</div><div class="muted">'+esc(whenText(w.live))+'</div>'+badges(w.live)+'</div><div style="text-align:right"><div class="lbl">Ends in</div><div class="cd num" id="hourCd"></div></div>'}
  else if(w.next){hour.className="hour";hour.innerHTML='<div><span class="chip">Next Treasure Hour</span>'+(w.next.unknown?' <span class="chip rose">Final</span>':'')+'<div class="t" style="margin-top:8px">'+esc(whenText(w.next))+'</div>'+badges(w.next)+'</div><div style="text-align:right"><div class="lbl">Starts in</div><div class="cd num" id="hourCd"></div></div>'}
  else{hour.className="hour";hour.innerHTML='<div><span class="chip mute">No more Treasure Hours</span><div class="muted" style="margin-top:8px">The last Treasure Hour has ended.</div></div>'}

  // ----- goal -----
  var g=$("#goal");
  if(!goal){
    g.className="card goal";g.style.display="block";
    g.innerHTML='<div class="empty">Everything is ticked as owned. Fresh prizes arrive at the reset.</div>';
  }else{
    var boosted=goal.boost>1;
    g.className="card goal"+(boosted?" boost":"");g.style.display="";
    var half=toHalf(goal.chance,goal.den);
    var gainNow=after(goal.chance,goal.den,goal.boost,plan)-goal.chance;
    var gainNorm=after(goal.chance,goal.den,1,plan)-goal.chance;
    g.innerHTML=
      '<div class="pic"><img alt="" src="'+goal.img+'"><span id="picSp"></span></div>'+
      '<div><div class="gtop"><span class="chip '+(auto?"":"amber")+'">'+(auto?"Next closest":"Your pick")+'</span>'+(boosted?'<span class="chip amber"><span class="dot"></span>Treasure Hour '+goal.boost+'&times;</span>':'')+'<button class="btn '+(auto?"on":"")+'" id="btnAuto" type="button">Auto: next closest</button></div>'+
      '<h3>'+esc(goal.name)+'</h3>'+
      '<div class="gpct num" id="gpct">0%</div><div class="muted" style="margin-top:-2px">cumulative chance this week</div>'+
      '<div class="gstats"><span><b>1 in '+n0(goal.den)+'</b> per chest</span><span><b>'+n0(D.chests)+'</b> chests opened this week</span><span>'+(half<=0?'<b style="color:var(--mint)">Past the 50% mark</b>':'About <b>'+n0(Math.ceil(half))+'</b> more chests to reach 50%')+'</span></div>'+
      '<div class="gproj" style="color:'+(boosted?"var(--amber)":"var(--muted)")+'">If you open '+n0(plan)+' more chests: +'+pct(gainNorm)+' normally'+(boosted?' &middot; <b>+'+pct(gainNow)+' during the Treasure Hour</b>':'')+'</div>'+
      '<div class="path" id="path"><div class="track"><div class="pfill" id="pfill"></div></div></div></div>';
    var path=$("#path");
    [[0,"0"],[0.25,"25%"],[0.5,"50%"],[0.75,"75%"],[1,"100%"]].forEach(function(m){
      var hit=m[0]>0&&goal.chance>=m[0];
      var d=document.createElement("span");d.className="ms"+(hit?" hit":"");d.style.left=(m[0]*100)+"%";path.appendChild(d);
      var l=document.createElement("span");l.className="ml"+(hit?" hit":"");l.style.left=(m[0]*100)+"%";l.textContent=m[1];path.appendChild(l);
    });
    if(goal.chance>0.003&&goal.chance<1){var mk=document.createElement("span");mk.className="mk";mk.style.left="0%";mk.id="mk";path.appendChild(mk)}
    sparkle($("#picSp"),7,goal.key.length);
    var sp=$("#picSp");sp.style.cssText="position:absolute;inset:-14px;display:block";
    requestAnimationFrame(function(){requestAnimationFrame(function(){
      $("#pfill").style.width=(goal.chance*100)+"%";
      var mk2=$("#mk");if(mk2)mk2.style.left=(goal.chance*100)+"%";
    })});
    countUp($("#gpct"),shownGoal,goal.chance,pct);shownGoal=goal.chance;
    $("#btnAuto").onclick=function(){pick=null;save("cc.pick",null);render()};
  }

  // ----- planner -----
  var pl=$("#planner");
  if(!goal){pl.style.display="none"}
  else{
    pl.style.display="";
    var win=w.live||w.next;
    var b=boostOf(win,goal.key);
    var nn=after(goal.chance,goal.den,1,plan), bb=after(goal.chance,goal.den,b,plan);
    var statusChip=w.live?'<span class="chip amber"><span class="dot"></span>Live now</span>':(w.next?'<span class="chip">Next Treasure Hour</span>':'<span class="chip mute">No more Treasure Hours</span>');
    var boostBlock=b>1
      ?'<div><div class="lbl" style="color:var(--amber)">Treasure Hour '+b+'&times;</div><div class="pv hot num" id="pvB">0%</div><div style="color:var(--amber)">+'+pct(bb-goal.chance)+'</div></div>'
      :(win&&win.unknown
        ?'<div class="unkbox"><span class="q">?</span><span>Boosts for this Treasure Hour are not announced yet.</span></div>'
        :'<div><div class="muted">No Treasure Hour boost for this prize.</div></div>');
    pl.innerHTML=
      '<div class="ptop"><div>'+statusChip+'</div><div class="muted">'+(win?esc(whenText(win))+(win.unknown?" (final)":""):"")+'</div></div>'+
      '<div class="pgrid"><div><div class="lbl">If you open this many chests</div><div class="chests" id="chests"></div></div>'+
      '<div><div class="lbl">Normal rate</div><div class="pv num" id="pvN">0%</div><div class="muted">+'+pct(nn-goal.chance)+'</div></div>'+
      boostBlock+'</div>';
    var ch=$("#chests");
    CHESTS.forEach(function(n){var bt=document.createElement("button");bt.type="button";bt.className="btn"+(n===plan?" on":"");bt.textContent=n0(n);bt.onclick=function(){plan=n;save("cc.plan",n);render()};ch.appendChild(bt)});
    countUp($("#pvN"),goal.chance,nn,pct);
    if(b>1)countUp($("#pvB"),goal.chance,bb,pct);
  }

  // ----- odds table -----
  var rows=$("#rows");
  var html='<div class="rhead lbl"><span>Prize</span><span>Cumulative odds this week &middot; dashed = if you open '+n0(plan)+' chests</span><span style="text-align:right">Chance</span></div>';
  if(!o.list.length)html+='<div class="empty">Everything is ticked as owned</div>';
  o.list.forEach(function(p){
    var sel=goal&&p.key===goal.key, gz=p.key==="gizmo", hot=p.boost>1;
    var ah=after(p.chance,p.den,p.boost,plan);
    var gl=Math.min(100,p.chance*100), gw=Math.max(0,Math.min(100,ah*100)-gl);
    html+='<div class="row'+(sel?" sel":"")+(gz?" gz":"")+'" data-k="'+p.key+'" tabindex="'+(gz?-1:0)+'" role="'+(gz?"row":"button")+'">'+
      '<div class="who"><img alt="" src="'+p.img+'"><div style="min-width:0"><div class="nm">'+esc(p.name)+'</div><div class="sb">1 in '+n0(p.den)+' per chest'+(gz?' &middot; can repeat':'')+(hot?' <span class="hot">&middot; '+p.boost+'&times; now</span>':'')+'</div></div></div>'+
      '<div class="bar"><div class="gh'+(hot?" hot":"")+'" style="left:'+gl+'%;width:'+gw+'%"></div><div class="fl" data-w="'+(p.chance*100)+'" style="width:'+((prevFill[p.key]||0))+'%"></div></div>'+
      '<div class="pc"><div class="p num">'+pct(p.chance)+'</div><div class="q2 num">\u2192 '+pct(ah)+'</div></div></div>';
  });
  rows.innerHTML=html;
  requestAnimationFrame(function(){requestAnimationFrame(function(){
    rows.querySelectorAll(".fl").forEach(function(f){var k=f.parentNode.parentNode.getAttribute("data-k");var v=parseFloat(f.getAttribute("data-w"));f.style.width=v+"%";prevFill[k]=v})
  })});
  rows.querySelectorAll(".row").forEach(function(r){
    var k=r.getAttribute("data-k");if(k==="gizmo")return;
    function choose(){pick=k;save("cc.pick",k);render();$("#goalSec").scrollIntoView({behavior:"smooth",block:"start"})}
    r.onclick=choose;r.onkeydown=function(e){if(e.key==="Enter"||e.key===" "){e.preventDefault();choose()}};
  });
  $("#oddsMeta").textContent=o.hidden>0?(o.hidden+" owned prize"+(o.hidden===1?"":"s")+" hidden \u00b7 untick in Weekly rewards to bring back"):"All prizes still to find";
  renderSchedule(w);
  tick();
}

function renderSchedule(w){
  var now=Date.now(), sc=$("#sched"), h="";
  W.forEach(function(x){
    var live=now>=x.s&&now<x.e, past=now>=x.e, nxt=w.next&&w.next===x;
    h+='<div class="card win'+(live?" live":"")+(past?" past":"")+'"><div class="when">'+esc(whenText(x))+'</div>'+badges(x)+
      '<div class="row2"><span>'+(live?'<span class="chip amber"><span class="dot"></span>Live</span>':past?'<span class="chip mute">Ended</span>':nxt?'<span class="chip">Next</span>':'<span class="chip mute">Upcoming</span>')+(x.unknown?' <span class="chip rose">Final</span>':' <span class="chip mint">Confirmed</span>')+'</span>'+
      '<span class="muted num" data-cd="'+(live?x.e:x.s)+'" data-live="'+(live?1:0)+'"></span></div></div>';
  });
  sc.innerHTML=h||'<div class="muted">No Treasure Hours in the odds file.</div>';
  var known=W.filter(function(x){return !x.unknown&&x.e>now}).length, last=W.filter(function(x){return x.unknown&&x.e>now}).length;
  $("#schedMeta").innerHTML=(known||last)?(known+" with announced boosts"+(last?" \u00b7 "+last+" final, boosts not announced":"")+" \u00b7 times in <b>"+(tz==="utc"?"GMT":"your local time")+"</b>"):("Times in <b>"+(tz==="utc"?"GMT":"your local time")+"</b>");
  $("#schedNote").textContent=W.some(function(x){return x.unknown})?"The Treasure Hours marked Final are confirmed to be the last two. What they boost is not announced yet, so the report counts them as normal odds (no boost) until it is.":"";
}

function tick(){
  var now=Date.now();
  var r=cd(D.resetUtc-now);
  $("#resetCd").textContent=r.d+"d "+pad(r.h)+"h "+pad(r.m)+"m "+pad(r.s)+"s";
  var w=windows();
  var key=(w.live?"L"+w.live.s:"")+"|"+(w.next?"N"+w.next.s:"");
  if(state&&key!==state){state=key;render();return}
  state=key;
  var hc=$("#hourCd");
  if(hc){var t=w.live?w.live.e-now:(w.next?w.next.s-now:0);var c=cd(t);hc.textContent=w.live?(pad(Math.floor(c.total/3600))+":"+pad(c.m)+":"+pad(c.s)):(c.d+"d "+pad(c.h)+"h "+pad(c.m)+"m "+pad(c.s)+"s")}
  document.querySelectorAll("[data-cd]").forEach(function(e){
    var t=parseFloat(e.getAttribute("data-cd"))-now;if(t<=0){e.textContent="";return}
    var c=cd(t);e.textContent=(e.getAttribute("data-live")==="1"?"ends in ":"in ")+(c.d>0?c.d+"d ":"")+pad(c.h)+":"+pad(c.m)+":"+pad(c.s);
  });
  var age=(now-D.generatedUtc)/60000, st=$("#stale");
  if(age>30){st.style.display="block";st.textContent="This report was made "+(age>=120?Math.floor(age/60)+" hours":Math.floor(age)+" minutes")+" ago. Your chest counts may be out of date: open it again from the app (Weekly tools \u203a Weekly report) to refresh."}
  else st.style.display="none";
}

function chrome(){
  $("#chipProfile").textContent=D.profile||"Profile";
  $("#chipGen").textContent="Updated "+fmt(D.generatedUtc,{day:"numeric",month:"short",hour:"2-digit",minute:"2-digit",hour12:false});
  $("#resetLocal").textContent=fmt(D.resetUtc,{weekday:"short",day:"numeric",month:"short",hour:"2-digit",minute:"2-digit",hour12:false})+(tz==="utc"?" GMT":" your time");
  $("#btnTz").textContent="Times: "+(tz==="utc"?"GMT":"Local");
  sparkle($("#heroSparks"),14,3);
  var o=D.odds||{};
  $("#oddsSource").innerHTML=o.source?'<a href="'+esc(o.source)+'" target="_blank" rel="noopener">fortnite.bot</a>':"fortnite.bot";
  $("#oddsStatus").textContent=(o.status||"")+(o.captured?" Treasure Hour list captured "+fmt(Date.parse(o.captured),{day:"numeric",month:"short",year:"numeric"})+".":"");
}

function renderStats(){
  var k=D.kpis||{}, h="";
  [["Your chests this week",k.mine,"var(--acc)","opened or destroyed by you"],
   ["Teammates seen",k.mates,"var(--amber)","minimum, limited to replay visibility"],
   ["Squad total",k.mine+k.mates,"var(--mint)","you + teammates seen"],
   ["Manual entries",k.manual,"var(--rose)","chests you added by hand"]].forEach(function(x){
    h+='<div class="card kpi" style="--c:'+x[2]+'"><div class="lbl">'+x[0]+'</div><div class="v num">'+n0(x[1]||0)+'</div><div class="s">'+x[3]+'</div></div>'});
  $("#kpis").innerHTML=h;
  var max=Math.max.apply(null,D.daily.map(function(d){return d.chests}).concat([1]));
  var dh="", todayIdx=-1, now=Date.now();
  D.daily.forEach(function(d,i){if(now>=Date.parse(d.start))todayIdx=i});
  D.daily.forEach(function(d,i){
    var lab=new Date(Date.parse(d.start)).toLocaleDateString("en-GB",{weekday:"short",timeZone:tz==="utc"?"UTC":undefined});
    dh+='<div class="day'+(i===todayIdx?" today":"")+'"><div class="b'+(d.chests?"":" zero")+'" data-h="'+(d.chests/max*100)+'">'+(d.chests?'<span>'+d.chests+'</span>':'')+'</div><div class="l">'+lab+'</div></div>'});
  $("#days").innerHTML=dh;
  requestAnimationFrame(function(){requestAnimationFrame(function(){document.querySelectorAll(".day .b").forEach(function(b){b.style.height=Math.max(3,parseFloat(b.getAttribute("data-h"))*0.78)+"%"})})});
}

function renderMatches(){
  var q=($("#q").value||"").toLowerCase().trim(), tb=$("#matches tbody"), h="", shown=0;
  D.matches.forEach(function(m){
    var text=(m.mode+" "+m.place+" "+m.treasures.join(" ")+" "+m.mates.map(function(t){return t.name}).join(" ")).toLowerCase();
    if(q&&text.indexOf(q)<0)return;shown++;
    var when=m.start?fmt(Date.parse(m.start),{day:"numeric",month:"short",hour:"2-digit",minute:"2-digit",hour12:false}):"";
    h+='<tr class="'+(m.win?"win-row":"")+'"><td class="num" style="white-space:nowrap">'+esc(when)+'</td><td>'+esc(m.mode)+'</td><td>'+esc(m.place)+'</td><td class="num"><b>'+m.chests+'</b></td>'+
      '<td>'+(m.mates.length?'<b class="num">\u2265 '+m.matesTotal+'</b><div class="muted">'+m.mates.map(function(t){return esc(t.name)+": "+t.chests}).join(", ")+'</div>':'<span class="muted">\u2014</span>')+'</td>'+
      '<td>'+(m.treasures.length?esc(m.treasures.join(", ")):'<span class="muted">\u2014</span>')+'</td></tr>'});
  tb.innerHTML=h||'<tr><td colspan="6" class="muted" style="text-align:center;padding:26px">No matches found.</td></tr>';
  $("#mcount").textContent=shown+" of "+D.matches.length+" shown";
  var mb=$("#manual tbody"), mh="";
  D.manual.forEach(function(e){mh+='<tr><td class="num" style="white-space:nowrap">'+esc(fmt(Date.parse(e.at),{day:"numeric",month:"short",year:"numeric",hour:"2-digit",minute:"2-digit",hour12:false}))+'</td><td class="num">'+e.mine+'</td><td class="num">'+e.squad+'</td><td>'+esc(e.note)+'</td></tr>'});
  mb.innerHTML=mh;
  $("#manualSec").style.display=D.manual.length?"":"none";
}

function summary(){
  var w=windows(), o=ordered(w.live), goal=(pick?o.cands.find(function(p){return p.key===pick}):null)||o.cands[0];
  var t="2xFlaw's Chest Counter \u2013 weekly chances ("+(D.profile||"")+")\n";
  t+=n0(D.chests)+" chests opened since the reset.\n";
  if(goal)t+="Goal: "+goal.name+" \u2013 "+pct(goal.chance)+" so far (1 in "+n0(goal.den)+" per chest).\n";
  o.list.slice(0,6).forEach(function(p){t+="\u2022 "+p.name+": "+pct(p.chance)+" (1 in "+n0(p.den)+")\n"});
  if(w.live)t+="Treasure Hour is live now!\n";else if(w.next)t+="Next Treasure Hour: "+whenText(w.next)+(w.next.unknown?" (boosts not announced)":"")+"\n";
  return t;
}

$("#btnTz").onclick=function(){tz=tz==="utc"?"local":"utc";save("cc.tz",tz);chrome();renderStats();renderMatches();render()};
$("#btnCopy").onclick=function(){
  var b=$("#btnCopy"), t=summary();
  function done(){b.textContent="Copied \u2713";setTimeout(function(){b.textContent="Copy summary"},1600)}
  if(navigator.clipboard&&navigator.clipboard.writeText){navigator.clipboard.writeText(t).then(done,function(){fallback()})}else fallback();
  function fallback(){var a=document.createElement("textarea");a.value=t;document.body.appendChild(a);a.select();try{document.execCommand("copy")}catch(e){}document.body.removeChild(a);done()}
};
$("#q").addEventListener("input",renderMatches);

chrome();renderStats();renderMatches();render();
setInterval(tick,1000);
})();
</script>
</body>
</html>

"""";
}
