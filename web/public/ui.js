let started=false;
export function polish(){if(started)return;started=true;
for(const href of ['/modern.css','/art.css']){const l=document.createElement('link');l.rel='stylesheet';l.href=href;document.head.append(l);}
const strip=document.querySelector('.ticker');if(strip){strip.removeAttribute('aria-hidden');strip.replaceChildren(...['COUNT EVERY CHEST','CHASE YOUR NEXT DROP','KNOW YOUR CHANCES'].map(text=>{const span=document.createElement('span');span.textContent=text;return span;}));}
const scan=()=>{document.querySelectorAll('.reward img,.feature-art img,.cc-prize-image,.cc-row-image').forEach(img=>{img.loading='eager';if(img.parentElement.classList.contains('art-card'))return;const card=document.createElement('span');card.className='art-card'+(img.classList.contains('cc-row-image')?' art-small':img.classList.contains('cc-prize-image')?' art-goal':'');img.before(card);card.append(img);});
const walker=document.createTreeWalker(document.documentElement,NodeFilter.SHOW_TEXT);let node;while(node=walker.nextNode()){if(!node.parentElement?.closest('script,style,textarea,input')&&node.nodeValue.includes('\u2014'))node.nodeValue=node.nodeValue.replace(/\s*\u2014\s*/g,' - ');}
document.querySelectorAll('a[href="/#dashboard"]').forEach(a=>{a.href='/stats.html';});};
scan();
const goal=document.querySelector('#goal'),rows=document.querySelector('#rows');
if(goal||rows){const observer=new MutationObserver(scan);if(goal)observer.observe(goal,{childList:true,subtree:true});if(rows)observer.observe(rows,{childList:true,subtree:true});}
}
