let started=false;
export function polish(){if(started)return;started=true;
for(const href of ['/modern.css','/art.css']){const l=document.createElement('link');l.rel='stylesheet';l.href=href;document.head.append(l);}
import('/fx.js').catch(error=>console.error('Motion layer failed',error));
import('/i18n.js').then(m=>m.init()).catch(error=>console.error('Language picker failed',error));
const contact=document.querySelector('footer a[href^="mailto:"]');
let privacy=document.querySelector('a[href="/privacy.html"]');
if(!privacy){privacy=document.createElement('a');privacy.href='/privacy.html';privacy.textContent='Privacy policy';}
if(contact&&!contact.closest('.cc-footer-links')){const old=privacy.parentElement;const removeOld=old?.tagName==='P'&&old.textContent.trim()===privacy.textContent;const group=document.createElement('span');group.className='cc-footer-links';group.style.cssText='display:inline-flex;align-items:baseline;gap:.4em;white-space:nowrap';contact.replaceWith(group);group.append(contact,document.createTextNode(' · '),privacy);if(removeOld)old.remove();}
else if(!privacy.isConnected){let footer=document.querySelector('footer');if(!footer){footer=document.createElement('footer');footer.className='site-footer';document.body.append(footer);}footer.append(document.createTextNode(' · '),privacy);}
const strip=document.querySelector('.ticker');if(strip&&!strip.hasAttribute('data-marquee')){strip.removeAttribute('aria-hidden');strip.replaceChildren(...['COUNT EVERY CHEST','CHASE YOUR NEXT DROP','KNOW YOUR CHANCES'].map(text=>{const span=document.createElement('span');span.textContent=text;return span;}));}
const scan=()=>{document.querySelectorAll('.reward img,.feature-art img,.cc-prize-image,.cc-row-image').forEach(img=>{img.loading='eager';if(img.parentElement.classList.contains('art-card'))return;const card=document.createElement('span');card.className='art-card'+(img.classList.contains('cc-row-image')?' art-small':img.classList.contains('cc-prize-image')?' art-goal':'');img.before(card);card.append(img);});
const walker=document.createTreeWalker(document.documentElement,NodeFilter.SHOW_TEXT);let node;while(node=walker.nextNode()){if(!node.parentElement?.closest('script,style,textarea,input')&&node.nodeValue.includes('\u2014'))node.nodeValue=node.nodeValue.replace(/\s*\u2014\s*/g,' - ');}
document.querySelectorAll('a[href="/#dashboard"]').forEach(a=>{a.href='/stats.html';});};
scan();
const goal=document.querySelector('#goal'),rows=document.querySelector('#rows');
if(goal||rows){const observer=new MutationObserver(scan);if(goal)observer.observe(goal,{childList:true,subtree:true});if(rows)observer.observe(rows,{childList:true,subtree:true});}
}
