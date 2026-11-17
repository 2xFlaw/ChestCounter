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
      const rate = Math.min(1, Math.max(0, boostFor(inWindow(e.time), key) / den));
      noDrop *= Math.pow(1 - rate, e.count);
    }
    const rateNow = Math.min(1, Math.max(0, boostFor(live, key) / den));
    return {
      key, name, den,
      chance: 1 - noDrop,
      boostNow: boostFor(live, key),
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
