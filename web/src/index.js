import worker from './worker.js';
import { handleExtra } from './extra.js';
import { seedTreasureHours } from './schedule.js';
import { handleRankings } from './rankings.js';
import { handlePersonal } from './personal.js';
const adPages = new Set(['/', '/index.html', '/stats', '/stats.html', '/luck', '/luck.html', '/community', '/community.html', '/leaderboard', '/leaderboard.html', '/friends', '/friends.html']);
const recoveryTag = `<script async src="https://fundingchoicesmessages.google.com/i/pub-3617574685495506?ers=1"></script><script>(function() {function signalGooglefcPresent() {if (!window.frames['googlefcPresent']) {if (document.body) {const iframe = document.createElement('iframe'); iframe.style = 'width: 0; height: 0; border: none; z-index: -1000; left: -1000px; top: -1000px;'; iframe.style.display = 'none'; iframe.name = 'googlefcPresent'; document.body.appendChild(iframe);} else {setTimeout(signalGooglefcPresent, 0);}}}signalGooglefcPresent();})();</script>`;
export default {
 async fetch(req,env){
  const url=new URL(req.url);
  if(url.pathname.startsWith('/api/')){
   try{await seedTreasureHours(env);}catch{}
   try{
    const personal=await handlePersonal(req,env,url);if(personal)return personal;
    const ranking=await handleRankings(req,env,url);if(ranking)return ranking;
    const extra=await handleExtra(req,env,url);if(extra)return extra;
   }catch{return new Response(JSON.stringify({error:'Server error'}),{status:500,headers:{'content-type':'application/json','cache-control':'no-store'}});}
  }
  const response=await worker.fetch(req,env);
  if(req.method!=='GET'||!adPages.has(url.pathname)||response.status!==200||!(response.headers.get('content-type')||'').includes('text/html'))return response;
  const html=await response.text();
  let tags='';
  if(!html.includes('fundingchoicesmessages.google.com/i/'))tags+=recoveryTag;
  if(!html.includes('/adsense.js')&&!html.includes('pagead2.googlesyndication.com/pagead/js/adsbygoogle.js'))tags+='<script defer src="/adsense.js"></script>';
  const headers=new Headers(response.headers);
  headers.delete('content-length');headers.delete('etag');headers.delete('content-encoding');
  const output=new Response(html,{status:response.status,statusText:response.statusText,headers});
  if(!tags)return output;
  return new HTMLRewriter().on('head',{element(head){head.append(tags,{html:true});}}).transform(output);
 }
};
