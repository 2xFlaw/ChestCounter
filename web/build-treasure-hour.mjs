import {readFileSync,writeFileSync,readdirSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname,join} from 'node:path';
const root=dirname(fileURLToPath(import.meta.url)),pub=join(root,'public');
const source=readFileSync(join(root,'src/schedule.js'),'utf8');
// Boost sets are named objects in schedule.js (BOOSTS is the default; a window can name another with boosts: NAME).
const sets=Object.fromEntries([...source.matchAll(/const (\w+) = \{ gold: ([\d.]+), shop: ([\d.]+), vehicle: ([\d.]+) \};/g)].map(m=>[m[1],{gold:Number(m[2]),shop:Number(m[3]),vehicle:Number(m[4])}]));
if(!sets.BOOSTS)throw Error('Missing schedule boosts');
const windows=[...source.matchAll(/start: '([^']+)', end: '([^']+)'(?:, boosts: (\w+))?/g)].map(m=>{const b=sets[m[3]||'BOOSTS'];if(!b)throw Error('Unknown boost set '+m[3]);return {start_utc:m[1],end_utc:m[2],confirmed:true,...b};});
if(!windows.length)throw Error('Treasure Hour schedule not found');
writeFileSync(join(pub,'treasure-hours.json'),JSON.stringify({windows}));
for(const entry of readdirSync(pub)){if(!entry.endsWith('.html'))continue;const p=join(pub,entry);let h=readFileSync(p,'utf8');if(!h.includes('src="/treasure-hour.js"'))h=h.replace('</head>','<link rel="stylesheet" href="/treasure-hour.css"><script type="module" src="/treasure-hour.js"></script></head>');writeFileSync(p,h);}
const path=join(pub,'luck.html');let html=readFileSync(path,'utf8');
if(!html.includes('function syncTreasureClock()')){
  for(const name of ['now','live','next']){const from='const '+name+' =';if(!html.includes(from))throw Error('Luck clock integration changed: '+name);html=html.replace(from,'let '+name+' =');}
  const chips=/\$\('#chips'\)\.innerHTML = [^\n]+;/;
  if(!chips.test(html))throw Error('Luck chip integration changed');
  html=html.replace(chips,m=>'function paintTreasureChips(){'+m+'}\npaintTreasureChips();');
  const end="render();\n</script>";
  if(!html.includes(end))throw Error('Luck render integration changed');
  html=html.replace(end,`render();
function syncTreasureClock(){
  now=Date.now();
  const current=W.find(w=>now>=w.s&&now<w.e)||null;
  const upcoming=W.find(w=>w.s>now)||null;
  if(current!==live||upcoming!==next){live=current;next=upcoming;paintTreasureChips();render();}
}
setInterval(()=>{if(!document.hidden)syncTreasureClock();},1000);
document.addEventListener('visibilitychange',()=>{if(!document.hidden)syncTreasureClock();});
</script>`);
  writeFileSync(path,html);
}
