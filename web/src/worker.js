import { TABLE, computeLuck, weekStart, GOLDEN_WEEK, isGoldenWeek } from "./odds.js";
import { MATCH_EXTRAS } from "./benchmarks.js";
import { withAllTimePirate } from "./alltime.js";

const J = (d, s = 200, h = {}) => new Response(JSON.stringify(d), { status: s, headers: { "content-type": "application/json", "cache-control": "no-store", ...h } });
const bad = (m, s = 400) => J({ error: m }, s);
const hex = (b) => [...new Uint8Array(b)].map((x) => x.toString(16).padStart(2, "0")).join("");
const sha = async (s) => hex(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(s)));
const rnd = (n) => hex(crypto.getRandomValues(new Uint8Array(n)));
const friendId = () => { const r = rnd(4).toUpperCase(); return r.slice(0, 4) + "-" + r.slice(4); };
const iso = (d) => d.toISOString();

async function profileAuth(req, env) {
  const cookie = req.headers.get("cookie") || "";
  const s = cookie.match(/cc_session=([a-f0-9]+)\.([a-f0-9]+)/);
  if (s) {
    try {
      const row = await env.DB.prepare("SELECT profile_id FROM web_sessions WHERE id=? AND secret_hash=? AND expires_at > datetime('now')").bind(s[1], await sha(s[2])).first();
      if (row) return row.profile_id;
    } catch { /* table not created yet */ }
  }
  const m = cookie.match(/cc_profile=([A-F0-9-]+)\.([a-f0-9]+)/);
  if (!m) return null;
  const row = await env.DB.prepare("SELECT profile_id FROM profile_secrets WHERE profile_id=? AND secret_hash=?").bind(m[1], await sha(m[2])).first();
  return row ? m[1] : null;
}

async function pcAuth(req, env) {
  const m = (req.headers.get("authorization") || "").match(/^Bearer ([a-f0-9]+)\.([a-f0-9]+)$/);
  if (!m) return null;
  const row = await env.DB.prepare("SELECT profile_id FROM linked_pcs WHERE id=? AND token_hash=?").bind(m[1], await sha(m[2])).first();
  if (!row) return null;
  await env.DB.prepare("UPDATE linked_pcs SET last_seen_at=datetime('now') WHERE id=?").bind(m[1]).run();
  return row.profile_id;
}

const body = async (req) => { try { return await req.json(); } catch { return {}; } };

async function windows(env) {
  return (await env.DB.prepare("SELECT * FROM treasure_hours ORDER BY start_utc").all()).results;
}

async function weekEvents(env, profileId, start, end) {
  const rows = (await env.DB.prepare("SELECT started_at, opened, chest_times_json FROM matches WHERE profile_id=? AND started_at < ? AND started_at >= ?").bind(profileId, iso(end), iso(new Date(start - 3600e3))).all()).results;
  const ev = [];
  for (const r of rows) {
    const t0 = new Date(r.started_at);
    let times = null;
    try { times = r.chest_times_json ? JSON.parse(r.chest_times_json) : null; } catch { /* ignore */ }
    if (times && times.length) {
      for (const s of times) { const t = new Date(t0.getTime() + s * 1000); if (t >= start && t < end) ev.push({ time: t, count: 1 }); }
    } else if (t0 >= start) ev.push({ time: t0, count: r.opened });
  }
  return ev;
}

const totalsSql = "SELECT p.id, p.username, p.starting_total AS st, COALESCE(SUM(m.opened),0) AS o, COALESCE(SUM(m.destroyed),0) AS d, COUNT(m.session_id) AS n FROM profiles p LEFT JOIN matches m ON m.profile_id=p.id";

export default {
  async fetch(req, env) {
    const url = new URL(req.url);
    const path = url.pathname;
    if (!path.startsWith("/api/")) return env.ASSETS.fetch(req);
    const route = req.method + " " + path;
    try {
      if (route === "POST /api/profile") {
        const existing = await profileAuth(req, env);
        if (existing) return J({ id: existing });
        const id = friendId(), secret = rnd(24);
        await env.DB.batch([
          env.DB.prepare("INSERT INTO profiles (id) VALUES (?)").bind(id),
          env.DB.prepare("INSERT INTO profile_secrets (profile_id, secret_hash) VALUES (?,?)").bind(id, await sha(secret)),
        ]);
        return J({ id }, 200, { "set-cookie": `cc_profile=${id}.${secret}; Path=/; Max-Age=31536000; HttpOnly; Secure; SameSite=Lax` });
      }

      if (route === "GET /api/leaderboard") {
        const sort = { global: "(st+o+d)", opened: "(st+o)", broken: "d", matches: "n" }[url.searchParams.get("sort") || "global"] || "(st+o+d)";
        const rows = (await env.DB.prepare(`SELECT * FROM (${totalsSql} WHERE p.username IS NOT NULL AND p.is_private=0 AND EXISTS (SELECT 1 FROM linked_pcs l WHERE l.profile_id=p.id) GROUP BY p.id) ORDER BY ${sort} DESC LIMIT 100`).all()).results;
        const record = await env.DB.prepare("SELECT p.username, m.opened+m.destroyed AS total, m.mode, m.started_at FROM matches m JOIN profiles p ON p.id=m.profile_id WHERE p.username IS NOT NULL AND p.is_private=0 ORDER BY total DESC LIMIT 1").first();
        return J({ players: rows.map((r, i) => ({ rank: i + 1, username: r.username, global: r.st + r.o + r.d, opened: r.st + r.o, destroyed: r.d, matches: r.n })), record });
      }

      if (route === "GET /api/community") {
        const t = await env.DB.prepare("SELECT COALESCE(SUM(opened),0) AS o, COALESCE(SUM(destroyed),0) AS d, COUNT(*) AS n, COUNT(DISTINCT profile_id) AS p FROM matches").first();
        const types = (await env.DB.prepare("SELECT j.key AS type, SUM(j.value) AS total FROM matches, json_each(matches.chest_types_json) j WHERE chest_types_json IS NOT NULL GROUP BY j.key ORDER BY total DESC").all()).results;
        return J({ opened: t.o, destroyed: t.d, matches: t.n, players: t.p, avgPerMatch: t.n ? (t.o + t.d) / t.n : 0, types });
      }

      if (route === "GET /api/treasure-hours") return J({ windows: await windows(env) });

      if (route === "GET /api/luck") {
        const wanted = url.searchParams.get("profile");
        const me = await profileAuth(req, env);
        let pid = me;
        if (wanted && wanted !== me) {
          const p = await env.DB.prepare("SELECT id, is_private FROM profiles WHERE id=?").bind(wanted).first();
          if (!p || p.is_private) return bad("Profile not found or private", 404);
          pid = p.id;
        }
        if (!pid) return bad("No profile", 401);
        const now = new Date(), start = weekStart(now), end = new Date(start.getTime() + 7 * 864e5);
        const win = await windows(env);
        const ev = await weekEvents(env, pid, start, end);
        // Pirate Shop Access is not a weekly prize: it uses the whole history (personal snapshot only for your own view).
        const prizes = await withAllTimePirate(computeLuck(ev, win, now), env, pid, win, now, pid === me);
        return J({ profile: pid, weekStart: iso(start), chests: ev.reduce((a, e) => a + e.count, 0), prizes, windows: win, goldenWeek: { ...GOLDEN_WEEK, active: isGoldenWeek(now) } });
      }

      if (route === "POST /api/link/claim") {
        const b = await body(req);
        if (b.optIn !== true) return bad("Sync must be turned on by the player (optIn: true)");
        const c = await env.DB.prepare("SELECT * FROM link_codes WHERE code=? AND expires_at > datetime('now')").bind(String(b.code || "").toUpperCase()).first();
        if (!c) return bad("Invalid or expired code", 404);
        const id = rnd(8), secret = rnd(24);
        await env.DB.batch([
          env.DB.prepare("DELETE FROM link_codes WHERE code=?").bind(c.code),
          env.DB.prepare("INSERT INTO linked_pcs (id, profile_id, token_hash) VALUES (?,?,?)").bind(id, c.profile_id, await sha(secret)),
        ]);
        return J({ token: `${id}.${secret}` });
      }

      // The app's "Chests before the counter" estimate (app 2.5.3+), so the website total matches the app and overlay.
      if (route === "POST /api/pc/starting-total") {
        const pid = await pcAuth(req, env);
        if (!pid) return bad("Unauthorized", 401);
        const st = Math.round(Number((await body(req)).total));
        if (!(st >= 0 && st <= 100000)) return bad("Starting total must be 0-100000");
        await env.DB.prepare("UPDATE profiles SET starting_total=? WHERE id=?").bind(st, pid).run();
        return J({ startingTotal: st });
      }

      if (route === "POST /api/matches") {
        const pid = await pcAuth(req, env);
        if (!pid) return bad("Unauthorized", 401);
        const list = ((await body(req)).matches || []).slice(0, 50);
        const stmts = [], extras = [];
        for (const m of list) {
          const t = new Date(m.startedAt);
          if (!m.sessionId || isNaN(t) || !(m.opened >= 0 && m.opened < 1000) || !(m.destroyed >= 0 && m.destroyed < 1000)) continue;
          stmts.push(env.DB.prepare("INSERT OR IGNORE INTO matches (profile_id, session_id, started_at, mode, opened, destroyed, chest_types_json, chest_times_json) VALUES (?,?,?,?,?,?,?,?)")
            .bind(pid, String(m.sessionId).slice(0, 80), iso(t), String(m.mode || "").slice(0, 40), m.opened | 0, m.destroyed | 0,
              m.chestTypes ? JSON.stringify(m.chestTypes) : null, Array.isArray(m.chestTimes) ? JSON.stringify(m.chestTimes.slice(0, 1000)) : null));
          // Teammate chests seen and match length (app 2.4.3+). Kept in their own table so the matches table and
          // older apps are unchanged. Upserted: the app re-sends its already-synced matches once to fill them in.
          const mates = Number.isSafeInteger(m.mates) && m.mates >= 0 && m.mates < 2000 ? m.mates : null;
          const dur = Number.isSafeInteger(m.durationS) && m.durationS > 0 && m.durationS < 7200 ? m.durationS : null;
          if (mates !== null || dur !== null)
            extras.push(env.DB.prepare("INSERT INTO match_extras (profile_id, session_id, mates, duration_s) VALUES (?,?,?,?) ON CONFLICT(profile_id, session_id) DO UPDATE SET mates=excluded.mates, duration_s=excluded.duration_s")
              .bind(pid, String(m.sessionId).slice(0, 80), mates, dur));
        }
        if (stmts.length) await env.DB.batch(stmts);
        if (extras.length) {
          await env.DB.prepare(MATCH_EXTRAS).run();
          await env.DB.batch(extras);
        }
        return J({ accepted: stmts.length, extras: extras.length });
      }

      const me = await profileAuth(req, env);
      if (!me) return bad("No profile. POST /api/profile first.", 401);

      if (route === "POST /api/link/code") {
        const code = rnd(3).toUpperCase();
        await env.DB.prepare("INSERT INTO link_codes (code, profile_id, expires_at) VALUES (?,?,datetime('now','+10 minutes'))").bind(code, me).run();
        return J({ code, expiresInMinutes: 10 });
      }

      if (route === "GET /api/me") {
        const t = await env.DB.prepare(`${totalsSql} WHERE p.id=? GROUP BY p.id`).bind(me).first();
        const p = await env.DB.prepare("SELECT username, is_private, starting_total FROM profiles WHERE id=?").bind(me).first();
        const pcs = (await env.DB.prepare("SELECT id, created_at, last_seen_at FROM linked_pcs WHERE profile_id=?").bind(me).all()).results;
        const recent = (await env.DB.prepare("SELECT started_at, mode, opened, destroyed FROM matches WHERE profile_id=? ORDER BY started_at DESC LIMIT 100").bind(me).all()).results;
        const start = weekStart();
        const daily = (await env.DB.prepare("SELECT substr(started_at,1,10) AS day, SUM(opened+destroyed) AS chests FROM matches WHERE profile_id=? AND started_at >= ? GROUP BY day ORDER BY day").bind(me, iso(start)).all()).results;
        return J({ id: me, ...p, totals: { global: t.st + t.o + t.d, opened: t.st + t.o, destroyed: t.d, matches: t.n }, linkedPcs: pcs, recent, daily });
      }

      if (route === "PUT /api/username") {
        const u = String((await body(req)).username || "").trim();
        if (!/^[A-Za-z0-9_. -]{3,20}$/.test(u)) return bad("3-20 characters: letters, numbers, space, . _ -");
        const linked = await env.DB.prepare("SELECT 1 FROM linked_pcs WHERE profile_id=? LIMIT 1").bind(me).first();
        if (!linked) return bad("Link a PC first", 403);
        const p = await env.DB.prepare("SELECT username_changed_at FROM profiles WHERE id=?").bind(me).first();
        if (p.username_changed_at && Date.now() - new Date(p.username_changed_at + "Z").getTime() < 7 * 864e5) return bad("Username can be changed once every 7 days", 429);
        try { await env.DB.prepare("UPDATE profiles SET username=?, username_changed_at=datetime('now') WHERE id=?").bind(u, me).run(); }
        catch { return bad("Username already taken", 409); }
        return J({ username: u });
      }

      if (route === "PUT /api/settings") {
        const b = await body(req);
        const st = Math.round(Number(b.startingTotal ?? 0));
        if (!(st >= 0 && st <= 100000)) return bad("Starting total must be 0-100000");
        await env.DB.prepare("UPDATE profiles SET is_private=?, starting_total=? WHERE id=?").bind(b.isPrivate ? 1 : 0, st, me).run();
        return J({ isPrivate: !!b.isPrivate, startingTotal: st });
      }

      if (route === "POST /api/friends/groups") {
        const name = String((await body(req)).name || "").trim().slice(0, 30);
        if (!name) return bad("Group name required");
        const n = await env.DB.prepare("SELECT COUNT(*) AS c FROM friend_groups WHERE owner_profile_id=?").bind(me).first();
        if (n.c >= 5) return bad("Maximum 5 groups");
        const id = rnd(6);
        await env.DB.batch([
          env.DB.prepare("INSERT INTO friend_groups (id, owner_profile_id, name) VALUES (?,?,?)").bind(id, me, name),
          env.DB.prepare("INSERT INTO friend_group_members (group_id, profile_id) VALUES (?,?)").bind(id, me),
        ]);
        return J({ id, name });
      }

      const add = path.match(/^\/api\/friends\/groups\/([a-f0-9]+)\/members$/);
      if (add && req.method === "POST") {
        const g = await env.DB.prepare("SELECT id FROM friend_groups WHERE id=? AND owner_profile_id=?").bind(add[1], me).first();
        if (!g) return bad("Group not found", 404);
        const fid = String((await body(req)).friendId || "").trim().toUpperCase();
        const f = await env.DB.prepare("SELECT id FROM profiles WHERE id=?").bind(fid).first();
        if (!f) return bad("No profile with that ID", 404);
        const n = await env.DB.prepare("SELECT COUNT(*) AS c FROM friend_group_members WHERE group_id=?").bind(g.id).first();
        if (n.c >= 25) return bad("Group is full (24 friends)");
        await env.DB.prepare("INSERT OR IGNORE INTO friend_group_members (group_id, profile_id) VALUES (?,?)").bind(g.id, f.id).run();
        return J({ added: f.id });
      }

      if (route === "GET /api/friends/groups") {
        const groups = (await env.DB.prepare("SELECT g.id, g.name FROM friend_groups g JOIN friend_group_members m ON m.group_id=g.id WHERE m.profile_id=?").bind(me).all()).results;
        const out = [];
        for (const g of groups) {
          const rows = (await env.DB.prepare(`SELECT * FROM (${totalsSql} JOIN friend_group_members fm ON fm.profile_id=p.id WHERE fm.group_id=? GROUP BY p.id) ORDER BY (st+o+d) DESC`).bind(g.id).all()).results;
          out.push({ ...g, members: rows.map((r) => ({ name: r.username || r.id, global: r.st + r.o + r.d, matches: r.n })) });
        }
        return J({ groups: out });
      }

      if (route === "DELETE /api/me") {
        await env.DB.prepare("DELETE FROM profiles WHERE id=?").bind(me).run();
        // Rows keyed by profile that are not tied to it by a foreign key (the tables may not exist yet).
        try { await env.DB.prepare("DELETE FROM match_extras WHERE profile_id=?").bind(me).run(); } catch {}
        try { await env.DB.prepare("DELETE FROM benchmarks WHERE key=?").bind("p:" + me).run(); } catch {}
        try { await env.DB.prepare("DELETE FROM owned_rewards WHERE profile_id=?").bind(me).run(); } catch {}
        return J({ deleted: true }, 200, { "set-cookie": "cc_profile=; Path=/; Max-Age=0; HttpOnly; Secure; SameSite=Lax" });
      }

      return bad("Not found", 404);
    } catch (e) {
      return bad("Server error", 500);
    }
  },
};
