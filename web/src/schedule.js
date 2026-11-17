// Treasure Hour windows that must exist on the website. Inserted once; existing rows are never overwritten.
// Boosts for the last two windows are not announced yet, so they are stored as unconfirmed (counted as normal odds).
const BOOSTS = { gold: 7.5, shop: 1.5, vehicle: 10 };
const WINDOWS = [
  { start: '2026-11-21T19:00:00.000Z', end: '2026-11-21T21:00:00.000Z', confirmed: 1 },
  { start: '2026-11-22T02:00:00.000Z', end: '2026-11-22T04:00:00.000Z', confirmed: 1 },
  { start: '2026-11-28T19:00:00.000Z', end: '2026-11-28T21:00:00.000Z', confirmed: 0 },
  { start: '2026-11-29T02:00:00.000Z', end: '2026-11-29T04:00:00.000Z', confirmed: 0 },
];

let seeded = null;
export function seedTreasureHours(env) {
  if (!seeded) {
    seeded = env.DB.batch(WINDOWS.map((w) => env.DB.prepare('INSERT OR IGNORE INTO treasure_hours (start_utc, end_utc, gold, shop, vehicle, confirmed) VALUES (?,?,?,?,?,?)')
      .bind(w.start, w.end, BOOSTS.gold, BOOSTS.shop, BOOSTS.vehicle, w.confirmed))).catch((e) => { seeded = null; throw e; });
  }
  return seeded;
}
