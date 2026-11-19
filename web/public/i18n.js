import { ROWS, HEADS, PATS } from '/i18n-data.js';
const NL = String.fromCharCode(10);
const LANGS = [['es', 'Español'], ['en', 'English'], ['pt', 'Português (Brasil)'], ['ru', 'Русский'], ['fr', 'Français'], ['de', 'Deutsch'], ['it', 'Italiano'], ['pl', 'Polski'], ['tr', 'Türkçe'], ['ja', '日本語']];
const COLS = ['es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
const PAGE_LINKS = new Set(['/', '/index.html', '/stats.html', '/luck.html', '/community.html', '/leaderboard.html', '/friends.html', '/privacy.html', '/link.html', '/claim.html']);
const PREFIX = new RegExp('^/([a-z]{2})(-[A-Za-z]{2,4})?(?=/|$)', 'i');
const rows = block => block.trim().split(NL).map(line => line.split('|'));
const dict = new Map(rows(ROWS).map(r => [r[0], r.slice(1)]));
const heads = rows(HEADS);
const pats = rows(PATS).map(r => [new RegExp(r[0]), r.slice(1)]);
const nodes = new WeakMap();
const attrs = new WeakMap();
const ATTRS = ['placeholder', 'aria-label', 'title', 'alt'];
const SKIP = 'script,style,textarea,noscript,.cc-lang';
let lang = 'en', htmlLang = 'en', titleEn = '', picker = null, label = null, menu = null, button = null, observer = null;

const CSS = `
.cc-lang{position:relative;display:inline-flex;align-items:center;margin:0 12px;font:600 13px/1 Inter,system-ui,sans-serif}
.nav .cc-lang{margin-left:auto}
.cc-lang.float{position:fixed;top:14px;right:14px;z-index:1000;margin:0}
.cc-lang-btn{display:inline-flex;align-items:center;gap:8px;padding:9px 13px;border-radius:999px;border:1px solid rgba(255,255,255,.18);background:rgba(255,255,255,.06);color:#f9ecf6;font:inherit;cursor:pointer;backdrop-filter:blur(10px);transition:border-color .2s,background .2s,box-shadow .2s}
.cc-lang-btn:hover,.cc-lang.open .cc-lang-btn{border-color:#ff7fb4;background:rgba(255,127,180,.12);box-shadow:0 0 0 4px rgba(208,0,128,.16)}
.cc-lang-btn:focus-visible{outline:2px solid #ff7fb4;outline-offset:2px}
.cc-lang-btn svg:last-child{transition:transform .2s}
.cc-lang.open .cc-lang-btn svg:last-child{transform:rotate(180deg)}
.cc-lang-menu{position:absolute;right:0;top:calc(100% + 10px);z-index:1000;min-width:210px;max-height:min(70vh,430px);overflow:auto;margin:0;padding:6px;list-style:none;border-radius:16px;border:1px solid #451c3c;background:#19101e;box-shadow:0 20px 60px rgba(0,0,0,.6);visibility:hidden;opacity:0;transform:translateY(-8px) scale(.97);transform-origin:top right;transition:opacity .18s,transform .18s,visibility .18s}
.cc-lang.open .cc-lang-menu{visibility:visible;opacity:1;transform:none}
.cc-lang-menu li{display:flex;justify-content:space-between;gap:14px;padding:10px 12px;border-radius:10px;color:#f9ecf6;cursor:pointer}
.cc-lang-menu li:hover,.cc-lang-menu li:focus-visible{background:#46183d;outline:none}
.cc-lang-menu li[aria-selected=true]{color:#ff7fb4}
.cc-lang-menu li[aria-selected=true]::after{content:'✓'}
@media(max-width:640px){.cc-lang{margin:0 8px}.cc-lang-btn{padding:8px 10px}}
`;
const GLOBE = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c2.6 2.7 3.9 5.7 3.9 9s-1.3 6.3-3.9 9c-2.6-2.7-3.9-5.7-3.9-9S9.4 5.7 12 3z"/></svg>';
const CHEVRON = '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>';

function split(path) {
  const m = path.match(PREFIX);
  if (m && LANGS.some(l => l[0] === m[1].toLowerCase())) {
    return { code: m[1].toLowerCase(), tag: m[1] + (m[2] || ''), rest: path.slice(m[0].length) || '/' };
  }
  return { code: null, tag: null, rest: path };
}

function remember(code) {
  try { localStorage.setItem('cc.lang', code); } catch (e) { /* storage blocked */ }
  document.cookie = 'cc_lang=' + code + '; Path=/; Max-Age=31536000; SameSite=Lax';
}

function detect() {
  const p = split(location.pathname);
  if (p.code) return { code: p.code, tag: p.tag, fromUrl: true };
  let saved = null;
  try { saved = localStorage.getItem('cc.lang'); } catch (e) { saved = null; }
  if (LANGS.some(l => l[0] === saved)) return { code: saved, tag: saved, fromUrl: false };
  const guess = String((navigator.languages && navigator.languages[0]) || navigator.language || 'en').slice(0, 2).toLowerCase();
  const code = LANGS.some(l => l[0] === guess) ? guess : 'en';
  return { code, tag: code, fromUrl: false };
}

export function t(src) {
  if (lang === 'en') return src;
  const body = src.trim();
  if (!body) return src;
  const key = body.split(' ').filter(Boolean).join(' ');
  const col = COLS.indexOf(lang);
  const row = dict.get(key);
  let out = row && row[col] ? row[col] : null;
  if (out === null) {
    for (const [re, tpl] of pats) {
      const hit = key.match(re);
      if (hit && tpl[col]) {
        out = tpl[col].split('$1').join(hit[1] || '').split('$2').join(hit[2] || '');
        break;
      }
    }
  }
  if (out === null) return src;
  const at = src.indexOf(body);
  return src.slice(0, at) + out + src.slice(at + body.length);
}

function text(node) {
  const parent = node.parentElement;
  if (!parent || parent.closest(SKIP)) return;
  let rec = nodes.get(node);
  if (!rec || node.nodeValue !== rec.out) {
    rec = { en: node.nodeValue, out: node.nodeValue };
    nodes.set(node, rec);
  }
  const out = t(rec.en);
  if (out !== node.nodeValue) node.nodeValue = out;
  rec.out = out;
}

function attributes(el) {
  if (el.closest('.cc-lang')) return;
  for (const name of ATTRS) {
    if (!el.hasAttribute(name)) continue;
    const value = el.getAttribute(name);
    let rec = attrs.get(el);
    if (!rec) { rec = {}; attrs.set(el, rec); }
    let r = rec[name];
    if (!r || value !== r.out) { r = { en: value, out: value }; rec[name] = r; }
    const out = t(r.en);
    if (out !== value) el.setAttribute(name, out);
    r.out = out;
  }
}

function links(root) {
  if (!root.querySelectorAll) return;
  const list = [...root.querySelectorAll('a[href^="/"]')];
  if (root.matches && root.matches('a[href^="/"]')) list.push(root);
  for (const a of list) {
    const href = a.getAttribute('href');
    if (href.startsWith('//')) continue;
    const m = href.match(/^([^?#]*)(.*)$/);
    const p = split(m[1]);
    if (!PAGE_LINKS.has(p.rest)) continue;
    const next = (lang === 'en' ? p.rest : '/' + lang + p.rest) + m[2];
    if (next !== href) a.setAttribute('href', next);
  }
}

function walk(root) {
  if (root.nodeType === 3) { text(root); return; }
  if (root.nodeType !== 1 || root.closest(SKIP)) return;
  attributes(root);
  links(root);
  const tree = document.createTreeWalker(root, 5);
  let n;
  while ((n = tree.nextNode())) {
    if (n.nodeType === 3) text(n);
    else if (!n.closest(SKIP)) attributes(n);
  }
}

function title() {
  if (!titleEn) titleEn = document.title;
  document.title = titleEn.split(' - ').map(part => t(part)).join(' - ');
}

function words(html) {
  const box = document.createElement('div');
  box.innerHTML = html;
  let n = 0;
  const go = node => {
    [...node.childNodes].forEach(c => {
      if (c.nodeType === 3) {
        const frag = document.createDocumentFragment();
        c.textContent.split(' ').forEach((part, k) => {
          if (k) frag.append(' ');
          if (!part) return;
          const w = document.createElement('b');
          w.className = 'w';
          w.setAttribute('aria-hidden', 'true');
          const i = document.createElement('i');
          i.textContent = part;
          i.style.setProperty('--i', n++);
          w.append(i);
          frag.append(w);
        });
        c.replaceWith(frag);
      } else if (c.nodeType === 1 && c.tagName !== 'BR') {
        go(c);
      }
    });
  };
  go(box);
  return box.innerHTML;
}

function headings() {
  const col = COLS.indexOf(lang);
  document.querySelectorAll('[data-split]').forEach((el, i) => {
    const row = heads[i];
    if (!row) return;
    if (lang === 'en' && !el.dataset.ccLang) return;
    const html = lang === 'en' ? row[0] : (row[col + 1] || row[0]);
    el.innerHTML = el.querySelector('b.w') ? words(html) : html;
    el.dataset.ccLang = lang;
    if (el.hasAttribute('aria-label')) el.setAttribute('aria-label', el.textContent.split(' ').filter(Boolean).join(' '));
  });
}

function refresh() {
  const current = LANGS.find(l => l[0] === lang) || LANGS[1];
  label.textContent = current[1];
  button.setAttribute('aria-label', t('Language') + ': ' + current[1]);
  [...menu.children].forEach(li => li.setAttribute('aria-selected', String(li.dataset.code === lang)));
}

function apply() {
  if (observer) observer.takeRecords();
  walk(document.body);
  title();
  headings();
  if (observer) observer.takeRecords();
}

function address(code) {
  const p = split(location.pathname);
  if (!PAGE_LINKS.has(p.rest) && p.rest !== '/stats' && p.rest !== '/luck' && p.rest !== '/claim') return;
  const path = code === 'en' ? p.rest : '/' + code + p.rest;
  if (path !== location.pathname) history.replaceState(null, '', path + location.search + location.hash);
}

function choose(code) {
  lang = code;
  htmlLang = code;
  remember(code);
  document.documentElement.lang = code;
  address(code);
  refresh();
  apply();
}

function build() {
  const style = document.createElement('style');
  style.textContent = CSS;
  document.head.append(style);
  picker = document.createElement('div');
  picker.className = 'cc-lang';
  button = document.createElement('button');
  button.type = 'button';
  button.className = 'cc-lang-btn';
  button.setAttribute('aria-haspopup', 'listbox');
  button.setAttribute('aria-expanded', 'false');
  button.innerHTML = GLOBE + '<span></span>' + CHEVRON;
  label = button.querySelector('span');
  menu = document.createElement('ul');
  menu.className = 'cc-lang-menu';
  menu.setAttribute('role', 'listbox');
  for (const [code, name] of LANGS) {
    const li = document.createElement('li');
    li.setAttribute('role', 'option');
    li.tabIndex = -1;
    li.dataset.code = code;
    li.lang = code;
    li.textContent = name;
    menu.append(li);
  }
  picker.append(button, menu);
  const open = on => {
    picker.classList.toggle('open', on);
    button.setAttribute('aria-expanded', String(on));
    if (on) (menu.querySelector('[aria-selected=true]') || menu.firstChild).focus();
  };
  button.addEventListener('click', () => open(!picker.classList.contains('open')));
  menu.addEventListener('click', e => {
    const li = e.target.closest('li');
    if (!li) return;
    choose(li.dataset.code);
    open(false);
    button.focus();
  });
  menu.addEventListener('keydown', e => {
    const items = [...menu.children];
    const i = items.indexOf(document.activeElement);
    if (e.key === 'ArrowDown') { e.preventDefault(); items[(i + 1) % items.length].focus(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); items[(i - 1 + items.length) % items.length].focus(); }
    else if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); document.activeElement.click(); }
    else if (e.key === 'Escape' || e.key === 'Tab') { open(false); button.focus(); }
  });
  document.addEventListener('click', e => { if (!picker.contains(e.target)) open(false); });
  const top = document.querySelector('header.top');
  const nav = document.querySelector('nav.nav');
  if (top) top.insertBefore(picker, top.querySelector('.download'));
  else if (nav) nav.append(picker);
  else { picker.classList.add('float'); document.body.append(picker); }
}

export function init() {
  if (picker) return;
  const found = detect();
  lang = found.code;
  htmlLang = found.tag;
  document.documentElement.lang = htmlLang;
  if (found.fromUrl) remember(lang);
  else if (lang !== 'en') address(lang);
  build();
  refresh();
  observer = new MutationObserver(list => {
    if (lang !== 'en') {
      for (const m of list) {
        if (m.type === 'characterData') text(m.target);
        else m.addedNodes.forEach(walk);
      }
    }
    observer.takeRecords();
  });
  observer.observe(document.body, { childList: true, characterData: true, subtree: true });
  apply();
}
