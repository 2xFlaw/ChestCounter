import('/ui.js').then(({polish})=>polish()).catch(error=>console.error('UI enhancement failed',error));
import('/home.js').catch(error=>{document.documentElement.classList.remove('fx');console.error('Homepage motion failed',error);});

// Homepage reward art follows the Treasure Hunt: the latest week that has started, using the newest real picture
// for each slot (pictures not released yet, named soon-*, are skipped).
fetch('/rewards/weeks.json',{cache:'no-cache'}).then(r=>r.json()).then(d=>{
  const now=Date.now(),weeks=(d.weeks||[]).filter(w=>Array.isArray(w.items)&&Date.parse(w.start)<=now).sort((a,b)=>Date.parse(b.start)-Date.parse(a.start));
  document.querySelectorAll('img[data-slot]').forEach(img=>{
    const file=weeks.map(w=>w.items.find(i=>i.slot===img.dataset.slot)?.image).find(f=>f&&/^[a-z0-9-]+\.png$/.test(f)&&!f.startsWith('soon-'));
    if(!file||img.getAttribute('src')==='/rewards/'+file)return;
    const probe=new Image();probe.onload=()=>{img.src=probe.src;};probe.src='/rewards/'+file;
  });
}).catch(()=>{});
