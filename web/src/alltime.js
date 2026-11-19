// Pirate Shop Access over the whole chest history, like the app (PirateShopChances.cs).
// The other prizes reset every week; Pirate Shop Access does not, so its odds use every chest:
//  - the profile's starting total (base odds),
//  - every synced match: chests with a time use that time's Treasure Hour boost, the rest base odds,
//  - for the profile's own view, its personal history snapshot from the app (imports, matches from before
//    syncing, manual entries and teammate chests, following the app's settings), without counting synced
//    matches twice.
import { TABLE, boostFor } from './odds.js';

const KEY = 'pirate-shop-access';
const DEN = TABLE.find((t) => t[0] === KEY)[2];

const sha = async (s) => Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s))), (v) => v.toString(16).padStart(2, '0')).join('');

export async function pirateAllTime(env, profileId, windows, now = new Date(), withPersonal = false) {
  const W = windows.map((w) => ({ w, s: Date.parse(w.start_utc), e: Date.parse(w.end_utc) }));
  const nowMs = now.getTime();
  const boostAt = (t) => {
    if (t == null || !Number.isFinite(t)) return 1;
    const hit = W.find((x) => t >= x.s && t < x.e);
    return hit ? boostFor(hit.w, KEY) : 1;
  };
  // Sum of logs so very long histories do not underflow.
  let logNoDrop = 0, chests = 0;
  const add = (t, n) => {
    n = Math.floor(Number(n) || 0);
    if (n <= 0) return;
    const rate = Math.min(1, Math.max(0, boostAt(t) / DEN));
    logNoDrop += rate >= 1 ? -Infinity : n * Math.log1p(-rate);
    chests += n;
  };

  const profile = await env.DB.prepare('SELECT starting_total FROM profiles WHERE id=?').bind(profileId).first();
  add(null, profile ? profile.starting_total : 0);

  let snapshot = null;
  if (withPersonal) {
    try {
      const row = await env.DB.prepare('SELECT payload FROM personal_snapshots WHERE profile_id=?').bind(profileId).first();
      snapshot = row ? JSON.parse(row.payload) : null;
    } catch { snapshot = null; } // table not created yet
  }

  const rows = (await env.DB.prepare('SELECT session_id, started_at, opened, destroyed, chest_times_json FROM matches WHERE profile_id=?').bind(profileId).all()).results;
  const synced = new Set();
  for (const r of rows) {
    const t0 = Date.parse(r.started_at);
    if (Number.isFinite(t0) && t0 > nowMs) continue;
    let times = [];
    try { times = r.chest_times_json ? JSON.parse(r.chest_times_json) : []; } catch { times = []; }
    if (!Array.isArray(times)) times = [];
    const total = Math.max(0, (r.opened | 0) + (r.destroyed | 0));
    const stamped = Math.min(total, times.length);
    for (let i = 0; i < stamped; i++) {
      const t = t0 + Number(times[i]) * 1000;
      if (!(t > nowMs)) add(t, 1);
    }
    add(null, total - stamped);
    // The app identifies matches in the personal snapshot by SHA-256 of "session:<id>".
    if (snapshot) synced.add(await sha('session:' + r.session_id));
  }

  if (snapshot) {
    for (const m of snapshot.matches || []) {
      add(null, m.team); // teammate chests use base odds, as in the app
      if (synced.has(m.id)) continue; // already counted above, with exact chest times
      const t = m.startedAt ? Date.parse(m.startedAt) : null;
      if (t != null && t > nowMs) continue;
      add(t, (m.opened | 0) + (m.destroyed | 0));
    }
    for (const e of snapshot.entries || []) {
      const t = Date.parse(e.at);
      if (!(t > nowMs)) add(t, e.amount);
    }
  }

  const rateNow = Math.min(1, Math.max(0, boostAt(nowMs) / DEN));
  const chance = 1 - Math.exp(logNoDrop);
  const next50 = 1 - Math.exp(logNoDrop + (rateNow >= 1 ? -Infinity : 50 * Math.log1p(-rateNow)));
  return { chance, next50, chests };
}

// Replaces the weekly Pirate Shop Access entry with the all-time one.
export async function withAllTimePirate(prizes, env, profileId, windows, now, withPersonal) {
  const all = await pirateAllTime(env, profileId, windows, now, withPersonal);
  return prizes.map((p) => (p.key === KEY ? { ...p, chance: all.chance, next50: all.next50, allTime: true, allTimeChests: all.chests } : p));
}
