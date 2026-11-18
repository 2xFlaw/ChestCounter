import worker from './worker.js';
import { handleExtra } from './extra.js';
import { seedTreasureHours } from './schedule.js';
import { handleRankings } from './rankings.js';
import { handlePersonal } from './personal.js';
const adPages = new Set(['/', '/index.html', '/stats', '/stats.html', '/luck', '/luck.html', '/community', '/community.html', '/leaderboard', '/leaderboard.html', '/friends', '/friends.html']);
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
  // Avoid duplicate loading or replacing a manually installed Google tag.
  if(html.includes('/adsense.js')||html.includes('pagead2.googlesyndication.com/pagead/js/adsbygoogle.js'))return new Response(html,response);
  const headers=new Headers(response.headers);
  headers.delete('content-length');headers.delete('etag');headers.delete('content-encoding');
  return new HTMLRewriter().on('head',{element(head){head.append('<script defer src="/adsense.js"></script>',{html:true});}}).transform(new Response(html,{status:response.status,statusText:response.statusText,headers}));
 }
};
