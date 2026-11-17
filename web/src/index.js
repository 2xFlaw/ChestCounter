import worker from './worker.js';
import { handleExtra } from './extra.js';
import { seedTreasureHours } from './schedule.js';

export default {
  async fetch(req, env) {
    const url = new URL(req.url);
    if (url.pathname.startsWith('/api/')) {
      try {
        await seedTreasureHours(env);
      } catch (e) {
        // Seeding must never block the API.
      }
      try {
        const r = await handleExtra(req, env, url);
        if (r) return r;
      } catch (e) {
        return new Response(JSON.stringify({ error: 'Server error' }), { status: 500, headers: { 'content-type': 'application/json' } });
      }
    }
    return worker.fetch(req, env);
  },
};
