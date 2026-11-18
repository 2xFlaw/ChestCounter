const params=new URLSearchParams(location.search);
let preview=['start','live','end'].includes(params.get('th-preview'))?params.get('th-preview'):null;
let previewAt=Date.now(),windows=[],previous=null,active=null;
const root=document.documentElement;
const banner=document.createElement('aside');banner.className='th-banner';banner.hidden=true;banner.setAttribute('aria-label','Treasure Hour status');
const title=document.createElement('strong'),clock=document.createElement('time'),boosts=document.createElement('span'),note=document.createElement('span');boosts.className='th-boosts';note.className='th-preview-note';
banner.append(title,clock,boosts,note);
const main=document.querySelector('main');if(main)main.before(banner);else document.body.prepend(banner);
const atmosphere=document.createElement('div');atmosphere.className='th-atmosphere';atmosphere.hidden=true;atmosphere.setAttribute('aria-hidden','true');
for(let i=0;i<12;i++){const p=document.createElement('i');p.style.setProperty('--x',`${(i*37)%100}%`);p.style.setProperty('--delay',`${-i*1.3}s`);atmosphere.append(p);}document.body.prepend(atmosphere);
const fields={'gold-outfit':['gold','Gold outfit'],'pirate-shop-access':['shop','Pirate shop'],'vehicle-decal':['vehicle','Vehicle & decal']};
const seconds=ms=>{const s=Math.max(0,Math.ceil(ms/1000)),days=Math.floor(s/86400);return `${days?days+'d ':''}${String(Math.floor(s%86400/3600)).padStart(2,'0')}:${String(Math.floor(s%3600/60)).padStart(2,'0')}:${String(s%60).padStart(2,'0')}`;};
if(preview){const controls=document.createElement('div');controls.className='th-controls';for(const state of ['start','live','end']){const b=document.createElement('button');b.type='button';b.textContent=`Preview ${state}`;b.onclick=()=>{preview=state;previewAt=Date.now();previous=null;tick();};controls.append(b);}const exit=document.createElement('button');exit.type='button';exit.textContent='Exit preview';exit.onclick=()=>{const url=new URL(location.href);url.searchParams.delete('th-preview');location.replace(url);};controls.append(exit);banner.append(controls);}
function highlight(){
  for(const row of document.querySelectorAll('#rows [data-k]')){const field=fields[row.dataset.k]?.[0];row.classList.toggle('th-boosted',!!(active&&field&&Number(active[field])>1));}
  const goal=document.querySelector('#goal'),selected=document.querySelector('#rows .sel[data-k]');if(goal){const field=fields[selected?.dataset.k]?.[0];goal.classList.toggle('th-boosted',!!(active&&field&&Number(active[field])>1));}
}
function tick(){
  const now=Date.now();let current=windows.find(w=>w.confirmed&&now>=Date.parse(w.start_utc)&&now<Date.parse(w.end_utc))||null;
  const next=windows.filter(w=>w.confirmed&&Date.parse(w.start_utc)>now).sort((a,b)=>Date.parse(a.start_utc)-Date.parse(b.start_utc))[0]||null;
  const ended=windows.filter(w=>w.confirmed&&now>=Date.parse(w.end_utc)&&now<Date.parse(w.end_utc)+15*60000).sort((a,b)=>Date.parse(b.end_utc)-Date.parse(a.end_utc))[0]||null;
  let remaining=current?Date.parse(current.end_utc)-now:next?Date.parse(next.start_utc)-now:0;
  let state=current?'live':ended?'ended':next?(remaining<=15*60000?'starting':'upcoming'):'idle';
  if(preview){const elapsed=now-previewAt;state=preview==='end'?'ended':preview==='start'&&elapsed<4000?'starting':elapsed<(preview==='start'?24000:60000)?'live':'ended';const sample=windows.find(w=>w.confirmed);current=state==='live'?sample||null:null;remaining=state==='starting'?4000-elapsed:(preview==='start'?24000:60000)-elapsed;}
  active=current;banner.hidden=!preview&&state==='idle';atmosphere.hidden=!current;
  root.classList.toggle('th-paused',document.hidden);
  const label=state==='live'?'TREASURE HOUR LIVE':state==='ended'?'TREASURE HOUR ENDED':state==='starting'?'TREASURE HOUR STARTING SOON':'NEXT TREASURE HOUR';
  title.textContent=(preview?'VISUAL PREVIEW - ':'')+label;
  clock.textContent=state==='live'?`Ends in ${seconds(remaining)}`:(state==='starting'||state==='upcoming')?`Starts in ${seconds(remaining)}`:state==='ended'?'Boosts have ended':'';
  const displayed=current||(!preview&&state!=='ended'?next:null);
  boosts.textContent=displayed?(!current?'Scheduled boosts: ':'')+Object.values(fields).filter(([field])=>Number(displayed[field])>1).map(([field,label])=>`${label} ${displayed[field]}×`).join(' · '):'';
  note.textContent=preview?'Visual test only. Stats, saved data, real event times and Luck calculations are unchanged.':current?'Only the listed prizes receive these confirmed boosts.':state==='ended'?'Normal odds apply now.'+(next?` Next Treasure Hour starts in ${seconds(Date.parse(next.start_utc)-now)}.`:''):next?`Starts ${new Date(next.start_utc).toLocaleString()}. Boosts are not active yet.`:'';
  const key=current?(preview?'preview':current.start_utc):null;
  if(key&&key!==previous&&previous!==null){banner.classList.remove('th-shimmer');void banner.offsetWidth;banner.classList.add('th-shimmer');setTimeout(()=>banner.classList.remove('th-shimmer'),1300);}
  previous=key||'inactive';highlight();
}
async function load(){try{const r=await fetch('/treasure-hours.json',{cache:'no-store'});if(!r.ok)throw Error(r.status);const data=await r.json();windows=(data.windows||[]).filter(w=>Number.isFinite(Date.parse(w.start_utc))&&Number.isFinite(Date.parse(w.end_utc))&&Date.parse(w.end_utc)>Date.parse(w.start_utc));tick();}catch(error){console.warn('Treasure Hour schedule unavailable',error);windows=[];tick();}}
const rows=document.querySelector('#rows');if(rows)new MutationObserver(highlight).observe(rows,{childList:true});
setInterval(()=>{if(!document.hidden)tick();},1000);setInterval(()=>{if(!document.hidden)load();},60000);
document.addEventListener('visibilitychange',()=>{root.classList.toggle('th-paused',document.hidden);if(!document.hidden)load();});
await load();
