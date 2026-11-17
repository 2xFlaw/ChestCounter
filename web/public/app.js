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
  const links = [['stats', '/#dashboard', 'My stats'], ['luck', '/luck.html', '% Luck'], ['community', '/community.html', 'Community'], ['board', '/leaderboard.html', 'Leaderboard'], ['friends', '/friends.html', 'Friends']];
  document.body.insertAdjacentHTML('afterbegin', `<nav class='nav'><a class='brand' href='/'>Chest Counter</a>${links.map((l) => `<a class='${l[0] === active ? 'on' : ''}' href='${l[1]}'>${l[2]}</a>`).join('')}</nav>`);
  document.body.insertAdjacentHTML('beforeend', `<footer style='max-width:1180px;margin:0 auto;padding:0 18px 40px;color:var(--muted);font-size:12.5px'>Made by 2xFlaw &middot; Contact: <a href='mailto:flxwlesslybusiness@gmail.com'>flxwlesslybusiness@gmail.com</a></footer>`);
  enhance(active);
}

export function startResetClock(el) {
  const tick = () => { el.textContent = countdown(resetInfo().next - Date.now()); };
  tick();
  setInterval(tick, 1000);
}

const rewardFiles = ['gold-outfit','pirate-shop-access','vehicle-decal','pickaxe-back-bling','jam-track','loading-screen','gizmo'];

function artwork(key, name, className = '') {
  const img = document.createElement('img');
  img.src = `/rewards/${key}.png`;
  img.alt = name;
  img.className = className;
  img.decoding = 'async';
  img.addEventListener('error', () => { img.hidden = true; }, { once: true });
  return img;
}

function enhance(active) {
  const style = document.createElement('style');
  style.textContent = `
.cc-landing{isolation:isolate;overflow:hidden;border:1px solid #533045;border-radius:28px;margin:14px 0 42px;background:radial-gradient(ellipse at 85% 20%,#7c28534d,transparent 60%),#100c17}
.cc-intro{min-height:620px;display:grid;grid-template-columns:1.15fr 1fr;align-items:center;gap:20px;padding:clamp(24px,5vw,64px);position:relative}
.cc-kicker{color:#ffb84d;letter-spacing:.22em;font-size:11px;font-weight:800;text-transform:uppercase}
.cc-intro h2{font-size:clamp(54px,8vw,104px);line-height:.98;margin:20px 0;letter-spacing:-.025em}
.cc-intro h2 span{color:#ff66b2;text-shadow:0 0 60px #ff66b240}
.cc-copy{max-width:460px;color:#beb1c8;font-size:16px;line-height:1.8}
.cc-actions{display:flex;flex-wrap:wrap;gap:12px;margin:28px 0}
.cc-actions a{display:inline-flex;align-items:center;justify-content:center;text-decoration:none;padding:15px 22px;border-radius:12px;font-weight:800;border:1px solid #64425c;color:#fff;transition:transform .2s,box-shadow .2s}
.cc-actions a:first-child{background:#ff66b2;color:#160c19;border-color:#ff66b2;box-shadow:0 8px 40px #ff66b230}
.cc-actions a:hover{transform:translateY(-3px);box-shadow:0 10px 40px #ff66b245}
.cc-fine{color:#a596b2;font-size:12px}
.cc-stage{position:relative;min-width:0;height:440px;display:grid;place-items:center}
.cc-stage:before{content:'';position:absolute;inset:35px;border:1px solid #ff66b24d;border-radius:50%;box-shadow:0 0 100px #ff66b222,inset 0 0 80px #ff66b218}
.cc-art{object-fit:contain;filter:drop-shadow(0 20px 25px #0008);animation:cc-float 7s ease-in-out infinite}
.cc-stage .cc-art{position:absolute;width:62%;height:90%}
.cc-stage .cc-art:nth-child(2){width:39%;height:38%;left:-2%;bottom:0;animation-delay:-2s}
.cc-stage .cc-art:nth-child(3){width:34%;height:34%;right:0;top:0;animation-delay:-4s}
.cc-story{margin:0;padding:clamp(28px,6vw,70px);display:grid;grid-template-columns:1fr 1fr;align-items:center;gap:40px;border-top:1px solid #ffffff12;min-height:400px}
.cc-story h2{font-size:clamp(32px,5vw,60px);line-height:1.05;margin:12px 0}
.cc-story .cc-art{width:100%;max-height:280px}
.cc-story p{color:#beb1c8;font-size:16px;line-height:1.8}
.cc-story a{font-weight:800}
.cc-reveal{opacity:0;transform:translateY(36px);transition:opacity .85s ease,transform .85s cubic-bezier(.2,.65,.25,1)}
.cc-reveal.cc-visible{opacity:1;transform:none}
#dashboard{scroll-margin-top:24px}
.cc-prize-image{width:100%;height:100%;object-fit:contain;padding:10px}
.cc-row-image{width:48px;height:48px;object-fit:contain;float:left;margin-right:12px}
.prow:has(.cc-row-image){min-height:76px}
@keyframes cc-float{0%,100%{transform:translateY(0) rotate(-3deg)}50%{transform:translateY(-15px) rotate(2deg)}}
@media(max-width:700px){.cc-intro{grid-template-columns:1fr;min-height:0}.cc-stage{height:310px}.cc-intro h2{font-size:clamp(50px,12vw,80px)}.cc-story{grid-template-columns:1fr;gap:20px;min-height:0}.cc-story .cc-art{max-height:210px}.cc-actions a{flex:1}.cc-stage:before{inset:10px 45px}}
@media(prefers-reduced-motion:reduce){.cc-art{animation:none!important}.cc-reveal{opacity:1;transform:none;transition:none}.cc-actions a:hover{transform:none}}
`;
  document.head.append(style);

  if (active === 'luck') {
    const goal = document.querySelector('#goal');
    const rows = document.querySelector('#rows');
    const decorate = () => {
      if (rows) rows.querySelectorAll('[data-k]').forEach(row => {
        const key = row.dataset.k;
        if (!rewardFiles.includes(key) || row.querySelector('.cc-row-image')) return;
        row.firstElementChild?.prepend(artwork(key, '', 'cc-row-image'));
      });
      const badge = goal?.querySelector('.badge');
      if (!badge || badge.querySelector('img')) return;
      const key = rows?.querySelector('.sel[data-k]')?.dataset.k;
      if (rewardFiles.includes(key)) badge.replaceChildren(artwork(key, goal.querySelector('h3')?.textContent || 'Reward', 'cc-prize-image'));
    };
    const observer = new MutationObserver(decorate);
    if (goal) observer.observe(goal, { childList: true, subtree: true });
    if (rows) observer.observe(rows, { childList: true, subtree: true });
    decorate();
  }

  if (active !== 'stats') return;

  document.title = 'Chest Counter — Make every chest count';
  const main = document.querySelector('main');
  if (!main || document.querySelector('.cc-landing')) return;
  const dashboard = main.querySelector('header');
  if (dashboard) dashboard.id = 'dashboard';

  const landing = document.createElement('div');
  landing.className = 'cc-landing';
  landing.innerHTML = `
<div class='cc-intro'>
<div>
<div class='cc-kicker'>2xFlaw's Chest Counter</div>
<h2>EVERY CHEST.<br>EVERY CHANCE.<br><span>KEEP GRINDING.</span></h2>
<p class='cc-copy'>Turn your Fortnite chest grind into a story worth tracking. Follow your matches, watch your weekly chances grow, and plan your next session.</p>
<div class='cc-actions'><a href='https://github.com/2xFlaw/ChestCounter/releases/latest'>GRIND NOW ↗</a><a href='#dashboard'>MY STATS ↓</a></div>
<div class='cc-fine'>Free for Windows 10 / 11 · Online sync is optional.</div>
</div>
<div class='cc-stage' aria-hidden='true'>
<img class='cc-art' src='/rewards/gold-outfit.png' alt='' fetchpriority='high'>
<img class='cc-art' src='/rewards/pirate-shop-access.png' alt=''>
<img class='cc-art' src='/rewards/gizmo.png' alt=''>
</div>
</div>
<section class='cc-story'>
<img class='cc-art' src='/rewards/pickaxe-back-bling.png' alt='Weekly pickaxe and back bling reward' loading='lazy'>
<div>
<div class='cc-kicker'>01 / Make progress visible</div>
<h2>NOT JUST ANOTHER<br>CHEST OPENED.</h2>
<p>Keep your chest counts and match history together. Your next session becomes part of the bigger picture.</p>
<a href='#dashboard'>See your progress →</a>
</div>
</section>
<section class='cc-story'>
<div>
<div class='cc-kicker'>02 / Know your chances</div>
<h2>CHASE THE REWARD.<br>UNDERSTAND THE ODDS.</h2>
<p>Explore weekly prize probabilities and plan around confirmed Treasure Hour boosts. A chance is never a guarantee — but your grind deserves a clear view.</p>
<a href='/luck.html'>Explore your luck →</a>
</div>
<img class='cc-art' src='/rewards/jam-track.png' alt='Weekly Jam Track reward' loading='lazy'>
</section>`;
  main.prepend(landing);

  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (!reduced && 'IntersectionObserver' in window) {
    const observer = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.classList.add('cc-visible'); observer.unobserve(entry.target); }
    }), { threshold: 0.08 });
    const targets = [...landing.querySelectorAll('.cc-story'), ...main.querySelectorAll(':scope > section')];
    targets.forEach(section => { section.classList.add('cc-reveal'); observer.observe(section); });
  }

  if (location.hash === '#dashboard') requestAnimationFrame(() => dashboard?.scrollIntoView());
}
