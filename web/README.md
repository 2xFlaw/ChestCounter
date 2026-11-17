# ChestCounter online (chesttracker.io)

Cloudflare Pages (site) + Workers (API) + D1 (database).

## Status
- Part 2A-1: foundation (this commit)
- Part 2A-2: API
- Part 2A-3: pages
- Part 2A-4: OBS + weekly report

## Principles
- Sync is OFF by default; players opt in inside the app.
- Players can delete all their data from the site.
- Friend groups and private mode keep stats hidden from the public leaderboard.

## Setup (later)
1. Create a D1 database named chestcounter and put its id in wrangler.toml.
2. Apply schema: wrangler d1 execute chestcounter --file=schema.sql --remote
3. Deploy.
