export const $ = (s, r = document) => r.querySelector(s);
export const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
export const n0 = (n) => Number(n).toLocaleString('en-US', { maximumFractionDigits: 0 });
export const pct = (c) => { const v = c * 100; return v <= 0 ? '0%' : (v >= 10 ? v.toFixed(1) : v >= 1 ? v.toFixed(2) : v.toFixed(4)) + '%'; };
export const after = (c, den, b, n) => 1 - (1 - c) * Math.pow(1 - Math.min(b / den, 0.999999), n);

export async function api(path, opt = {}) {
  const r = await fetch(path, { method: opt.method || 'GET', credentials: 'same-origin', headers: { 'content-type': 'application/json' }, body: opt.body ? JSON.stringify(opt.body) : undefined });
  const j = await r.json().catch(() => ({}));
  if (!r.ok) throw new Error(j.error || 'Error ' + r.status);
  return j;
}

// Weekly reset: every Sunday 14:00 UTC, same as the app.
export function resetInfo(now = Date.now()) {
  const d = new Date(now);
  let s = Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate(), 14) - d.getUTCDay() * 864e5;
  if (s > now) s -= 7 * 864e5;
  return { start: s, next: s + 7 * 864e5 };
}

export function countdown(ms) {
  const s = Math.max(0, Math.floor(ms / 1000));
  const p = (n) => String(n).padStart(2, '0');
  return `${Math.floor(s / 86400)}d ${p(Math.floor(s % 86400 / 3600))}h ${p(Math.floor(s % 3600 / 60))}m ${p(s % 60)}s`;
}

export function nav(active) {
  const links = [['stats', '/', 'My stats'], ['luck', '/luck.html', '% Luck'], ['community', '/community.html', 'Community'], ['board', '/leaderboard.html', 'Leaderboard'], ['friends', '/friends.html', 'Friends']];
  document.body.insertAdjacentHTML('afterbegin', `<nav class='nav'><a class='brand' href='/'>Chest Counter</a>${links.map((l) => `<a class='${l[0] === active ? 'on' : ''}' href='${l[1]}'>${l[2]}</a>`).join('')}</nav>`);
}

export function startResetClock(el) {
  const tick = () => { el.textContent = countdown(resetInfo().next - Date.now()); };
  tick();
  setInterval(tick, 1000);
}
