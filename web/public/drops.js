// "Drop rates" on the % Luck page: the per-chest odds of every treasure, as "1 in" and as a percentage, normally
// and in the next Treasure Hour. During Golden Week every earlier week's item is back, so each row is "any" item of
// that kind you still need: the per-item rates add up, and anything ticked in the Treasure Hunt leaves the pool.
import { esc, n0 } from '/app.js';
import { huntBus, huntOwned } from '/hunt.js';

const GW_START = Date.parse('2026-11-28T14:00:00Z'), GW_END = Date.parse('2026-12-05T19:00:00Z');
const WEEK_MS = 7 * 864e5;
const PIRATE = 'pirate-shop-access@all-time';
// Epic's per-item odds (same as the app and the server). th: which Treasure Hour boost applies.
const KINDS = [
  { key: 'gold-outfit', any: 'Any Gold Outfit', weekly: 'Weekly Gold Outfit', den: 1562500, th: 'gold', golden: 2 },
  { key: 'pirate-shop-access', any: 'Pirate Shop Access', weekly: 'Pirate Shop Access', den: 137500, th: 'shop' },
  { key: 'vehicle-decal', any: 'Any Vehicle & Decal', weekly: 'Weekly Vehicle & Decal', den: 7813, th: 'vehicle' },
  { key: 'pickaxe-back-bling', any: 'Any Pickaxe or Back Bling', weekly: 'Weekly Pickaxe or Back Bling', den: 568 },
  { key: 'jam-track', any: 'Any Jam Track', weekly: 'Weekly Jam Track', den: 391 },
  { key: 'loading-screen', any: 'Any Loading Screen', weekly: 'Weekly Loading Screen', den: 272 },
  { key: 'gizmo', any: 'Any Gizmo', weekly: 'Gizmo (Any)', den: 127, repeat: true },
];
const TICK = "<svg viewBox='0 0 16 16' aria-hidden='true'><path d='M3.6 8.4l2.9 2.8 5.9-6.2' fill='none' stroke='currentColor' stroke-width='2.6' stroke-linecap='round' stroke-linejoin='round'/></svg>";

const CSS = `
.drops{padding:22px 24px 10px}
.drops .dth{border:1px solid rgba(255,184,77,.32);background:linear-gradient(180deg,rgba(255,184,77,.09),rgba(255,184,77,.03));border-radius:18px;padding:16px 18px 18px;margin-bottom:10px}
.drops .dth h3{display:flex;align-items:center;gap:10px;color:var(--amber);font-size:24px;line-height:1;margin:0 0 14px}
.drops .dth h3 svg{width:22px;height:22px;flex:none}
.drops .dwins{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}
.drops .dwin{background:rgba(0,0,0,.22);border:1px solid var(--line);border-radius:14px;padding:12px 14px 14px}
.drops .dwin.live{border-color:rgba(61,220,151,.5);background:rgba(61,220,151,.08)}
.drops .dwin .lbl{display:flex;justify-content:space-between;gap:10px;margin-bottom:8px}
.drops .dwin .lbl span:last-child{letter-spacing:.04em;text-transform:none;font-weight:600}
.drops .dwin.live .lbl span:first-child{color:var(--mint)}
.drops .dcd{display:grid;grid-template-columns:repeat(4,1fr);gap:8px}
.drops .dcd span{display:flex;align-items:baseline;justify-content:center;gap:2px;padding:10px 4px;border-radius:10px;background:#120c17;font:400 clamp(24px,3vw,34px)/1 var(--display);letter-spacing:.02em}
.drops .dcd small{font:800 12px/1 var(--body);color:var(--amber)}
.drops .dnone{color:var(--muted);font-size:13px}
.drops .dhead,.drops .drow{display:grid;grid-template-columns:minmax(170px,1.1fr) minmax(0,1.5fr) 150px 190px;gap:16px;align-items:center}
.drops .dhead{padding:14px 4px 10px;border-bottom:1px solid var(--line);font-size:12.5px;color:var(--muted)}
.drops .dhead span:nth-child(n+3){text-align:right}
.drops .dhead .thh{display:flex;align-items:center;justify-content:flex-end;gap:6px;color:var(--amber)}
.drops .dhead .thh svg{width:14px;height:14px}
.drops .drow{padding:14px 4px;border-bottom:1px solid var(--line)}
.drops .drow.done{opacity:.62}
.drops .dnm{font-weight:800;font-size:15.5px;line-height:1.25}
.drops .dleft{color:var(--muted);font-size:12.5px;font-weight:600;margin-top:2px}
.drops .drow.done .dleft{color:var(--mint)}
.drops .dpics{display:flex;flex-wrap:wrap;gap:8px}
.drops .dpic{position:relative;width:52px;height:52px;padding:0;border-radius:11px;border:1.5px solid var(--line2);background:#221829;overflow:hidden;cursor:pointer;transition:transform .15s,border-color .2s}
.drops .dpic img{width:100%;height:100%;object-fit:cover;display:block;transition:filter .3s}
.drops .dpic img.cut{object-fit:contain;padding:4px}
.drops .dpic:not([disabled]):hover{transform:translateY(-2px);border-color:var(--acc)}
.drops .dpic[disabled]{cursor:default}
.drops .dpic.own{border-color:var(--mint)}
.drops .dpic.own img{filter:grayscale(1) brightness(.45)}
.drops .dpic .dt{position:absolute;inset:0;display:none;align-items:center;justify-content:center;color:var(--mint)}
.drops .dpic.own .dt{display:flex}
.drops .dpic .dt svg{width:24px;height:24px;filter:drop-shadow(0 1px 3px #000)}
.drops .dodd{text-align:right;line-height:1.2}
.drops .dodd .o{font:800 19px/1.1 var(--body);font-variant-numeric:tabular-nums;white-space:nowrap}
.drops .dodd .p{color:var(--muted);font-size:12px;font-weight:600;font-variant-numeric:tabular-nums;margin-top:3px}
.drops .dodd.hot .o{color:#ffd58f}
.drops .dodd.hot .p{color:rgba(255,184,77,.8)}
.drops .dodd .x{display:inline-block;margin-right:8px;padding:3px 8px;border-radius:7px;background:var(--amber);color:#2a1a02;font:800 12px/1.2 var(--body);vertical-align:2px}
.drops .dodd .na{color:var(--faint)}
.drops .dodd .dh{display:none;font-size:10.5px;font-weight:700;letter-spacing:.14em;text-transform:uppercase;color:var(--muted);margin-bottom:3px}
.drops .dodd.hot .dh{color:var(--amber)}
.drops .dodd.got .o{color:var(--mint);font-size:15px}
.drops .dall{display:grid;grid-template-columns:minmax(170px,1.1fr) minmax(0,1.5fr) 150px 190px;gap:16px;align-items:center;padding:16px 4px 10px;color:var(--muted)}
.drops .dall .dnm{font-weight:700;font-size:14px;color:var(--muted)}
.drops .dfoot{color:var(--muted);font-size:12.5px;padding:6px 4px 10px}
@media(max-width:860px){
 .drops{padding:18px 14px 6px}
 .drops .dhead{display:none}
 .drops .drow,.drops .dall{grid-template-columns:1fr 1fr;gap:10px 12px}
 .drops .drow>div:nth-child(2){grid-column:1/-1;order:3}
 .drops .drow>.dname{grid-column:1/-1}
 .drops .dodd{text-align:left}
 .drops .dodd .dh{display:block}
 .drops .dall .dnm{grid-column:1/-1}
 .drops .dall>div:nth-child(2){display:none}
 .drops .dpic{width:46px;height:46px}
}
`;
const DH_N = "<span class='dh'>Per chest</span>", DH_T = "<span class='dh'>Treasure Hour</span>";
const CLOCK = "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.2' stroke-linecap='round' aria-hidden='true'><circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/></svg>";

const ymd = ms => { const d = new Date(ms); return `${d.getUTCFullYear()}${String(d.getUTCMonth() + 1).padStart(2, '0')}${String(d.getUTCDate()).padStart(2, '0')}`; };
// Percent with enough digits to mean something for tiny odds: 0.00064%, 0.064%, 1.28%, 12.5%.
export const pctOdds = r => { const v = r * 100; if (!(v > 0)) return '0%'; return (v >= 10 ? v.toFixed(1) : v >= 1 ? v.toFixed(2) : Number(v.toPrecision(2)).toString()) + '%'; };
const locale = () => { const l = (document.documentElement.lang || 'en').toLowerCase(); return l.startsWith('en') ? 'en-GB' : l; };
const when = ms => { try { return new Date(ms).toLocaleString(locale(), { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }); } catch { return new Date(ms).toLocaleString('en-GB'); } };
const split = ms => { const s = Math.max(0, Math.floor(ms / 1000)); return [Math.floor(s / 86400), Math.floor(s % 86400 / 3600), Math.floor(s % 3600 / 60), s % 60]; };

function fit(img) {
  try {
    const c = document.createElement('canvas'), g = c.getContext('2d', { willReadFrequently: true }); c.width = c.height = 24;
    g.drawImage(img, 0, 0, 24, 24);
    if ([[1, 1], [22, 1], [1, 22], [22, 22]].some(([x, y]) => g.getImageData(x, y, 1, 1).data[3] < 200)) img.classList.add('cut');
  } catch { img.classList.add('cut'); }
}

export async function initDrops(root, windows) {
  if (!root) return;
  const style = document.createElement('style'); style.textContent = CSS; document.head.append(style);
  let weeks = [];
  try { weeks = ((await (await fetch('/rewards/weeks.json', { cache: 'no-cache' })).json()).weeks || []).map(w => ({ ...w, s: Date.parse(w.start) })).filter(w => Number.isFinite(w.s) && Array.isArray(w.items)).sort((a, b) => a.s - b.s); } catch { weeks = []; }
  const W = (windows || []).map(w => ({ ...w, s: Date.parse(w.start_utc), e: Date.parse(w.end_utc) })).filter(w => Number.isFinite(w.s) && Number.isFinite(w.e)).sort((a, b) => a.s - b.s);
  let timer = 0;

  function render() {
    const now = Date.now();
    const owned = huntOwned();
    // From two weeks before Golden Week until it ends, show its odds (all weeks' items, Gold Outfits 2x).
    const golden = now < GW_END && now >= GW_START - 14 * 864e5;
    const goldenLive = now >= GW_START && now < GW_END;
    const cur = weeks.findLast(w => w.s <= now && now < w.s + WEEK_MS) || null;
    const pool = golden ? weeks.filter(w => w.s < GW_END) : cur ? [cur] : [];
    const th = W.find(w => now >= w.s && now < w.e) || W.find(w => w.s > now) || null;
    const thBoost = k => (th && th.confirmed && k.th ? Number(th[k.th]) || 1 : 1);
    let sumN = 0, sumT = 0, anyRow = false;

    const rows = KINDS.map(k => {
      let items;
      if (k.key === 'pirate-shop-access') items = [{ key: PIRATE, image: 'pirate-shop-access.png', name: 'Pirate Shop Token', can: true }];
      else if (k.repeat) items = [{ key: null, image: 'gizmo.png', name: 'Gizmo', can: false }];
      else items = pool.flatMap(w => w.items.filter(i => i.slot === k.key).map(i => ({ key: `${k.key}@${ymd(w.s)}`, image: i.image, name: i.name, week: w.week, can: w.s <= now || goldenLive })));
      if (!items.length) return '';
      anyRow = true;
      const total = k.repeat ? 1 : items.length;
      const left = k.repeat ? 1 : items.filter(i => !owned[i.key]).length;
      const g = golden && k.golden ? k.golden : 1;
      const rN = left * g / k.den;
      // Treasure Hour boosts are announced against the normal odds and already include Golden Week (15x = 7.5x * 2x).
      const b = thBoost(k), rT = left * Math.max(b, g) / k.den;
      sumN += rN; sumT += b > 1 ? rT : rN;
      const name = golden ? k.any : k.weekly;
      const sub = k.repeat ? 'Can drop more than once' : k.key === 'pirate-shop-access' ? (left ? 'Found any week' : 'Found') : left ? `${left} of ${total} left` : 'All found';
      const pics = items.map(i => {
        const own = i.key && owned[i.key];
        const tip = i.key ? (i.can ? (own ? 'Click to mark as not found' : 'Click to mark as found') : 'Not available yet') : i.name;
        return `<button type='button' class='dpic${own ? ' own' : ''}' ${i.key && i.can ? `data-key='${esc(i.key)}' aria-pressed='${!!own}'` : 'disabled'} title='${esc(tip)}' aria-label='${esc(i.name)}' translate='no'><img src='/rewards/${esc(i.image)}' alt='' width='52' height='52' loading='lazy' decoding='async'><span class='dt'>${TICK}</span></button>`;
      }).join('');
      const odd = (r, hot, boost) => !left ? `<div class='dodd got'>${hot ? DH_T : DH_N}<div class='o'>Found</div></div>`
        : `<div class='dodd${hot ? ' hot' : ''}'>${hot ? DH_T : DH_N}<div class='o'>${boost ? `<span class='x'>${esc(String(boost))}x</span>` : ''}<span>1 in ${n0(Math.round(1 / r))}</span></div><div class='p'>${pctOdds(r)}</div></div>`;
      return `<div class='drow${left ? '' : ' done'}'><div class='dname'><div class='dnm'>${esc(name)}</div><div class='dleft'>${esc(sub)}</div></div><div class='dpics'>${pics}</div>` +
        odd(rN, false, 0) + (b > 1 ? odd(rT, true, b) : `<div class='dodd'>${DH_T}<div class='o na'>-</div></div>`) + `</div>`;
    }).join('');

    const wins = W.filter(w => w.e > now && w.s - now < 7 * 864e5).slice(0, 2);
    const meta = golden ? (goldenLive ? "Golden Week odds. Treasure you've found is left out." : "Golden Week odds, from 28 November. Treasure you've found is left out.") : "This week's odds. Treasure you've found is left out.";
    root.innerHTML = `<div class='dth'><h3>${CLOCK}<span>Treasure Hours</span></h3>` +
      (wins.length ? `<div class='dwins'>${wins.map((w, i) => `<div class='dwin${now >= w.s ? ' live' : ''}' data-i='${i}'><div class='lbl'><span>${now >= w.s ? 'Ends in' : 'Starts in'}</span><span>${esc(when(w.s))}</span></div><div class='dcd num'>${['d', 'h', 'm', 's'].map(u => `<span><b>00</b><small>${u}</small></span>`).join('')}</div></div>`).join('')}</div>`
        : `<div class='dnone'>No Treasure Hour in the next 7 days yet.</div>`) + `</div>` +
      (anyRow ? `<div class='dhead'><span>Treasure</span><span></span><span>Per chest</span><span class='thh'>${CLOCK}<span>Treasure Hour</span></span></div>${rows}` +
        `<div class='dall'><div class='dnm'>Any treasure</div><div></div>` +
        `<div class='dodd'>${DH_N}<div class='o' style='font-size:16px;color:var(--muted)'><span>1 in ${n0(Math.round(1 / sumN))}</span></div><div class='p'>${pctOdds(sumN)}</div></div>` +
        (sumT > sumN * 1.0001 ? `<div class='dodd hot'>${DH_T}<div class='o' style='font-size:16px'><span>1 in ${n0(Math.round(1 / sumT))}</span></div><div class='p'>${pctOdds(sumT)}</div></div>` : `<div class='dodd'>${DH_T}<div class='o na'>-</div></div>`) + `</div>`
        : `<div class='dnone' style='padding:16px 4px'>No treasure is in chests right now.</div>`) +
      `<div class='dfoot'>Click a picture when you find it: it leaves the pool here and is ticked in the Treasure Hunt below.</div>`;
    root.closest('section')?.querySelector('.meta')?.replaceChildren(document.createTextNode(meta));
    root.querySelectorAll('.dpic img').forEach(img => { if (img.complete && img.naturalWidth) fit(img); else img.addEventListener('load', () => fit(img), { once: true }); });
    root.querySelectorAll('.dpic[data-key]').forEach(el => { el.onclick = () => huntBus.dispatchEvent(new CustomEvent('toggle', { detail: { key: el.dataset.key } })); });

    const tick = () => {
      const t = Date.now();
      root.querySelectorAll('.dwin').forEach(el => {
        const w = wins[Number(el.dataset.i)];
        if (!w) return;
        if ((t >= w.s) !== el.classList.contains('live') || t >= w.e) { render(); return; }
        const parts = split((t >= w.s ? w.e : w.s) - t);
        el.querySelectorAll('.dcd b').forEach((b, i) => { b.textContent = String(parts[i]).padStart(2, '0'); });
      });
    };
    clearInterval(timer); tick(); timer = setInterval(() => { if (!document.hidden) tick(); }, 1000);
  }

  huntBus.addEventListener('change', render);
  new MutationObserver(render).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
  render();
}
