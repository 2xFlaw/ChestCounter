import worker from './worker.js';
import { handleExtra } from './extra.js';
import { seedTreasureHours } from './schedule.js';
import { handleRankings } from './rankings.js';
import { handlePersonal } from './personal.js';
import { handleBenchmarks } from './benchmarks.js';
import { handleOwned } from './owned.js';
const adPages = new Set(['/', '/index.html', '/stats', '/stats.html', '/luck', '/luck.html', '/community', '/community.html', '/leaderboard', '/leaderboard.html', '/friends', '/friends.html', '/claim', '/claim.html']);
const recoveryTag = `<script async src="https://fundingchoicesmessages.google.com/i/pub-3617574685495506?ers=1"></script><script>(function() {function signalGooglefcPresent() {if (!window.frames['googlefcPresent']) {if (document.body) {const iframe = document.createElement('iframe'); iframe.style = 'width: 0; height: 0; border: none; z-index: -1000; left: -1000px; top: -1000px;'; iframe.style.display = 'none'; iframe.name = 'googlefcPresent'; document.body.appendChild(iframe);} else {setTimeout(signalGooglefcPresent, 0);}}}signalGooglefcPresent();})();</script>`;
const LANGS = ['en', 'es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
const COUNTRY = {};
for (const [lang, list] of Object.entries({
 es: 'ES MX AR CO CL PE VE EC GT CU BO DO HN PY SV NI CR PA UY PR',
 pt: 'BR PT AO MZ',
 ru: 'RU BY',
 fr: 'FR MC BE',
 de: 'DE AT LI CH',
 it: 'IT SM VA',
 pl: 'PL',
 tr: 'TR',
 ja: 'JP'
})) for (const code of list.split(' ')) COUNTRY[code] = lang;
const PREFIX = new RegExp('^/([a-z]{2})(-[A-Za-z]{2,4})?(?=/|$)', 'i');
const BOTS = new RegExp('bot|crawl|spider|slurp|facebookexternalhit|embedly|preview|lighthouse|mediapartners', 'i');
const hasChoice = req => (req.headers.get('cookie') || '').split(';').some(part => part.trim().startsWith('cc_lang='));
function guessLanguage(req) {
 const byCountry = COUNTRY[(req.cf && req.cf.country) || ''];
 if (byCountry) return byCountry;
 const header = (req.headers.get('accept-language') || '').slice(0, 2).toLowerCase();
 return LANGS.includes(header) ? header : 'en';
}
function alternates(origin, path) {
 const page = path === '/index.html' ? '/' : path;
 const link = (hreflang, href) => `<link rel="alternate" hreflang="${hreflang}" href="${origin}${href}">`;
 return LANGS.map(l => link(l, l === 'en' ? page : '/' + l + page)).join('') + link('x-default', page);
}
export default {
 async fetch(req,env){
  const url=new URL(req.url);
  let request=req,path=url.pathname,lang=null,tag=null;
  const prefix=url.pathname.match(PREFIX);
  if(prefix&&LANGS.includes(prefix[1].toLowerCase())){
   lang=prefix[1].toLowerCase();tag=prefix[1]+(prefix[2]||'');
   const inner=new URL(req.url);
   inner.pathname=url.pathname.slice(prefix[0].length)||'/';
   path=inner.pathname;
   request=new Request(inner,req);
  }else if(req.method==='GET'&&adPages.has(url.pathname)&&!hasChoice(req)&&!BOTS.test(req.headers.get('user-agent')||'')){
   const guess=guessLanguage(req);
   if(guess!=='en')return new Response(null,{status:302,headers:{location:'/'+guess+(url.pathname==='/index.html'?'/':url.pathname)+url.search,'cache-control':'no-store',vary:'Cookie, Accept-Language'}});
  }
  if(url.pathname.startsWith('/api/')){
   try{await seedTreasureHours(env);}catch{}
   try{
    const benchmark=await handleBenchmarks(req,env,url);if(benchmark)return benchmark;
    const owned=await handleOwned(req,env,url);if(owned)return owned;
    const personal=await handlePersonal(req,env,url);if(personal)return personal;
    const ranking=await handleRankings(req,env,url);if(ranking)return ranking;
    const extra=await handleExtra(req,env,url);if(extra)return extra;
   }catch{return new Response(JSON.stringify({error:'Server error'}),{status:500,headers:{'content-type':'application/json','cache-control':'no-store'}});}
  }
  const response=await worker.fetch(request,env);
  if(lang&&response.status>=300&&response.status<400){
   const target=response.headers.get('location');
   if(target){
    const to=new URL(target,req.url);
    if(to.origin===url.origin){const headers=new Headers(response.headers);headers.set('location','/'+lang+to.pathname+to.search+to.hash);return new Response(null,{status:response.status,headers});}
   }
   return response;
  }
  if(req.method!=='GET'||response.status!==200||!(response.headers.get('content-type')||'').includes('text/html'))return response;
  const ads=adPages.has(path);
  if(!ads&&!lang)return response;
  const html=await response.text();
  let tags='';
  if(ads){
   if(!html.includes('fundingchoicesmessages.google.com/i/'))tags+=recoveryTag;
   if(!html.includes('/adsense.js')&&!html.includes('pagead2.googlesyndication.com/pagead/js/adsbygoogle.js'))tags+='<script defer src="/adsense.js"></script>';
   tags+=alternates(url.origin,path);
  }
  const headers=new Headers(response.headers);
  headers.delete('content-length');headers.delete('etag');headers.delete('content-encoding');
  const output=new Response(html,{status:response.status,statusText:response.statusText,headers});
  if(!lang&&!tags)return output;
  let rewriter=new HTMLRewriter();
  if(lang)rewriter=rewriter.on('html',{element(el){el.setAttribute('lang',tag);}});
  if(tags)rewriter=rewriter.on('head',{element(head){head.append(tags,{html:true});}});
  return rewriter.transform(output);
 }
};
