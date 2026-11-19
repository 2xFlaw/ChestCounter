// Anonymous percentile rankings for the app's dashboard tiles.
// The app sends its own summary figures (no names, Epic IDs, match IDs or replay files) under a random install ID.
// Linking a PC is not needed. A linked PC may add its token so its synced matches are not counted twice.
const J = (data, status = 200) => new Response(JSON.stringify(data), { status, headers: { 'content-type': 'application/json', 'cache-control': 'no-store' } });
const sha = async s => Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s))), v => v.toString(16).padStart(2, '0')).join('');
const TABLE = "CREATE TABLE IF NOT EXISTS benchmarks (key TEXT PRIMARY KEY, matches INTEGER NOT NULL, total INTEGER NOT NULL, mates INTEGER NOT NULL, avg REAL NOT NULL, avg_all REAL NOT NULL, best INTEGER NOT NULL, best_all INTEGER NOT NULL, pm REAL, pm_all REAL, updated_at TEXT NOT NULL DEFAULT (datetime('now')))";
// The order matters: it is the order of the columns in the population query below.
const METRICS = ['total', 'mates', 'avg', 'avgAll', 'best', 'bestAll', 'pm', 'pmAll'];
const COLUMNS = { total: 'total', mates: 'mates', avg: 'avg', avgAll: 'avg_all', best: 'best', bestAll: 'best_all', pm: 'pm', pmAll: 'pm_all' };
const MIN_MATCHES = 3;
const num = (v, max) => typeof v === 'number' && Number.isFinite(v) && v >= 0 && v <= max;

function validate(b) {
  if (!b || typeof b.id !== 'string' || !/^[a-f0-9]{32}$/.test(b.id)) return null;
  const m = b.matches;
  if (!Number.isSafeInteger(m) || m < 0 || m > 100000) return null;
  const v = {
    total: b.total, mates: b.mates, avg: b.avg, avgAll: b.avgAll,
    best: b.best, bestAll: b.bestAll, pm: b.pm ?? null, pmAll: b.pmAll ?? null,
  };
  if (!Number.isSafeInteger(v.total) || !Number.isSafeInteger(v.mates) || !Number.isSafeInteger(v.best) || !Number.isSafeInteger(v.bestAll)) return null;
  // Plausibility limits: a single Battle Royale match never gets near these.
  if (!num(v.total, m * 400) || !num(v.mates, m * 1200) || !num(v.best, 400) || !num(v.bestAll, 1600)) return null;
  if (!num(v.avg, 400) || !num(v.avgAll, 1600) || v.avg > v.best + 0.001 || v.avgAll > v.bestAll + 0.001 || v.bestAll < v.best) return null;
  if (v.pm !== null && !num(v.pm, 30)) return null;
  if (v.pmAll !== null && !num(v.pmAll, 120)) return null;
  return { matches: m, ...v };
}

async function linkedProfile(req, env) {
  const a = (req.headers.get('authorization') || '').match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
  if (!a) return null;
  const pc = await env.DB.prepare('SELECT profile_id FROM linked_pcs WHERE id=? AND token_hash=?').bind(a[1], await sha(a[2])).first();
  return pc ? pc.profile_id : null;
}

export async function handleBenchmarks(req, env, url) {
  if (url.pathname !== '/api/benchmarks') return null;
  if (req.method !== 'POST' && req.method !== 'DELETE') return J({ error: 'Not found' }, 404);
  const raw = await req.text();
  if (raw.length > 2000) return J({ error: 'Too large' }, 413);
  let body; try { body = JSON.parse(raw); } catch { return J({ error: 'Invalid JSON' }, 400); }
  if (req.method === 'DELETE') {
    // Rankings turned off in the app: its figures are removed straight away.
    if (!body || typeof body.id !== 'string' || !/^[a-f0-9]{32}$/.test(body.id)) return J({ error: 'Invalid ID' }, 400);
    await env.DB.prepare(TABLE).run();
    await env.DB.prepare('DELETE FROM benchmarks WHERE key=?').bind('a:' + await sha('benchmark:' + body.id)).run();
    const linked = await linkedProfile(req, env);
    if (linked) await env.DB.prepare('DELETE FROM benchmarks WHERE key=?').bind('p:' + linked).run();
    return J({ ok: true });
  }
  const v = validate(body);
  if (!v) return J({ error: 'Invalid statistics' }, 400);
  await env.DB.prepare(TABLE).run();
  const profile = await linkedProfile(req, env);
  const key = profile ? 'p:' + profile : 'a:' + await sha('benchmark:' + body.id);
  // A PC that links later replaces its anonymous row, so one player never counts twice.
  if (profile) await env.DB.prepare('DELETE FROM benchmarks WHERE key=?').bind('a:' + await sha('benchmark:' + body.id)).run();
  const stored = v.matches >= MIN_MATCHES;
  if (stored) {
    await env.DB.prepare("INSERT INTO benchmarks (key,matches,total,mates,avg,avg_all,best,best_all,pm,pm_all) VALUES (?,?,?,?,?,?,?,?,?,?) ON CONFLICT(key) DO UPDATE SET matches=excluded.matches,total=excluded.total,mates=excluded.mates,avg=excluded.avg,avg_all=excluded.avg_all,best=excluded.best,best_all=excluded.best_all,pm=excluded.pm,pm_all=excluded.pm_all,updated_at=datetime('now')")
      .bind(key, v.matches, v.total, v.mates, v.avg, v.avgAll, v.best, v.bestAll, v.pm, v.pmAll).run();
  } else {
    await env.DB.prepare('DELETE FROM benchmarks WHERE key=?').bind(key).run();
  }
  // Population: app rankings from the last 180 days, plus public website profiles with synced matches that the app
  // has not reported yet (those only have the "just you" figures; teammates and match length are not synced).
  const counts = METRICS.map(k => `COUNT(${COLUMNS[k]}) AS n_${k}, COALESCE(SUM(${COLUMNS[k]} > ?),0) AS g_${k}`).join(',');
  const sql = `WITH pop AS (
      SELECT total,mates,avg,avg_all,best,best_all,pm,pm_all FROM benchmarks WHERE matches>=${MIN_MATCHES} AND julianday(updated_at)>julianday('now','-180 days')
      UNION ALL
      SELECT SUM(m.opened+m.destroyed),NULL,AVG(m.opened+m.destroyed),NULL,MAX(m.opened+m.destroyed),NULL,NULL,NULL
        FROM matches m JOIN profiles p ON p.id=m.profile_id
        WHERE p.is_private=0 AND NOT EXISTS (SELECT 1 FROM benchmarks b WHERE b.key='p:'||m.profile_id)
        GROUP BY m.profile_id HAVING COUNT(*)>=${MIN_MATCHES}
    ) SELECT ${counts} FROM pop`;
  const row = await env.DB.prepare(sql).bind(...METRICS.map(k => v[k] ?? -1)).first();
  const top = {}, players = {};
  for (const k of METRICS) {
    if (v[k] === null) continue;
    // The caller is part of the population only once it has been stored.
    const n = row['n_' + k] + (stored ? 0 : 1), rank = row['g_' + k] + 1;
    players[k] = n;
    top[k] = Math.min(100, Math.max(1, Math.ceil(100 * rank / n)));
  }
  return J({ top, players, minMatches: MIN_MATCHES });
}
