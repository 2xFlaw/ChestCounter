import { computeLuck, weekStart } from './odds.js';

const J = (d, s = 200) => new Response(JSON.stringify(d), { status: s, headers: { 'content-type': 'application/json', 'cache-control': 'no-store' } });
const hex = (b) => [...new Uint8Array(b)].map((x) => x.toString(16).padStart(2, '0')).join('');
const sha = async (s) => hex(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s)));
const rnd = (n) => hex(crypto.getRandomValues(new Uint8Array(n)));

async function profileAuth(req, env) {
  const m = (req.headers.get('cookie') || '').match(/cc_profile=([A-F0-9-]+)\.([a-f0-9]+)/);
  if (!m) return null;
  const row = await env.DB.prepare('SELECT profile_id FROM profile_secrets WHERE profile_id=? AND secret_hash=?').bind(m[1], await sha(m[2])).first();
  return row ? m[1] : null;
}

async function weekEvents(env, profileId, start, end) {
  const rows = (await env.DB.prepare('SELECT started_at, opened, chest_times_json FROM matches WHERE profile_id=? AND started_at < ? AND started_at >= ?').bind(profileId, end.toISOString(), new Date(start - 3600e3).toISOString()).all()).results;
  const ev = [];
  for (const r of rows) {
    const t0 = new Date(r.started_at);
    let times = null;
    try { times = r.chest_times_json ? JSON.parse(r.chest_times_json) : null; } catch { /* ignore */ }
    if (times && times.length) {
      for (const s of times) { const t = new Date(t0.getTime() + s * 1000); if (t >= start && t < end) ev.push({ time: t, count: 1 }); }
    } else if (t0 >= start) ev.push({ time: t0, count: r.opened });
  }
  return ev;
}

// Routes added on top of worker.js. Returns null when the route is not handled here.
export async function handleExtra(req, env, url) {
  const route = req.method + ' ' + url.pathname;

  // Private link for the OBS browser source. The secret is shown once; only its hash is stored.
  if (route === 'POST /api/obs/link') {
    const me = await profileAuth(req, env);
    if (!me) return J({ error: 'No profile' }, 401);
    const secret = rnd(24);
    await env.DB.prepare('INSERT INTO obs_links (secret_hash, profile_id) VALUES (?,?)').bind(await sha(secret), me).run();
    return J({ secret, path: '/obs/luck.html?k=' + secret });
  }

  if (route === 'DELETE /api/obs/link') {
    const me = await profileAuth(req, env);
    if (!me) return J({ error: 'No profile' }, 401);
    await env.DB.prepare('DELETE FROM obs_links WHERE profile_id=?').bind(me).run();
    return J({ revoked: true });
  }

  if (route === 'GET /api/obs/luck') {
    const row = await env.DB.prepare('SELECT profile_id FROM obs_links WHERE secret_hash=?').bind(await sha(url.searchParams.get('k') || '')).first();
    if (!row) return J({ error: 'Invalid link' }, 404);
    const now = new Date(), start = weekStart(now), end = new Date(start.getTime() + 7 * 864e5);
    const windows = (await env.DB.prepare('SELECT * FROM treasure_hours ORDER BY start_utc').all()).results;
    const ev = await weekEvents(env, row.profile_id, start, end);
    const next = windows.find((w) => new Date(w.start_utc) > now) || null;
    return J({ chests: ev.reduce((a, e) => a + e.count, 0), prizes: computeLuck(ev, windows, now), nextHour: next });
  }

  // Loads Treasure Hour windows. Protected by a secret: wrangler secret put ADMIN_KEY
  if (route === 'POST /api/admin/treasure-hours') {
    if (!env.ADMIN_KEY || req.headers.get('x-admin-key') !== env.ADMIN_KEY) return J({ error: 'Unauthorized' }, 401);
    let b = {};
    try { b = await req.json(); } catch { /* ignore */ }
    const stmts = [];
    for (const w of b.windows || []) {
      const s = new Date(w.start), e = new Date(w.end);
      if (isNaN(s) || isNaN(e) || e <= s) continue;
      stmts.push(env.DB.prepare('INSERT OR REPLACE INTO treasure_hours (start_utc, end_utc, gold, shop, vehicle, confirmed) VALUES (?,?,?,?,?,?)')
        .bind(s.toISOString(), e.toISOString(), Number(w.gold) || 1, Number(w.shop) || 1, Number(w.vehicle) || 1, w.confirmed === false ? 0 : 1));
    }
    if (stmts.length) await env.DB.batch(stmts);
    return J({ loaded: stmts.length });
  }

  return null;
}
