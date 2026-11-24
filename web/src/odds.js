// Per-chest odds copied from the app (WeeklyChances.cs, WeeklyChanceModel.Table).
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
// (Dec 5, 19:00 UTC) every week's treasure rewards are back, Gold Outfit odds are 2x and Gold Vehicle odds 5x.
export const GOLDEN_WEEK = { start: '2026-11-28T14:00:00.000Z', end: '2026-12-05T19:00:00.000Z', boosts: { 'gold-outfit': 2, 'vehicle-decal': 5 } };
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

// events: [{ time: Date|null, count: number }]; windows: rows from treasure_hours.
export function computeLuck(events, windows, now = new Date()) {
  const inWindow = (t) => t && windows.find((w) => t >= new Date(w.start_utc) && t < new Date(w.end_utc));
  const live = inWindow(now);
  return TABLE.map(([key, name, den]) => {
    let noDrop = 1;
    for (const e of events) {
      const rate = Math.min(1, Math.max(0, boostAt(inWindow(e.time), e.time, key) / den));
      noDrop *= Math.pow(1 - rate, e.count);
    }
    const rateNow = Math.min(1, Math.max(0, boostAt(live, now, key) / den));
    // During Golden Week its odds are shown as the normal rate, with any Treasure Hour boost on top of them.
    const g = goldenFor(now, key);
    return {
      key, name, den: den / g, baseDen: den,
      chance: 1 - noDrop,
      boostNow: boostAt(live, now, key) / g,
      next50: 1 - noDrop * Math.pow(1 - rateNow, 50),
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
