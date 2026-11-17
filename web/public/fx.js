const doc = document.documentElement;
let stored = null;
try { stored = localStorage.getItem('cc.motion'); } catch { /* storage blocked */ }
const osReduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
// On by default for everyone. Only an explicit "off" from the visitor turns it off.
export const motion = stored !== 'off';

if (window.__fxT) clearTimeout(window.__fxT);
doc.dataset.motion = motion ? 'full' : 'reduced';
if (!motion) doc.classList.remove('fx');

if (!document.querySelector('link[href="/fx.css"]')) {
  const l = document.createElement('link');
  l.rel = 'stylesheet';
  l.href = '/fx.css';
  document.head.append(l);
}

// Motion is on by default, so drop the stylesheets' blanket system reduced-motion rules (the visitor can still switch motion off).
function stripReducedRules() {
  for (const sheet of document.styleSheets) {
    let rules;
    try { rules = sheet.cssRules; } catch { continue; }
    for (let i = rules.length - 1; i >= 0; i--) {
      const r = rules[i];
      if (r.media && /prefers-reduced-motion/.test(r.conditionText || r.media.mediaText || '')) sheet.deleteRule(i);
    }
  }
}
if (motion && osReduced) {
  stripReducedRules();
  addEventListener('load', () => { stripReducedRules(); setTimeout(stripReducedRules, 800); setTimeout(stripReducedRules, 2500); });
}

export function reveal(els) {
  if (!motion || !('IntersectionObserver' in window)) return;
  const io = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (!e.isIntersecting) continue;
      const el = e.target;
      io.unobserve(el);
      el.classList.add('in');
      const wait = 1500 + (Number(el.style.getPropertyValue('--i')) || 0) * 90;
      setTimeout(() => el.classList.remove('fx-hide', 'in'), wait);
    }
  }, { threshold: 0.08, rootMargin: '0px 0px -6% 0px' });
  els.forEach((el) => {
    if (el.parentElement && el.parentElement.classList.contains('kpis')) el.style.setProperty('--i', [...el.parentElement.children].indexOf(el));
    el.classList.add('fx-hide');
    io.observe(el);
  });
}

function countUp(card) {
  const v = card.querySelector('.v');
  if (!v) return;
  const end = Number(v.textContent.replace(/[^0-9.]/g, ''));
  if (!isFinite(end) || end < 2) return;
  const t0 = performance.now(), dur = 1100;
  const step = (t) => {
    const p = Math.min(1, (t - t0) / dur), e = 1 - Math.pow(1 - p, 3);
    v.textContent = Math.round(end * e).toLocaleString('en-US');
    if (p < 1) requestAnimationFrame(step);
  };
  v.textContent = '0';
  requestAnimationFrame(step);
}

function progressBar() {
  const bar = document.createElement('div');
  bar.className = 'fx-progress';
  document.body.append(bar);
  const update = () => {
    const h = doc.scrollHeight - innerHeight;
    bar.style.setProperty('--p', h > 0 ? Math.min(1, scrollY / h).toFixed(4) : '0');
  };
  addEventListener('scroll', update, { passive: true });
  addEventListener('resize', update);
  update();
}

function cursorGlow() {
  if (!matchMedia('(pointer: fine)').matches) return;
  const g = document.createElement('div');
  g.className = 'fx-glow';
  document.body.append(g);
  let tx = innerWidth / 2, ty = innerHeight / 2, x = tx, y = ty, raf = 0;
  const loop = () => {
    x += (tx - x) * 0.12;
    y += (ty - y) * 0.12;
    g.style.setProperty('--x', x.toFixed(1) + 'px');
    g.style.setProperty('--y', y.toFixed(1) + 'px');
    raf = Math.abs(tx - x) > 0.5 || Math.abs(ty - y) > 0.5 ? requestAnimationFrame(loop) : 0;
  };
  addEventListener('pointermove', (e) => {
    tx = e.clientX;
    ty = e.clientY;
    g.classList.add('on');
    if (!raf) raf = requestAnimationFrame(loop);
  }, { passive: true });
}

function tactile() {
  if (!matchMedia('(pointer: fine)').matches) return;
  let cur = null;
  const release = (el) => {
    el.style.transform = '';
    el.style.removeProperty('--mx');
    el.style.removeProperty('--my');
    if (cur === el) cur = null;
  };
  document.addEventListener('pointermove', (e) => {
    const el = e.target.closest ? e.target.closest('.download, .btn.on, .art-card:not(.art-small)') : null;
    if (cur && cur !== el) release(cur);
    cur = el;
    if (!el) return;
    const r = el.getBoundingClientRect();
    const dx = (e.clientX - r.left - r.width / 2) / r.width;
    const dy = (e.clientY - r.top - r.height / 2) / r.height;
    if (el.classList.contains('art-card')) {
      el.style.transform = `perspective(700px) rotateY(${(dx * 14).toFixed(2)}deg) rotateX(${(-dy * 14).toFixed(2)}deg) scale(1.03)`;
    } else {
      el.style.setProperty('--mx', (dx * 10).toFixed(1) + 'px');
      el.style.setProperty('--my', (dy * 8).toFixed(1) + 'px');
    }
  }, { passive: true });
  document.addEventListener('pointerleave', () => { if (cur) release(cur); });
}

function toggle() {
  const b = document.createElement('button');
  b.type = 'button';
  b.className = 'fx-toggle';
  b.textContent = motion ? 'Motion: on' : 'Motion: off';
  b.setAttribute('aria-pressed', String(motion));
  b.title = motion
    ? (osReduced ? 'Animations are on. Your system prefers reduced motion, so you may want to turn them off.' : 'Click to turn animations off')
    : 'Click to turn animations on';
  b.addEventListener('click', () => {
    try { localStorage.setItem('cc.motion', motion ? 'off' : 'on'); } catch { /* storage blocked */ }
    location.reload();
  });
  document.body.append(b);
}

function boot() {
  toggle();
  if (!motion) return;
  progressBar();
  cursorGlow();
  tactile();
  if (document.querySelector('.hero.scene')) return;
  reveal([...document.querySelectorAll('main > header, main > section, main > .note')]);
  const k = document.querySelector('.kpis');
  if (k) {
    new MutationObserver(() => {
      const fresh = [...k.children].filter((c) => !c.dataset.fx);
      fresh.forEach((c) => { c.dataset.fx = '1'; });
      if (!fresh.length) return;
      reveal(fresh);
      fresh.forEach(countUp);
    }).observe(k, { childList: true });
  }
}

if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
else boot();
