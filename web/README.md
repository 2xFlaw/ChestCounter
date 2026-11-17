# ChestCounter online (chestcounter.io)

Cloudflare Workers (API + site) and D1 (database).

## Status
- 2A-1 foundation: done
- 2A-2 API: done (untested)
- 2A-3 pages (My stats, % Luck, Leaderboard): done (untested)
- 2A-4 Friends, Community, OBS luck page, Treasure Hours loader: done (untested)
- 2B app side (link this PC, username, opt-in upload): done (not compiled yet)

## Principles
- Sync is OFF by default; players opt in inside the app.
- Players can delete all their data from My stats.
- Private mode hides a player from the public leaderboard.

## Setup
1. The Worker connects to this repository (root directory: web, branch: online-integration). Wrangler creates the D1 database on the first deploy because wrangler.toml has no database_id.
2. In the D1 console, run in order, once each:
   - schema.sql
   - migrations/002_auth.sql
   - migrations/003_obs.sql
3. Add the secret ADMIN_KEY in the Worker settings.
4. Add chestcounter.io as a custom domain on the Worker. A second domain can point at the same Worker.

## Loading Treasure Hours
The app keeps its schedule in 2xflaw-odds.json. Copy the windows into a request like this:

    curl -X POST https://chestcounter.io/api/admin/treasure-hours \
      -H "x-admin-key: YOUR_ADMIN_KEY" -H "content-type: application/json" \
      -d '{"windows":[{"start":"2026-11-21T19:00:00Z","end":"2026-11-21T21:00:00Z","gold":7.5,"shop":1.5,"vehicle":10,"confirmed":true}]}'

Set confirmed to false for hours whose boosts are not announced; they count as normal odds.

## Images (upload by hand)
Upload them to web/public/ in the browser:
- src/ChestCounter/Assets/2xflaw-banner.jpg -> web/public/banner.jpg
- src/ChestCounter/Assets/rewards/*.png -> web/public/rewards/
- src/ChestCounter/Assets/social-*.png -> web/public/social/
