const json = data => new Response(JSON.stringify(data), {headers:{'content-type':'application/json','cache-control':'no-store'}});
const eligible = "p.username IS NOT NULL AND p.is_private=0 AND EXISTS (SELECT 1 FROM linked_pcs l WHERE l.profile_id=p.id)";
// Competitive totals deliberately read matches only. Never add starting_total or manual ledger data here.
export async function handleRankings(req, env, url) {
  if (req.method !== 'GET') return null;
  if (url.pathname === '/api/leaderboard') {
    const sort = {global:'total',opened:'opened',broken:'destroyed',matches:'matches'}[url.searchParams.get('sort')] || 'total';
    const rows = (await env.DB.prepare(`SELECT p.id,p.username,COALESCE(SUM(m.opened),0) AS opened,COALESCE(SUM(m.destroyed),0) AS destroyed,COALESCE(SUM(m.opened+m.destroyed),0) AS total,COUNT(m.session_id) AS matches FROM profiles p LEFT JOIN matches m ON m.profile_id=p.id WHERE ${eligible} GROUP BY p.id ORDER BY ${sort} DESC,p.id ASC LIMIT 100`).all()).results;
    const record = await env.DB.prepare(`SELECT p.username,m.opened+m.destroyed AS total,m.mode,m.started_at FROM matches m JOIN profiles p ON p.id=m.profile_id WHERE ${eligible} ORDER BY total DESC,m.started_at ASC,p.id ASC,m.session_id ASC LIMIT 1`).first();
    return json({players:rows.map((p,i)=>({rank:i+1,username:p.username,global:p.total,opened:p.opened,destroyed:p.destroyed,matches:p.matches})),record,rankingSource:'synced_matches_only'});
  }
  return null;
}
