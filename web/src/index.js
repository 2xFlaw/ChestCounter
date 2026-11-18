import worker from './worker.js';
import { handleExtra } from './extra.js';
import { seedTreasureHours } from './schedule.js';
import { handleRankings } from './rankings.js';
import { handlePersonal } from './personal.js';
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
  return worker.fetch(req,env);
 }
};
