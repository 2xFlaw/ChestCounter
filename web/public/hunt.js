// Treasure Hunt grid on the % Luck page. Ticks are shared with the Chest Counter app through /api/owned
// (newest change wins) and kept in this browser too, so nothing is lost while offline.
import { $, esc, api, countdown } from '/app.js';

const STORE = 'cc.hunt';
const PIRATE = 'pirate-shop-access@all-time';
const WEEK_MS = 7 * 864e5;
const SLOTS = ['gold-outfit', 'vehicle-decal', 'pickaxe-back-bling', 'jam-track', 'loading-screen'];
const reduced = matchMedia('(prefers-reduced-motion: reduce)');

const CSS = `
.hunt{--slot:#34ceb2;position:relative;overflow:hidden}
.hunt .hhead{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:flex-end;gap:10px 24px;margin-bottom:18px}
.hunt .eyebrow{color:var(--amber)}
.hunt h2.ht{font-size:clamp(30px,4vw,44px);line-height:1;margin:6px 0 8px}
.hunt .hcount{text-align:right}
.hunt .hcount .hbig{font:400 clamp(40px,5vw,58px)/1 var(--display);letter-spacing:.02em}
.hunt .hcount .hbig b{color:var(--mint);font-weight:400}
.hunt .hcount .hbig i{font-style:normal;color:var(--muted);margin:0 .12em}
.hunt .pirate{display:flex;align-items:center;gap:16px;width:100%;padding:14px 20px;margin:0 0 22px;border-radius:16px;border:1.5px solid rgba(246,190,70,.42);background:linear-gradient(90deg,rgba(246,190,70,.08),rgba(246,190,70,.02));text-align:left;cursor:pointer;transition:border-color .2s,background .3s,transform .2s}
.hunt .pirate:hover{border-color:#f6be46;transform:translateY(-1px)}
.hunt .pirate img{width:50px;height:50px;object-fit:contain;filter:grayscale(1) brightness(.6);transition:filter .5s}
.hunt .pirate .pt{font:400 21px/1.1 var(--display);letter-spacing:.04em;text-transform:uppercase;color:#b9a37a;transition:color .4s}
.hunt .pirate .ps{color:var(--muted);font-size:12.5px}
.hunt .pirate .pst{margin-left:auto;font:400 22px/1 var(--display);letter-spacing:.04em;color:var(--muted);white-space:nowrap;text-transform:uppercase}
.hunt .pirate.is-found{border-color:#f6be46;background:linear-gradient(90deg,rgba(246,190,70,.24),rgba(246,190,70,.04))}
.hunt .pirate.is-found img{filter:none}
.hunt .pirate.is-found .pt{color:#f6be46}
.hunt .pirate.is-found .pst{color:var(--mint)}
.hunt .hweek{display:grid;grid-template-columns:150px 1fr;gap:14px;align-items:center;margin-bottom:22px}
.hunt .wl h3{font-size:30px;line-height:1}
.hunt .wl .hsub{color:var(--muted);font-size:12.5px;font-weight:700;margin-top:4px}
.hunt .wl .hnow{display:inline-block;margin-top:8px;padding:2px 9px;border-radius:999px;border:1px solid rgba(246,190,70,.6);background:rgba(246,190,70,.18);color:#f6be46;font-size:10.5px;font-weight:800;letter-spacing:.12em}
.hunt .hgold{margin:0 0 14px;padding:10px 14px;border-radius:14px;border:1px solid rgba(246,190,70,.55);background:linear-gradient(90deg,rgba(246,190,70,.2),rgba(246,190,70,.05));color:#f6d68a;font-size:13px;font-weight:700}
.hunt .hgold b{color:#f6be46;text-transform:uppercase;letter-spacing:.06em;margin-right:6px}
.hunt .hweek.cur .wl h3{color:#f6be46}
.hunt .hweek.future{opacity:.55}
.hunt .tiles{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:14px}
.hunt .tile{position:relative;display:flex;flex-direction:column;align-items:stretch;gap:8px;padding:0;border:0;background:none;color:inherit;text-align:center;cursor:pointer}
.hunt .tile[disabled]{cursor:not-allowed}
.hunt .box{position:relative;aspect-ratio:1;border-radius:14px;border:2px solid #423448;background:#1f1625;overflow:visible;transition:border-color .3s,transform .2s,box-shadow .3s}
.hunt .box .clip{position:absolute;inset:0;border-radius:12px;overflow:hidden;display:flex;align-items:center;justify-content:center}
.hunt .box img{width:100%;height:100%;object-fit:cover;filter:grayscale(1) brightness(.6) contrast(.95);transition:filter .45s ease,transform .45s ease}
.hunt .box img.cut{width:86%;height:86%;object-fit:contain}
.hunt .tile:not([disabled]):hover .box{transform:translateY(-3px);border-color:color-mix(in srgb,var(--slot) 55%,#423448)}
.hunt .tile:not([disabled]):hover .box img{filter:grayscale(.6) brightness(.8)}
.hunt .tile.is-found .box{border-color:var(--slot);box-shadow:0 0 0 1px color-mix(in srgb,var(--slot) 30%,transparent),0 10px 26px color-mix(in srgb,var(--slot) 22%,transparent)}
.hunt .tile.is-found .box img,.hunt .tile.is-found:hover .box img{filter:none}
.hunt .hbadge{position:absolute;top:7px;right:7px;width:24px;height:24px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:rgba(14,10,18,.75);color:var(--muted);z-index:2}
.hunt .hbadge svg{width:13px;height:13px}
.hunt .tile.is-found .hbadge{background:var(--mint);color:#0a2819;transform:scale(1)}
.hunt .tile.is-found .hbadge svg{width:15px;height:15px}
.hunt .hnm{font-size:12.5px;font-weight:700;line-height:1.25;color:var(--muted);transition:color .3s;overflow-wrap:anywhere}
.hunt .tile.is-found .hnm{color:var(--ink)}
.hunt .foot{border-top:1px solid var(--line2);padding-top:12px;color:var(--muted);font-size:12.5px}
.hunt .foot div+div{margin-top:4px}
.hunt .ring{position:absolute;inset:-2px;border-radius:16px;border:3px solid var(--slot);pointer-events:none;animation:hring .7s ease-out forwards}
.hunt .spark{position:absolute;left:50%;top:50%;width:6px;height:6px;margin:-3px;border-radius:50%;background:#fff0c8;box-shadow:0 0 8px #ffd77a;pointer-events:none;animation:hspark .7s ease-out forwards}
.hunt .tile.pop .box{animation:hpop .7s cubic-bezier(.2,.8,.3,1)}
.hunt .tile.pop .box img{animation:hcolour .7s ease-out}
.hunt .tile.pop .hbadge{animation:htick .45s .15s cubic-bezier(.34,1.56,.64,1) both}
.hunt .pirate.pop{animation:hpop .7s cubic-bezier(.2,.8,.3,1)}
@keyframes hpop{0%{transform:scale(1)}35%{transform:scale(1.08)}100%{transform:scale(1)}}
@keyframes hcolour{from{filter:grayscale(1) brightness(.6)}to{filter:none}}
@keyframes htick{from{transform:scale(0)}to{transform:scale(1)}}
@keyframes hring{from{opacity:.9;transform:scale(1)}to{opacity:0;transform:scale(1.22)}}
@keyframes hspark{from{opacity:1;transform:translate(0,0) scale(1)}to{opacity:0;transform:translate(var(--dx),var(--dy)) scale(.3)}}
@media(max-width:760px){.hunt .hweek{grid-template-columns:1fr}.hunt .wl{display:flex;align-items:baseline;flex-wrap:wrap;gap:4px 12px}.hunt .wl .hnow{margin-top:0}.hunt .tiles{gap:9px}.hunt .hnm{font-size:11px}.hunt .hbadge{width:20px;height:20px;top:5px;right:5px}}
@media(max-width:460px){.hunt .tiles{grid-template-columns:repeat(3,minmax(0,1fr))}.hunt .pirate{flex-wrap:wrap;gap:6px 14px}.hunt .pirate>span:nth-of-type(1){flex:1 1 0}.hunt .pirate .pst{font-size:17px;flex-basis:100%;padding-left:54px;margin-left:0}.hunt .pirate img{width:40px;height:40px}}
@media(prefers-reduced-motion:reduce){.hunt *,.hunt .tile.pop .box,.hunt .tile.pop .box img,.hunt .tile.pop .hbadge,.hunt .pirate.pop{animation:none!important;transition:none!important}.hunt .ring,.hunt .spark{display:none}}
`;

const COLOURS = { 'gold-outfit': '#d6aa48', 'vehicle-decal': '#ec8034', 'pickaxe-back-bling': '#4896ec', 'jam-track': '#ec54aa', 'loading-screen': '#34ceb2' };
const LOCK = "<svg viewBox='0 0 16 16' aria-hidden='true'><rect x='3' y='7' width='10' height='7.5' rx='1.6' fill='currentColor'/><path d='M5.2 7V5.2a2.8 2.8 0 0 1 5.6 0V7' fill='none' stroke='currentColor' stroke-width='1.8'/></svg>";
const TICK = "<svg viewBox='0 0 16 16' aria-hidden='true'><path d='M3.6 8.4l2.9 2.8 5.9-6.2' fill='none' stroke='currentColor' stroke-width='2.4' stroke-linecap='round' stroke-linejoin='round'/></svg>";

const ymd = ms => { const d = new Date(ms); return `${d.getUTCFullYear()}${String(d.getUTCMonth() + 1).padStart(2, '0')}${String(d.getUTCDate()).padStart(2, '0')}`; };
const locale = () => { const l = (document.documentElement.lang || 'en').toLowerCase(); return l.startsWith('en') ? 'en-GB' : l; };
const day = ms => { try { return new Date(ms).toLocaleDateString(locale(), { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' }); } catch { return new Date(ms).toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' }); } };

function load() { try { const v = JSON.parse(localStorage.getItem(STORE) || '{}'); return v && typeof v === 'object' ? v : {}; } catch { return {}; } }
function save(map) { try { localStorage.setItem(STORE, JSON.stringify(map)); } catch { /* storage blocked */ } }

// Opaque art (loading screens, jam covers) fills the tile; cut-outs are shown whole.
function fit(img) {
  try {
    const c = document.createElement('canvas'), w = c.width = 32, h = c.height = 32, g = c.getContext('2d', { willReadFrequently: true });
    g.drawImage(img, 0, 0, w, h);
    const corners = [[2, 2], [w - 3, 2], [2, h - 3], [w - 3, h - 3]];
    if (corners.some(([x, y]) => g.getImageData(x, y, 1, 1).data[3] < 200)) img.classList.add('cut');
  } catch { img.classList.add('cut'); }
}

export async function initHunt(root) {
  if (!root) return;
  const style = document.createElement('style'); style.textContent = CSS; document.head.append(style);
  let data;
  try { data = await (await fetch('/rewards/weeks.json', { cache: 'no-cache' })).json(); } catch { root.closest('section')?.remove(); return; }
  const weeks = (data.weeks || []).map(w => ({ ...w, s: Date.parse(w.start) })).filter(w => Number.isFinite(w.s) && Array.isArray(w.items)).sort((a, b) => a.s - b.s);
  if (!weeks.length) { root.closest('section')?.remove(); return; }

  let map = load();
  let synced = false;
  const owned = key => !!map[key]?.v;

  // the page script creates the browser profile on a first visit, so wait for it rather than racing it
  async function fetchOwned() {
    for (let i = 0; ; i++) {
      try { return await api('/api/owned'); } catch (e) { if (i >= 4 || !/Unauthorized/.test(e.message)) throw e; await new Promise(r => setTimeout(r, 800 * (i + 1))); }
    }
  }

  async function sync() {
    try {
      const server = await fetchOwned();
      const push = {};
      for (const [k, v] of Object.entries(map)) { const st = server.times?.[k]; if (st == null || v.t > st) push[k] = v; }
      const merged = {};
      for (const [k, v] of Object.entries(server.owned || {})) merged[k] = { v, t: server.times?.[k] ?? 0 };
      for (const [k, v] of Object.entries(push)) merged[k] = v;
      map = merged; save(map); synced = true;
      const keys = Object.keys(push);
      for (let i = 0; i < keys.length; i += 200) await api('/api/owned', { method: 'POST', body: { set: Object.fromEntries(keys.slice(i, i + 200).map(k => [k, push[k]])) } });
    } catch { synced = false; }
  }

  async function setOwned(key, value) {
    map[key] = { v: value, t: Date.now() }; save(map);
    try { await api('/api/owned', { method: 'POST', body: { set: { [key]: map[key] } } }); synced = true; } catch { synced = false; }
    status();
  }

  function burst(box) {
    if (reduced.matches) return;
    const ring = document.createElement('i'); ring.className = 'ring'; box.append(ring);
    for (let i = 0; i < 10; i++) {
      const a = i / 10 * Math.PI * 2 + Math.random() * .4, r = box.offsetWidth * (.55 + Math.random() * .2);
      const s = document.createElement('i'); s.className = 'spark';
      s.style.setProperty('--dx', `${Math.cos(a) * r}px`); s.style.setProperty('--dy', `${Math.sin(a) * r}px`);
      box.append(s);
    }
    setTimeout(() => box.querySelectorAll('.ring,.spark').forEach(e => e.remove()), 800);
  }

  function status() {
    const el = root.querySelector('.syncline');
    if (el) el.textContent = synced ? 'Saved to your profile. Link your PC to see the same ticks in the Chest Counter app.' : 'Saved in this browser for now. It syncs to your profile next time the website can be reached.';
  }

  function counts() {
    const now = Date.now();
    const open = weeks.filter(w => w.s <= now);
    const total = open.reduce((n, w) => n + w.items.length, 0);
    const found = open.reduce((n, w) => n + w.items.filter(it => owned(`${it.slot}@${ymd(w.s)}`)).length, 0);
    root.querySelector('.hcount b').textContent = String(found);
    root.querySelector('.hcount .tot').textContent = String(total);
    root.querySelectorAll('.hweek').forEach(row => {
      const w = weeks[Number(row.dataset.i)];
      const n = w.items.filter(it => owned(`${it.slot}@${ymd(w.s)}`)).length;
      const fc = row.querySelector('.fc'); if (fc) fc.textContent = `${n}/${w.items.length} found`;
    });
  }

  function render() {
    const now = Date.now();
    const cur = weeks.findLast(w => w.s <= now && now < w.s + WEEK_MS) || null;
    const end = cur ? cur.s + WEEK_MS : null;
    const pirate = data.pirate || { name: 'Pirate Shop Token', image: 'pirate-shop-access.png' };
    // Golden Week (Nov 28 14:00 UTC to the Dec 5 end event): every week's rewards can drop again.
    const golden = now >= Date.parse('2026-11-28T14:00:00Z') && now < Date.parse('2026-12-05T19:00:00Z');
    root.innerHTML =
      `<div class='hhead'><div><div class='lbl eyebrow'>Treasure Hunt</div><h2 class='ht'>Weekly rewards</h2>` +
      `<div class='muted' style='font-size:12.5px;font-weight:700'>${cur ? `<span>${esc(`Week ${cur.week} ends ${day(end)}`)}</span> · <span>Resets in</span> <span class='num hcd'></span>` : '<span>No Treasure Hunt week is live right now.</span>'}</div></div>` +
      `<div class='hcount'><div class='lbl'>Found</div><div class='hbig num'><b>0</b><i>/</i><span class='tot'>0</span></div></div></div>` +
      (golden ? `<div class='hgold'><b>Golden Week</b> <span>Every treasure reward is back, so items from earlier weeks can still drop.</span></div>` : '') +
      `<button type='button' class='pirate${owned(PIRATE) ? ' is-found' : ''}' aria-pressed='${owned(PIRATE)}'><img src='/rewards/${esc(pirate.image)}' alt='' width='50' height='50'><span><span class='pt'>Pirate Shop Token</span><br><span class='ps'>14 days of free item shops - found any week</span></span><span class='pst'>${owned(PIRATE) ? 'Found it' : 'Not found'}</span></button>` +
      weeks.map((w, i) => {
        const future = w.s > now, isCur = w === cur;
        return `<div class='hweek${isCur ? ' cur' : ''}${future ? ' future' : ''}' data-i='${i}'><div class='wl'><h3>Week ${w.week}</h3>` +
          `<div class='hsub'>${future ? esc(`Starts ${day(w.s)}`) : `<span class='fc'>0/${w.items.length} found</span>`}</div>${isCur ? "<span class='hnow'>This week</span>" : ''}</div>` +
          `<div class='tiles'>${w.items.map(it => {
            const key = `${it.slot}@${ymd(w.s)}`, on = owned(key), slot = SLOTS.includes(it.slot) ? it.slot : 'loading-screen';
            return `<button type='button' class='tile${on ? ' is-found' : ''}' data-key='${esc(key)}' style='--slot:${COLOURS[slot]}' aria-pressed='${on}' ${future ? "disabled title='Not available yet'" : `title='${on ? 'Click to mark as not found' : 'Click to mark as found'}'`}>` +
              `<span class='box'><span class='clip'><img src='/rewards/${esc(it.image)}' alt='' loading='lazy' decoding='async'></span><span class='hbadge'>${on ? TICK : LOCK}</span></span><span class='hnm' translate='no'>${esc(it.name)}</span></button>`;
          }).join('')}</div></div>`;
      }).join('') +
      `<div class='foot'><div>Click an item when you find it.</div><div class='syncline'></div></div>`;
    root.querySelectorAll('.box img').forEach(img => { if (img.complete && img.naturalWidth) fit(img); else img.addEventListener('load', () => fit(img), { once: true }); });

    root.querySelectorAll('.tile:not([disabled])').forEach(t => t.addEventListener('click', () => {
      const key = t.dataset.key, value = !owned(key);
      t.classList.toggle('is-found', value); t.setAttribute('aria-pressed', String(value));
      t.title = value ? 'Click to mark as not found' : 'Click to mark as found';
      t.querySelector('.hbadge').innerHTML = value ? TICK : LOCK;
      t.classList.remove('pop');
      if (value) { void t.offsetWidth; t.classList.add('pop'); burst(t.querySelector('.box')); }
      setOwned(key, value); counts();
    }));
    const p = root.querySelector('.pirate');
    p.addEventListener('click', () => {
      const value = !owned(PIRATE);
      p.classList.toggle('is-found', value); p.setAttribute('aria-pressed', String(value));
      p.querySelector('.pst').textContent = value ? 'Found it' : 'Not found';
      p.classList.remove('pop'); if (value) { void p.offsetWidth; p.classList.add('pop'); }
      setOwned(PIRATE, value);
    });
    counts(); status();
    const cd = root.querySelector('.hcd');
    if (cd && end) { const tick = () => { cd.textContent = countdown(end - Date.now()); }; tick(); clearInterval(render.timer); render.timer = setInterval(tick, 1000); }
  }

  // dates follow the page language, so draw again when the language picker changes it
  new MutationObserver(() => render()).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
  render();
  await sync();
  render();
  // roll over to the next week without a reload
  setInterval(() => { const now = Date.now(); const cur = weeks.findLast(w => w.s <= now && now < w.s + WEEK_MS); if ((cur?.week ?? 0) !== (render.week ?? 0)) { render.week = cur?.week ?? 0; render(); } }, 60000);
  render.week = weeks.findLast(w => w.s <= Date.now() && Date.now() < w.s + WEEK_MS)?.week ?? 0;
}
