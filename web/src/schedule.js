// Treasure Hour windows that must exist on the website. This file is the source of truth for these rows.
// Boosts multiply the normal odds. The usual Treasure Hour is 7.5x / 1.5x / 10x; the Golden Week Treasure Hour
// (Nov 28, both sessions) was announced as Gold Outfit 15x (1 in 104,167), Pirate Shop 1.5x, Vehicle & Decal 10x.
const BOOSTS = { gold: 7.5, shop: 1.5, vehicle: 10 };
const GOLDEN_HOUR = { gold: 15, shop: 1.5, vehicle: 10 };
const WINDOWS = [
  { start: '2026-11-21T19:00:00.000Z', end: '2026-11-21T21:00:00.000Z' },
  { start: '2026-11-22T02:00:00.000Z', end: '2026-11-22T04:00:00.000Z' },
  { start: '2026-11-28T19:00:00.000Z', end: '2026-11-28T21:00:00.000Z', boosts: GOLDEN_HOUR },
  { start: '2026-11-29T02:00:00.000Z', end: '2026-11-29T04:00:00.000Z', boosts: GOLDEN_HOUR },
];

let seeded = null;
export function seedTreasureHours(env) {
  if (!seeded) {
    seeded = env.DB.batch(WINDOWS.map((w) => env.DB.prepare('INSERT OR REPLACE INTO treasure_hours (start_utc, end_utc, gold, shop, vehicle, confirmed) VALUES (?,?,?,?,?,1)')
      .bind(w.start, w.end, (w.boosts || BOOSTS).gold, (w.boosts || BOOSTS).shop, (w.boosts || BOOSTS).vehicle))).catch((e) => { seeded = null; throw e; });
  }
  return seeded;
}
