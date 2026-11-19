import { computeLuck, weekStart } from './odds.js';
import { withAllTimePirate } from './alltime.js';

const J = (d, s = 200, h = {}) => new Response(JSON.stringify(d), { status: s, headers: { 'content-type': 'application/json', 'cache-control': 'no-store', ...h } });
const hex = (b) => [...new Uint8Array(b)].map((x) => x.toString(16).padStart(2, '0')).join('');
const sha = async (s) => hex(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s)));
const rnd = (n) => hex(crypto.getRandomValues(new Uint8Array(n)));
const NEXT_ALLOWED = ['/stats.html', '/luck.html', '/'];

let tablesReady = null;
function ensureTables(env) {
  if (!tablesReady) {
    tablesReady = env.DB.batch([
      env.DB.prepare('CREATE TABLE IF NOT EXISTS web_tickets (hash TEXT PRIMARY KEY, profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE, expires_at TEXT NOT NULL)'),
      env.DB.prepare('CREATE TABLE IF NOT EXISTS web_sessions (id TEXT PRIMARY KEY, profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE, secret_hash TEXT NOT NULL, expires_at TEXT NOT NULL)'),
      env.DB.prepare('CREATE TABLE IF NOT EXISTS pair_requests (code TEXT PRIMARY KEY, secret_hash TEXT NOT NULL, profile_id TEXT REFERENCES profiles(id) ON DELETE CASCADE, expires_at TEXT NOT NULL)'),
    ]).catch((e) => { tablesReady = null; throw e; });
  }
  return tablesReady;
}

async function profileAuth(req, env) {
  const cookie = req.headers.get('cookie') || '';
  const s = cookie.match(/cc_session=([a-f0-9]+)\.([a-f0-9]+)/);
  if (s) {
    try {
      const row = await env.DB.prepare("SELECT profile_id FROM web_sessions WHERE id=? AND secret_hash=? AND expires_at > datetime('now')").bind(s[1], await sha(s[2])).first();
      if (row) return row.profile_id;
    } catch { /* table not created yet */ }
  }
  const m = cookie.match(/cc_profile=([A-F0-9-]+)\.([a-f0-9]+)/);
  if (!m) return null;
  const row = await env.DB.prepare('SELECT profile_id FROM profile_secrets WHERE profile_id=? AND secret_hash=?').bind(m[1], await sha(m[2])).first();
  return row ? m[1] : null;
}

async function pcAuth(req, env) {
  const m = (req.headers.get('authorization') || '').match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
  if (!m) return null;
  const row = await env.DB.prepare('SELECT profile_id FROM linked_pcs WHERE id=? AND token_hash=?').bind(m[1], await sha(m[2])).first();
  return row ? row.profile_id : null;
}

async function readBody(req) {
  try { return await req.json(); } catch { return {}; }
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

  // The app tells the server to forget this PC when the player unlinks it.
  if (route === 'POST /api/pc/unlink') {
    const m = (req.headers.get('authorization') || '').match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
    if (!m) return J({ error: 'Unauthorized' }, 401);
    const r = await env.DB.prepare('DELETE FROM linked_pcs WHERE id=? AND token_hash=?').bind(m[1], await sha(m[2])).run();
    return J({ unlinked: r.meta.changes > 0 });
  }

  // The player removes one of their own linked PCs from the website.
  const removePc = url.pathname.match(/^\/api\/linked-pcs\/([a-f0-9]+)$/);
  if (removePc && req.method === 'DELETE') {
    const me = await profileAuth(req, env);
    if (!me) return J({ error: 'No profile' }, 401);
    const r = await env.DB.prepare('DELETE FROM linked_pcs WHERE id=? AND profile_id=?').bind(removePc[1], me).run();
    return J({ removed: r.meta.changes > 0 });
  }

  // Linked app asks for a one-time ticket that signs the browser in as the same profile.
  if (route === 'POST /api/pc/web-session') {
    const pid = await pcAuth(req, env);
    if (!pid) return J({ error: 'Unauthorized' }, 401);
    await ensureTables(env);
    const b = await readBody(req);
    const next = NEXT_ALLOWED.includes(b.next) ? b.next : '/stats.html';
    const ticket = rnd(24);
    await env.DB.batch([
      env.DB.prepare("DELETE FROM web_tickets WHERE expires_at < datetime('now')"),
      env.DB.prepare("INSERT INTO web_tickets (hash, profile_id, expires_at) VALUES (?,?,datetime('now','+2 minutes'))").bind(await sha(ticket), pid),
    ]);
    return J({ url: `${url.origin}/api/session/redeem?t=${ticket}&next=${encodeURIComponent(next)}` });
  }

  // Browser redeems the ticket once and receives a session cookie.
  if (route === 'GET /api/session/redeem') {
    await ensureTables(env);
    const ticket = url.searchParams.get('t') || '';
    const next = NEXT_ALLOWED.includes(url.searchParams.get('next')) ? url.searchParams.get('next') : '/stats.html';
    const hash = await sha(ticket);
    const row = await env.DB.prepare("SELECT profile_id FROM web_tickets WHERE hash=? AND expires_at > datetime('now')").bind(hash).first();
    const used = row ? await env.DB.prepare('DELETE FROM web_tickets WHERE hash=?').bind(hash).run() : null;
    if (!row || !used || used.meta.changes !== 1) {
      return new Response('This sign-in link expired or was already used. Open it again from the Chest Counter app.', { status: 400, headers: { 'content-type': 'text/plain; charset=utf-8', 'cache-control': 'no-store' } });
    }
    const id = rnd(8), secret = rnd(24);
    await env.DB.prepare("INSERT INTO web_sessions (id, profile_id, secret_hash, expires_at) VALUES (?,?,?,datetime('now','+30 days'))").bind(id, row.profile_id, await sha(secret)).run();
    return new Response(null, {
      status: 302,
      headers: {
        location: next,
        'cache-control': 'no-store',
        'referrer-policy': 'no-referrer',
        'set-cookie': `cc_session=${id}.${secret}; Path=/; Max-Age=2592000; HttpOnly; Secure; SameSite=Lax`,
      },
    });
  }

  // Unlinked app starts pairing. The secret stays in the app; only the code goes into the browser URL.
  if (route === 'POST /api/pc/pair/start') {
    await ensureTables(env);
    const pending = await env.DB.prepare("SELECT COUNT(*) AS c FROM pair_requests WHERE expires_at > datetime('now')").first();
    if (pending && pending.c > 500) return J({ error: 'Too many link requests right now. Try again in a few minutes.' }, 429);
    const code = rnd(4).toUpperCase(), secret = rnd(24);
    await env.DB.batch([
      env.DB.prepare("DELETE FROM pair_requests WHERE expires_at < datetime('now')"),
      env.DB.prepare("INSERT INTO pair_requests (code, secret_hash, profile_id, expires_at) VALUES (?,?,NULL,datetime('now','+10 minutes'))").bind(code, await sha(secret)),
    ]);
    return J({ code, secret, expiresInMinutes: 10 });
  }

  // Signed-in website visitor approves the pairing request.
  if (route === 'POST /api/link/approve') {
    const me = await profileAuth(req, env);
    if (!me) return J({ error: 'No profile. Reload the page and try again.' }, 401);
    await ensureTables(env);
    const code = String((await readBody(req)).code || '').toUpperCase();
    const r = await env.DB.prepare("UPDATE pair_requests SET profile_id=? WHERE code=? AND profile_id IS NULL AND expires_at > datetime('now')").bind(me, code).run();
    if (!r.meta.changes) return J({ error: 'This link request expired. Start again in the app.' }, 404);
    return J({ approved: true });
  }

  // App polls with code + secret until the request is approved, then receives its device token once.
  if (route === 'POST /api/pc/pair/poll') {
    await ensureTables(env);
    const b = await readBody(req);
    const code = String(b.code || '').toUpperCase();
    const row = await env.DB.prepare("SELECT secret_hash, profile_id, expires_at > datetime('now') AS live FROM pair_requests WHERE code=?").bind(code).first();
    if (!row || row.secret_hash !== await sha(String(b.secret || ''))) return J({ status: 'expired' });
    if (!row.live) {
      await env.DB.prepare('DELETE FROM pair_requests WHERE code=?').bind(code).run();
      return J({ status: 'expired' });
    }
    if (!row.profile_id) return J({ status: 'pending' });
    const used = await env.DB.prepare('DELETE FROM pair_requests WHERE code=?').bind(code).run();
    if (used.meta.changes !== 1) return J({ status: 'expired' });
    const id = rnd(8), secret = rnd(24);
    await env.DB.prepare('INSERT INTO linked_pcs (id, profile_id, token_hash) VALUES (?,?,?)').bind(id, row.profile_id, await sha(secret)).run();
    return J({ status: 'linked', token: `${id}.${secret}` });
  }

  // Username set from the app (PC token). Same rules as the website: 3-20 chars, once every 7 days.
  if (route === 'PUT /api/pc/username') {
    const pid = await pcAuth(req, env);
    if (!pid) return J({ error: 'Unauthorized' }, 401);
    let b = {};
    try { b = await req.json(); } catch { /* ignore */ }
    const u = String(b.username || '').trim();
    if (!/^[A-Za-z0-9_. -]{3,20}$/.test(u)) return J({ error: '3-20 characters: letters, numbers, space, . _ -' }, 400);
    const p = await env.DB.prepare('SELECT username_changed_at FROM profiles WHERE id=?').bind(pid).first();
    if (p && p.username_changed_at && Date.now() - new Date(p.username_changed_at + 'Z').getTime() < 7 * 864e5) return J({ error: 'Username can be changed once every 7 days' }, 429);
    try { await env.DB.prepare("UPDATE profiles SET username=?, username_changed_at=datetime('now') WHERE id=?").bind(u, pid).run(); }
    catch { return J({ error: 'Username already taken' }, 409); }
    return J({ username: u });
  }

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
    const prizes = await withAllTimePirate(computeLuck(ev, windows, now), env, row.profile_id, windows, now, true);
    return J({ chests: ev.reduce((a, e) => a + e.count, 0), prizes, nextHour: next });
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
