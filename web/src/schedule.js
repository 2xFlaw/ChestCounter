// Treasure Hour windows that must exist on the website. This file is the source of truth for these rows.
// Every window currently has the same boosts.
const BOOSTS = { gold: 7.5, shop: 1.5, vehicle: 10 };
const WINDOWS = [
  { start: '2026-11-21T19:00:00.000Z', end: '2026-11-21T21:00:00.000Z' },
  { start: '2026-11-22T02:00:00.000Z', end: '2026-11-22T04:00:00.000Z' },
  { start: '2026-11-28T19:00:00.000Z', end: '2026-11-28T21:00:00.000Z' },
  { start: '2026-11-29T02:00:00.000Z', end: '2026-11-29T04:00:00.000Z' },
];

let seeded = null;
export function seedTreasureHours(env) {
  if (!seeded) {
    seeded = env.DB.batch(WINDOWS.map((w) => env.DB.prepare('INSERT OR REPLACE INTO treasure_hours (start_utc, end_utc, gold, shop, vehicle, confirmed) VALUES (?,?,?,?,?,1)')
      .bind(w.start, w.end, BOOSTS.gold, BOOSTS.shop, BOOSTS.vehicle))).catch((e) => { seeded = null; throw e; });
  }
  return seeded;
}
