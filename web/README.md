# ChestCounter online (chesttracker.io)

Cloudflare Workers (API + site) and D1 (database).

## Status
- 2A-1 foundation: done
- 2A-2 API: done (untested)
- 2A-3 pages (My stats, % Luck, Leaderboard): done (untested)
- 2A-4 Friends, Community, OBS luck page, Treasure Hours loader: done (untested)
- 2B app side (link this PC, username, opt-in upload): not started

## Principles
- Sync is OFF by default; players opt in inside the app.
- Players can delete all their data from My stats.
- Private mode hides a player from the public leaderboard.

## Setup
1. wrangler d1 create chestcounter, then put the id in wrangler.toml.
2. Apply, in order, with: wrangler d1 execute chestcounter --remote --file=<file>
   - schema.sql
   - migrations/002_auth.sql
   - migrations/003_obs.sql
3. Set the admin secret: wrangler secret put ADMIN_KEY
4. Deploy: wrangler deploy
5. Add chesttracker.io as a custom domain to the Worker in the Cloudflare dashboard.

## Loading Treasure Hours
The app keeps its schedule in 2xflaw-odds.json. Copy the windows into a request like this:

    curl -X POST https://chesttracker.io/api/admin/treasure-hours \
      -H "x-admin-key: YOUR_ADMIN_KEY" -H "content-type: application/json" \
      -d '{"windows":[{"start":"2026-11-21T19:00:00Z","end":"2026-11-21T21:00:00Z","gold":7.5,"shop":1.5,"vehicle":10,"confirmed":true}]}'

Set confirmed to false for hours whose boosts are not announced; they count as normal odds.

## Images (upload by hand)
GitHub cannot receive images from the assistant. Upload them to web/public/ in the browser:
- src/ChestCounter/Assets/2xflaw-banner.jpg -> web/public/banner.jpg
- src/ChestCounter/Assets/rewards/*.png -> web/public/rewards/
- src/ChestCounter/Assets/social-*.png -> web/public/social/
