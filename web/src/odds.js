import WEEKS_DATA from '../public/rewards/weeks.json';

// Per-chest odds copied from the app (WeeklyChances.cs, WeeklyChanceModel.Table). They are per item: during
// Golden Week every earlier week's item is back as well, so the chance of "any" item of a kind adds up.
export const TABLE = [
  ["gold-outfit", "Weekly Gold Outfit", 1562500],
  ["pirate-shop-access", "Pirate Shop Access", 137500],
  ["vehicle-decal", "Weekly Vehicle & Decal", 7813],
  ["pickaxe-back-bling", "Weekly Pickaxe or Back Bling", 568],
  ["jam-track", "Weekly Jam Track", 391],
  ["loading-screen", "Weekly Loading Screen", 272],
  ["gizmo", "Gizmo (Any)", 127],
];

const BOOST_FIELD = { "gold-outfit": "gold", "pirate-shop-access": "shop", "vehicle-decal": "vehicle" };

// Unconfirmed Treasure Hours count as normal odds, like in the app.
export function boostFor(w, key) {
  const f = BOOST_FIELD[key];
  if (!w || !w.confirmed || !f) return 1;
  return w[f] || 1;
}

// Golden Week (announced, same as the app's TreasureHours.cs): from Nov 28 14:00 UTC until the Chapter 7 end event
// (Dec 5, 19:00 UTC) every week's treasure rewards are back and Gold Outfit odds are 2x. Every other item keeps its
// own rate: Epic's example is 1 in 7,813 for a specific Gold Vehicle but 1 in 1,563 for any of the five you don't own.
export const GOLDEN_WEEK = { start: '2026-11-28T14:00:00.000Z', end: '2026-12-05T19:00:00.000Z', boosts: { 'gold-outfit': 2 } };
const GW_START = Date.parse(GOLDEN_WEEK.start), GW_END = Date.parse(GOLDEN_WEEK.end);
export const isGoldenWeek = (t) => { const ms = t instanceof Date ? t.getTime() : t; return ms != null && Number.isFinite(ms) && ms >= GW_START && ms < GW_END; };
export const goldenFor = (t, key) => (isGoldenWeek(t) ? GOLDEN_WEEK.boosts[key] || 1 : 1);
// Multiplier on the normal odds at a moment: a confirmed Treasure Hour's boost (announced against the normal odds,
// so it already includes Golden Week), otherwise the Golden Week multiplier.
export const boostAt = (w, t, key) => (w && w.confirmed ? boostFor(w, key) : goldenFor(t, key));

// Weekly reset: every Sunday 14:00 GMT/UTC, same as the app (WeeklyReset.cs).
export const RESET_HOUR_UTC = 14;
export function weekStart(now = new Date()) {
  const s = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), RESET_HOUR_UTC));
  s.setUTCDate(s.getUTCDate() - s.getUTCDay());
  if (s > now) s.setUTCDate(s.getUTCDate() - 7);
  return s;
}

const WEEK_MS = 7 * 864e5;
const WEEKS = (WEEKS_DATA.weeks || []).map((w) => ({ ...w, s: Date.parse(w.start) })).filter((w) => Number.isFinite(w.s) && Array.isArray(w.items)).sort((a, b) => a.s - b.s);
export const PIRATE_KEY = 'pirate-shop-access@all-time';
const ymd = (ms) => { const d = new Date(ms); return `${d.getUTCFullYear()}${String(d.getUTCMonth() + 1).padStart(2, '0')}${String(d.getUTCDate()).padStart(2, '0')}`; };
// The "Any ..." names used while several items of a kind can drop (Golden Week).
export const ANY_NAMES = { 'gold-outfit': 'Any Gold Outfit', 'vehicle-decal': 'Any Vehicle & Decal', 'pickaxe-back-bling': 'Any Pickaxe or Back Bling', 'jam-track': 'Any Jam Track', 'loading-screen': 'Any Loading Screen', gizmo: 'Any Gizmo' };

// Items of a kind that can drop at time t, and how many of them you still need. owned: { "<slot>@<yyyymmdd>": true }
// (the Treasure Hunt ticks). Outside Golden Week only the live week's item drops; during it, every week's item.
// Gizmos can repeat, so they have no pool (null).
export function poolAt(key, t, owned = {}) {
  if (key === 'pirate-shop-access') { const own = !!owned[PIRATE_KEY]; return { total: 1, left: own ? 0 : 1, items: [{ key: PIRATE_KEY, owned: own }] }; }
  if (key === 'gizmo') return null;
  const ms = t instanceof Date ? t.getTime() : (t ?? Date.now());
  const weeks = isGoldenWeek(ms) ? WEEKS : WEEKS.filter((w) => w.s <= ms && ms < w.s + WEEK_MS);
  const items = weeks.flatMap((w) => w.items.filter((i) => i.slot === key).map((i) => ({ key: `${key}@${ymd(w.s)}`, week: w.week, name: i.name, image: i.image, owned: !!owned[`${key}@${ymd(w.s)}`] })));
  // No reward list for this moment (between seasons, or weeks.json behind): fall back to one item, like before.
  if (!items.length) return { total: 1, left: 1, items: [] };
  return { total: items.length, left: items.filter((i) => !i.owned).length, items };
}
const leftAt = (key, t, owned) => { const p = poolAt(key, t, owned); return p ? p.left : 1; };

// events: [{ time: Date|null, count: number }]; windows: rows from treasure_hours; owned: Treasure Hunt ticks.
export function computeLuck(events, windows, now = new Date(), owned = {}) {
  const inWindow = (t) => t && windows.find((w) => t >= new Date(w.start_utc) && t < new Date(w.end_utc));
  const live = inWindow(now);
  const golden = isGoldenWeek(now);
  return TABLE.map(([key, name, den]) => {
    let noDrop = 1;
    for (const e of events) {
      const rate = Math.min(1, Math.max(0, leftAt(key, e.time || now, owned) * boostAt(inWindow(e.time), e.time, key) / den));
      noDrop *= Math.pow(1 - rate, e.count);
    }
    const pool = poolAt(key, now, owned);
    const left = pool ? pool.left : 1;
    const rateNow = Math.min(1, Math.max(0, left * boostAt(live, now, key) / den));
    // During Golden Week its odds are shown as the normal rate, with any Treasure Hour boost on top of them.
    // den is the per-chest "1 in" for any item of the kind you still need (null once you own them all).
    const g = goldenFor(now, key);
    return {
      key, name: golden && ANY_NAMES[key] ? ANY_NAMES[key] : name, den: left > 0 ? den / (left * g) : null, baseDen: den,
      chance: left > 0 ? 1 - noDrop : 0,
      boostNow: boostAt(live, now, key) / g,
      next50: left > 0 ? 1 - noDrop * Math.pow(1 - rateNow, 50) : 0,
      pool: pool ? { total: pool.total, left } : null,
    };
  });
}

export function chestsToHalf(chance, den) {
  if (chance >= 0.5) return 0;
  return Math.log(0.5 / (1 - chance)) / Math.log(1 - 1 / den);
}

export function after(chance, den, boost, chests) {
  return 1 - (1 - chance) * Math.pow(1 - Math.min(1, Math.max(0, boost / den)), chests);
}
