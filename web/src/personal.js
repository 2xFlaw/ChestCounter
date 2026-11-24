const J=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json','cache-control':'no-store'}});
const hash=async s=>Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(s))),v=>v.toString(16).padStart(2,'0')).join('');
const table="CREATE TABLE IF NOT EXISTS personal_snapshots (profile_id TEXT PRIMARY KEY REFERENCES profiles(id) ON DELETE CASCADE, source TEXT NOT NULL, scope TEXT NOT NULL, payload TEXT NOT NULL, revision TEXT NOT NULL, updated_at TEXT NOT NULL DEFAULT (datetime('now')))";
export async function browser(req,env){
 const c=req.headers.get('cookie')||'',s=c.match(/(?:^|;\s*)cc_session=([a-f0-9]+)\.([a-f0-9]+)/),p=c.match(/(?:^|;\s*)cc_profile=([A-F0-9-]+)\.([a-f0-9]+)/);
 if(s){try{const r=await env.DB.prepare("SELECT profile_id FROM web_sessions WHERE id=? AND secret_hash=? AND expires_at>datetime('now')").bind(s[1],await hash(s[2])).first();if(r)return r.profile_id;}catch{}}
 if(p){const r=await env.DB.prepare('SELECT profile_id FROM profile_secrets WHERE profile_id=? AND secret_hash=?').bind(p[1],await hash(p[2])).first();if(r)return r.profile_id;}return null;
}
const count=n=>Number.isSafeInteger(n)&&n>=0&&n<=10000000;
const date=s=>typeof s==='string'&&s.length<=40&&Number.isFinite(Date.parse(s));
const id=s=>typeof s==='string'&&/^[a-f0-9-]{1,80}$/i.test(s);
export async function handlePersonal(req,env,url){
 if(!url.pathname.startsWith('/api/personal/'))return null;
 await env.DB.prepare(table).run();
 if(req.method==='POST'&&url.pathname==='/api/personal/snapshot'){
  const a=(req.headers.get('authorization')||'').match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
  if(!a)return J({error:'Unauthorized'},401);
  const pc=await env.DB.prepare('SELECT profile_id FROM linked_pcs WHERE id=? AND token_hash=?').bind(a[1],await hash(a[2])).first();
  if(!pc)return J({error:'Unauthorized'},401);
  const raw=await req.text();if(raw.length>4000000)return J({error:'Snapshot too large; previous personal stats preserved.'},413);
  let b;try{b=JSON.parse(raw);}catch{return J({error:'Invalid JSON'},400);}
  if(!id(b.source)||!id(b.scope)||!Array.isArray(b.matches)||!Array.isArray(b.entries)||b.matches.length>10000||b.entries.length>10000)return J({error:'Invalid snapshot'},400);
  const seen=new Set(),ids=new Set(),matches=[],entries=[];
  for(const m of b.matches){if(!id(m.id)||seen.has(m.id)||!['replay','imported'].includes(m.origin)||!(m.startedAt===null||date(m.startedAt))||!count(m.opened)||!count(m.destroyed)||!count(m.team)||typeof m.mode!=='string'||m.mode.length>100)return J({error:'Invalid or duplicate match'},400);seen.add(m.id);matches.push({id:m.id,origin:m.origin,startedAt:m.startedAt,mode:m.mode,opened:m.opened,destroyed:m.destroyed,team:m.team});}
  for(const e of b.entries){if(!id(e.id)||ids.has(e.id)||!date(e.at)||!count(e.amount))return J({error:'Invalid or duplicate manual entry'},400);ids.add(e.id);entries.push({id:e.id,at:e.at,amount:e.amount});}
  const payload=JSON.stringify({matches,entries}),revision=await hash(payload);
  const r=await env.DB.prepare("INSERT INTO personal_snapshots(profile_id,source,scope,payload,revision) VALUES(?,?,?,?,?) ON CONFLICT(profile_id) DO UPDATE SET payload=excluded.payload,revision=excluded.revision,updated_at=datetime('now') WHERE personal_snapshots.source=excluded.source AND personal_snapshots.scope=excluded.scope").bind(pc.profile_id,b.source,b.scope,payload,revision).run();
  if(!r.meta.changes)return J({error:'Another PC or app profile is the personal-history source. Clear the personal snapshot in My stats before changing source. Competitive data is unaffected.'},409);
  return J({saved:true,revision});
 }
 const me=await browser(req,env);if(!me)return J({error:'Unauthorized'},401);
 if(req.method==='DELETE'&&url.pathname==='/api/personal/snapshot'){
  if(req.headers.get('origin')!==url.origin)return J({error:'Invalid origin'},403);
  await env.DB.prepare('DELETE FROM personal_snapshots WHERE profile_id=?').bind(me).run();return J({removed:true});
 }
 if(req.method!=='GET')return J({error:'Not found'},404);
 const row=await env.DB.prepare('SELECT * FROM personal_snapshots WHERE profile_id=?').bind(me).first();
 if(url.pathname==='/api/personal/summary'){
  if(!row)return J({available:false});
  const data=JSON.parse(row.payload);let replay=0,imported=0,team=0,manual=0;const daily={};
  const day=(at,n)=>{if(at){const d=new Date(at).toISOString().slice(0,10);daily[d]=(daily[d]||0)+n;}};
  for(const m of data.matches){const n=m.opened+m.destroyed;if(m.origin==='replay')replay+=n;else imported+=n;team+=m.team;day(m.startedAt,n+m.team);}
  for(const e of data.entries){manual+=e.amount;day(e.at,e.amount);}
  return J({available:true,revision:row.revision,updatedAt:row.updated_at,replay,imported,team,manual,total:replay+imported+team+manual,matches:data.matches.length,daily});
 }
 if(url.pathname==='/api/personal/matches'){
  const offset=Number(url.searchParams.get('offset')||0);if(!Number.isSafeInteger(offset)||offset<0)return J({error:'Invalid page'},400);
  const q=(url.searchParams.get('q')||'').slice(0,100);const needle='%'+q.replace(/[\\%_]/g,'\\$&')+'%';
  const rev=url.searchParams.get('revision');if(row&&rev&&rev!==row.revision)return J({error:'History changed. Refresh history to load consistent pages.'},409);
  if(row){
   const filter="(COALESCE(json_extract(value,'$.mode'),'') LIKE ? ESCAPE '\\' OR COALESCE(json_extract(value,'$.startedAt'),'') LIKE ? ESCAPE '\\')";
   const total=await env.DB.prepare(`SELECT COUNT(*) AS n FROM json_each(?, '$.matches') WHERE ${filter}`).bind(row.payload,needle,needle).first();
   const records=(await env.DB.prepare(`SELECT value FROM json_each(?, '$.matches') WHERE ${filter} ORDER BY json_extract(value,'$.startedAt') DESC,json_extract(value,'$.id') DESC LIMIT 100 OFFSET ?`).bind(row.payload,needle,needle,offset).all()).results;
   return J({matches:records.map(r=>JSON.parse(r.value)),total:total.n,next:offset+records.length<total.n?offset+records.length:null,revision:row.revision,personal:true});
  }
  const filter="profile_id=? AND (COALESCE(mode,'') LIKE ? ESCAPE '\\' OR started_at LIKE ? ESCAPE '\\')";
  const total=await env.DB.prepare(`SELECT COUNT(*) AS n FROM matches WHERE ${filter}`).bind(me,needle,needle).first();
  const records=(await env.DB.prepare(`SELECT session_id AS id,started_at AS startedAt,mode,opened,destroyed FROM matches WHERE ${filter} ORDER BY started_at DESC,session_id DESC LIMIT 100 OFFSET ?`).bind(me,needle,needle,offset).all()).results;
  return J({matches:records.map(m=>({...m,origin:'replay',team:0})),total:total.n,next:offset+records.length<total.n?offset+records.length:null,personal:false});
 }
 return J({error:'Not found'},404);
}
