import { browser } from './personal.js';
// Treasure Hunt "found" ticks, shared by the app (linked PC token) and the website (browser profile).
// Each key keeps its value and the time it was last changed, so the newest change wins wherever it was made.
const J=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json','cache-control':'no-store'}});
const hash=async s=>Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(s))),v=>v.toString(16).padStart(2,'0')).join('');
const table="CREATE TABLE IF NOT EXISTS owned_rewards (profile_id TEXT PRIMARY KEY REFERENCES profiles(id) ON DELETE CASCADE, payload TEXT NOT NULL, updated_at TEXT NOT NULL DEFAULT (datetime('now')))";
const KEY=/^[a-z0-9-]{1,40}@(all-time|\d{8})$/;
const MAX_KEYS=1000,MAX_SET=200;
const plain=map=>Object.fromEntries(Object.entries(map).map(([k,v])=>[k,v.v]));
export async function handleOwned(req,env,url){
 if(url.pathname!=='/api/owned')return null;
 await env.DB.prepare(table).run();
 let me=null,pc=false;
 const a=(req.headers.get('authorization')||'').match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
 if(a){const r=await env.DB.prepare('SELECT profile_id FROM linked_pcs WHERE id=? AND token_hash=?').bind(a[1],await hash(a[2])).first();if(!r)return J({error:'Unauthorized'},401);me=r.profile_id;pc=true;}
 else me=await browser(req,env);
 if(!me)return J({error:'Unauthorized'},401);
 const row=await env.DB.prepare('SELECT payload FROM owned_rewards WHERE profile_id=?').bind(me).first();
 let map={};try{map=row?JSON.parse(row.payload):{};}catch{map={};}
 if(req.method==='GET')return J({owned:plain(map),times:Object.fromEntries(Object.entries(map).map(([k,v])=>[k,v.t]))});
 if(req.method!=='POST')return J({error:'Not found'},404);
 if(!pc&&req.headers.get('origin')!==url.origin)return J({error:'Invalid origin'},403);
 const raw=await req.text();if(raw.length>40000)return J({error:'Too large'},413);
 let b;try{b=JSON.parse(raw);}catch{return J({error:'Invalid JSON'},400);}
 const set=b&&typeof b.set==='object'&&!Array.isArray(b.set)?b.set:null;
 if(!set)return J({error:'Invalid request'},400);
 const entries=Object.entries(set);if(entries.length>MAX_SET)return J({error:'Too many changes'},400);
 const now=Date.now();
 for(const [k,v] of entries){
  if(!KEY.test(k)||!v||typeof v.v!=='boolean'||!Number.isFinite(v.t))return J({error:'Invalid change'},400);
  const t=Math.min(Math.floor(v.t),now+60000);
  if(!map[k]||t>=map[k].t)map[k]={v:v.v,t};
 }
 if(Object.keys(map).length>MAX_KEYS)return J({error:'Too many rewards'},400);
 await env.DB.prepare("INSERT INTO owned_rewards(profile_id,payload) VALUES(?,?) ON CONFLICT(profile_id) DO UPDATE SET payload=excluded.payload,updated_at=datetime('now')").bind(me,JSON.stringify(map)).run();
 return J({owned:plain(map),times:Object.fromEntries(Object.entries(map).map(([k,v])=>[k,v.t]))});
}
// Treasure Hunt ticks of a profile ({ key: true } for owned items), for the odds. Empty when there are none yet.
export async function ownedFor(env,pid){
 try{
  const row=await env.DB.prepare('SELECT payload FROM owned_rewards WHERE profile_id=?').bind(pid).first();
  const map=row?JSON.parse(row.payload):{};
  return Object.fromEntries(Object.entries(map).filter(([,v])=>v&&v.v===true).map(([k])=>[k,true]));
 }catch{return {};}
}
