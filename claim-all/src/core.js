// Chest Counter Claim All: claims every free Item Shop offer while the Pirate Shop is active.
// Runs in the fortnite.com page. Every claim is sent with expectedTotalPrice 0, so Epic refuses
// anything that isn't actually free, and the run stops if the V-Bucks balance ever drops.
// Built on the approach of Napzeee's pirate-token-claim-all extension (MIT), see LICENSE.
/* global CC_STRINGS, CC_CSS, CC_LOGO, CC_VERSION, CC_PLATFORM */
(() => {
  if (window.__ccClaimAll) return;
  window.__ccClaimAll = true;

  const VERSION = typeof CC_VERSION === 'string' ? CC_VERSION : 'dev';
  const PLATFORM = typeof CC_PLATFORM === 'string' ? CC_PLATFORM : 'dev';
  const SHOP_RE = /^\/(?:[a-z]{2}(?:-[a-z]{2,4})?\/)?item-shop\/?$/i;
  const ROUTE = 'routes/item-shop._index';
  const BASE_CONCURRENCY = 4;
  const BALANCE_EVERY = 25;
  const SETTINGS_KEY = 'ccClaim.settings';
  const STATS_KEY = 'ccClaim.stats';
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const nativeFetch = window.fetch.bind(window);
  const reduceMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const DEMO = new URL(location.href).searchParams.has('ccdemo');

  // ---------- storage ----------
  const readJSON = (k, d) => { try { return Object.assign({}, d, JSON.parse(localStorage.getItem(k) || '{}')); } catch (_) { return { ...d }; } };
  const writeJSON = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch (_) { /* private mode */ } };
  const settings = () => readJSON(SETTINGS_KEY, { quick: false, lang: '' });
  const saveSettings = (patch) => writeJSON(SETTINGS_KEY, { ...settings(), ...patch });

  // ---------- language ----------
  const LANGS = ['en', 'es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
  function pickLang() {
    const want = [settings().lang, new URL(location.href).searchParams.get('lang'), document.documentElement.lang, navigator.language];
    for (const w of want) {
      const base = String(w || '').toLowerCase().split(/[-_]/)[0];
      if (LANGS.includes(base)) return base;
    }
    return 'en';
  }
  let lang = pickLang();
  const S = typeof CC_STRINGS === 'object' ? CC_STRINGS : { en: {} };
  const fmtN = (n) => Number(n || 0).toLocaleString(lang === 'en' ? 'en-GB' : lang);
  function T(key, vars = {}) {
    let v = (S[lang] && S[lang][key]) ?? S.en[key] ?? key;
    if (v && typeof v === 'object') {
      const n = Number(vars.n ?? 0);
      let form = 'other';
      try { form = new Intl.PluralRules(lang).select(n); } catch (_) { /* old browser */ }
      const en = S.en[key] && typeof S.en[key] === 'object' ? S.en[key] : {};
      v = v[form] ?? v.other ?? en[form] ?? en.other ?? key;
    }
    return String(v).replace(/\{(\w+)\}/g, (_, k) => (k in vars ? (typeof vars[k] === 'number' ? fmtN(vars[k]) : vars[k]) : `{${k}}`));
  }

  // ---------- Epic's turbo-stream (.data) decoder ----------
  function decodeTS(text) {
    const arr = JSON.parse(text);
    const SPECIAL = { '-1': undefined, '-2': null, '-3': NaN, '-4': Infinity, '-5': -Infinity, '-6': -0, '-7': undefined };
    const cache = new Map();
    const h = (i) => {
      if (typeof i !== 'number') return i;
      if (i < 0) return SPECIAL[i];
      if (cache.has(i)) return cache.get(i);
      const v = arr[i];
      if (Array.isArray(v)) {
        if (typeof v[0] === 'string') {
          if (v[0] === 'D') { const d = new Date(v[1]).toISOString(); cache.set(i, d); return d; }
          if (v[0] === 'Z') { const o = {}; cache.set(i, o); return o; }
          return v;
        }
        const out = []; cache.set(i, out);
        for (const x of v) out.push(h(x));
        return out;
      }
      if (v && typeof v === 'object') {
        const out = {}; cache.set(i, out);
        for (const k of Object.keys(v)) out[arr[+k.slice(1)]] = h(v[k]);
        return out;
      }
      return v;
    };
    return h(0);
  }

  // Epic rate-limits the shop data now and then, so back off and try again before giving up
  async function getData(url, tries = 4) {
    for (let a = 0; ; a++) {
      let r = null;
      try { r = await nativeFetch(url, { credentials: 'include' }); } catch (e) { if (a >= tries - 1) throw e; }
      if (r && r.ok) return decodeTS(await r.text());
      const retry = !r || r.status === 429 || r.status >= 500;
      if (!retry || a >= tries - 1) throw new Error(T('errHttp', { s: r ? r.status : 0 }));
      await sleep(1200 * 2 ** a + Math.random() * 400);
    }
  }

  function collectOffers(root) {
    const offers = new Map();
    const seen = new Set();
    (function walk(o, depth) {
      if (!o || depth > 16 || typeof o !== 'object' || seen.has(o)) return;
      seen.add(o);
      if (typeof o.offerId === 'string' && o.pricing) { if (!offers.has(o.offerId)) offers.set(o.offerId, o); return; }
      for (const k of Object.keys(o)) walk(o[k], depth + 1);
    })(root, 0);
    return [...offers.values()];
  }

  // demo mode makes every V-Bucks offer look free and never sends a claim
  const demoOwned = new Set();
  function demoShop(offers) {
    return offers.map((o) => {
      if (o.pricing?.currencyType !== 'MtxCurrency') return o;
      return { ...o, ownershipStatus: demoOwned.has(o.offerId) ? 'owned' : o.ownershipStatus, pricing: { ...o.pricing, finalPrice: 0 } };
    });
  }

  async function loadShop({ fallback = false } = {}) {
    let d;
    try { d = await getData(`/item-shop.data?_routes=${encodeURIComponent(ROUTE)}`); }
    catch (e) { const p = fallback && pageShop(); if (p) return p; throw e; }   // the copy already on the page is fine for a first look
    const data = d?.[ROUTE]?.data;
    if (!data) throw new Error(T('errShop'));
    const offers = collectOffers(data);
    if (DEMO) return { loggedIn: true, offers: demoShop(offers) };
    return { loggedIn: data.isLoggedIn !== false, offers };
  }

  async function getCsrf() {
    if (DEMO) return 'demo';
    const d = await getData('/item-shop/resources/csrf-token.data?_routes=routes%2Fitem-shop.resources.csrf-token');
    const t = d?.['routes/item-shop.resources.csrf-token']?.data?.csrfToken;
    if (!t) throw new Error(T('errCsrf'));
    return t;
  }

  async function getBalance() {
    if (DEMO) return 5100;
    try {
      const d = await getData('/item-shop/resources/get-profile.data?_routes=routes%2Fitem-shop.resources.get-profile');
      const b = d?.['routes/item-shop.resources.get-profile']?.data?.fortniteProfile?.vbucksWallet?.balance;
      return typeof b === 'number' ? b : null;
    } catch (_) { return null; }
  }

  // ---------- offer helpers ----------
  const isFree = (o) => o.pricing?.finalPrice === 0 && o.pricing?.currencyType === 'MtxCurrency';
  const isBundle = (o) => o.assetType === 'dynamicbundle' || /bundle/i.test(o.offerMainType || '');
  const owned = (o) => o.ownershipStatus === 'owned';
  const excluded = (o) => o.delilahExcluded === true;   // Epic's own "not in the Pirate Shop" flag
  const grants = (o) => o.ownershipCalculationData?.itemGrantTemplateIds || [];
  const isSidekick = (o) => o.assetType === 'companion' || grants(o).some((g) => /^CosmeticMimosa:/i.test(g));
  const hasToken = (offers) => {
    const vb = offers.filter((o) => o.pricing?.currencyType === 'MtxCurrency' && !excluded(o));
    return vb.length > 0 && vb.filter(isFree).length > vb.length / 2;
  };
  // same resized CDN Epic's own shop uses; the signed ucs-images links often fail under the page's COEP
  const thumb = (o) => {
    const im = Array.isArray(o.image) ? o.image[0] : o.image;
    const cosmo = im?.cosmo?.url || (/\/\/cosmo\.[^/]+\/v1\//.test(im?.xl || '') ? im.xl : '');
    if (cosmo) return cosmo.replace(/^(https:\/\/[^/]+)\/(v1\/)/, '$1/cdn-cgi/image/width=256,format=auto/$2');
    return im?.xs || im?.sm || im?.md || im?.tall || im?.wide || '';
  };
  const CATS = [
    ['bundles', (o) => isBundle(o)],
    ['outfits', (o) => o.assetType === 'outfit'],
    ['emotes', (o) => o.assetType === 'emote'],
    ['pickaxes', (o) => o.assetType === 'pickaxe'],
    ['backblings', (o) => o.assetType === 'backbling'],
    ['gliders', (o) => o.assetType === 'glider'],
    ['wraps', (o) => o.assetType === 'wrap'],
    ['contrails', (o) => o.assetType === 'contrail'],
    ['jam', (o) => o.assetType === 'jamtrack'],
    ['instruments', (o) => /^(guitar|bass|drums|keyboard|keytar|microphone)$/.test(o.assetType)],
    ['cars', (o) => /^(wheels|turbo|trail|body|carbody|skin|decal|vehicle)$/.test(o.assetType) || /^Vehicle/i.test(o.offerMainType || '')],
    ['kicks', (o) => o.assetType === 'shoes'],
    ['other', () => true],
  ];
  const catOf = (o) => CATS.find(([, f]) => f(o))[0];

  // value of a set of offers without counting items twice (bundles overlap with singles)
  function worth(list) {
    const singles = list.filter((o) => !isBundle(o));
    let total = singles.reduce((a, o) => a + (o.pricing?.basePrice || 0), 0);
    for (const b of list.filter(isBundle)) {
      const set = new Set(grants(b));
      const inside = singles.filter((s) => grants(s).length && grants(s).every((g) => set.has(g)))
        .reduce((a, s) => a + (s.pricing?.basePrice || 0), 0);
      total += Math.max(0, (b.pricing?.basePrice || 0) - inside);
    }
    return total;
  }

  // ---------- claiming ----------
  const pace = { concurrency: BASE_CONCURRENCY, cooldownUntil: 0 };
  async function claim(offer, csrf) {
    if (DEMO) {
      await sleep(180 + Math.random() * 420);
      demoOwned.add(offer.offerId);
      return { result: 'claimed', granted: Math.max(1, grants(offer).length) };
    }
    for (let attempt = 0; attempt < 5; attempt++) {
      const wait = pace.cooldownUntil - Date.now();
      if (wait > 0) await sleep(wait);
      let r, d = null;
      try {
        r = await nativeFetch('/item-shop/purchase.data', {
          method: 'POST',
          credentials: 'include',
          body: new URLSearchParams({ csrfToken: csrf.value, offerId: offer.offerId, expectedTotalPrice: '0' }),
        });
        d = decodeTS(await r.text())?.data;
      } catch (e) { d = { success: false, error: String(e) }; }
      if (d?.success) {
        const items = (d.data?.notifications || []).flatMap((n) => n.lootResult?.items || []);
        return { result: 'claimed', granted: items.length };
      }
      const err = String(d?.error || `HTTP ${r?.status}`).replace(/^HttpError:\s*/, '');
      if (/already own/i.test(err)) return { result: 'owned' };
      if (/csrf/i.test(err) && attempt === 0) { csrf.value = await getCsrf(); continue; }
      if (/throttl|too many|rate limit|timeout|temporar/i.test(err) || r?.status === 429 || r?.status >= 500) {
        pace.concurrency = Math.max(1, Math.ceil(pace.concurrency / 2));   // ease off for the rest of the run
        pace.cooldownUntil = Date.now() + 1500 * (attempt + 1);
        continue;
      }
      if (/price|cost|expected/i.test(err)) return { result: 'failed', why: T('rNoLongerFree') };
      return { result: 'failed', why: err.slice(0, 140) };
    }
    return { result: 'failed', why: T('rSlow') };
  }

  async function pool(items, worker) {
    let i = 0;
    const lanes = new Set();
    const lane = async () => {
      while (i < items.length && !state.stopped && !state.halt) {
        if (lanes.size > pace.concurrency) break;   // shrink when Epic asks us to slow down
        await worker(items[i++]);
      }
    };
    while (i < items.length && !state.stopped && !state.halt) {
      while (lanes.size < pace.concurrency && i < items.length) {
        const p = lane().finally(() => lanes.delete(p));
        lanes.add(p);
      }
      await Promise.race(lanes);
    }
    await Promise.all(lanes);
  }

  const state = { running: false, stopped: false, halt: null, haltBal: null, remaining: null };

  function saveStats(claimedOffers, granted, value) {
    const s = readJSON(STATS_KEY, { runs: 0, offers: 0, cosmetics: 0, vbucks: 0, last: '' });
    writeJSON(STATS_KEY, { runs: s.runs + 1, offers: s.offers + claimedOffers, cosmetics: s.cosmetics + granted, vbucks: s.vbucks + value, last: new Date().toISOString() });
  }

  // review screen first (unless quick claim is on), then claim what was picked
  async function run({ only = null, forceReview = false } = {}) {
    if (state.running) return;
    state.running = true; state.stopped = false; state.halt = null; state.haltBal = null;
    pace.concurrency = BASE_CONCURRENCY; pace.cooldownUntil = 0;
    syncHero();
    const ui = openModal();
    try {
      ui.status(T('reading'));
      const [shop, csrf0, startBal] = await Promise.all([loadShop({ fallback: true }), getCsrf().catch((e) => e), getBalance()]);
      if (!shop.loggedIn) { ui.notice('signin'); return; }
      if (!hasToken(shop.offers)) { ui.notice(plunderChip() ? 'notactive' : 'notoken'); return; }
      if (csrf0 instanceof Error) throw csrf0;
      const csrf = { value: csrf0 };

      const unowned = shop.offers.filter((o) => !owned(o));
      let free = unowned.filter((o) => isFree(o) && !excluded(o));
      if (only) free = free.filter((o) => only.has(o.offerId));
      const notCovered = unowned.filter((o) => !isFree(o) || excluded(o));
      if (!free.length) { ui.done({ nothing: true, notCovered, claimed: 0, viaBundle: 0, owned: 0, failed: [], granted: 0 }); state.remaining = 0; return; }

      let plan;
      if (only || (settings().quick && !forceReview)) {
        plan = { picked: free.filter((o) => !isSidekick(o)), sidekick: null, leftOut: free.filter(isSidekick) };
        if (only) plan = { picked: free.filter((o) => !isSidekick(o) || only.has(o.offerId)), sidekick: null, leftOut: [] };
      } else {
        state.running = false; syncHero();
        plan = await ui.review(free, { balance: startBal, until: tokenUntil() });
        if (!plan) { closeModal(); return; }
        state.running = true; syncHero();
      }
      const t0 = performance.now();
      const stats = { claimed: 0, viaBundle: 0, owned: 0, failed: [], granted: 0, claimedList: [], leftOut: plan.leftOut };
      const targets = [...(plan.sidekick ? [plan.sidekick] : []), ...plan.picked];
      if (!targets.length) { closeModal(); return; }
      ui.build(targets);

      let sinceCheck = 0;
      const checkBalance = async () => {
        if (startBal == null) return;
        const b = await getBalance();
        if (b != null && b < startBal) { state.halt = T('errSafety', { a: startBal, b }); state.haltBal = b; }
      };
      const count = () => ui.progress(stats.claimed + stats.viaBundle + stats.owned + stats.failed.length);
      const work = async (o) => {
        ui.tile(o.offerId, 'working');
        const r = await claim(o, csrf);
        if (r.result === 'claimed') { stats.claimed++; stats.granted += r.granted; stats.claimedList.push(o); ui.toast(o); }
        else if (r.result === 'owned') stats.owned++;
        else stats.failed.push({ o, why: r.why });
        ui.tile(o.offerId, r.result, r.why);
        count();
        if (++sinceCheck >= BALANCE_EVERY) { sinceCheck = 0; await checkBalance(); }
      };

      // 1. the sidekick they picked, on its own, so the one free sidekick goes where they want it
      if (plan.sidekick) {
        ui.status(T('stSidekick'));
        await work(plan.sidekick);
      }
      const bundles = plan.picked.filter(isBundle).sort((a, b) => (b.pricing.basePrice || 0) - (a.pricing.basePrice || 0));
      let singles = plan.picked.filter((o) => !isBundle(o));
      // 2. bundles first, biggest first, so most items arrive in a few claims
      if (bundles.length && !state.stopped && !state.halt) {
        ui.status(T('stBundles', { n: bundles.length }));
        await pool(bundles, work);
      }
      // 3. see what the bundles already covered, then grab the rest one by one
      if (singles.length && !state.stopped && !state.halt) {
        if (bundles.length || plan.sidekick) {
          ui.status(T('stChecking'));
          const [shop2, midBal] = await Promise.all([loadShop().catch(() => null), getBalance()]);
          if (startBal != null && midBal != null && midBal < startBal) { state.halt = T('errSafety', { a: startBal, b: midBal }); state.haltBal = midBal; }
          // if Epic won't send the shop right now, claim the singles anyway: ones a bundle covered just come back as owned
          const fresh = new Map((shop2 ? shop2.offers : []).map((o) => [o.offerId, o]));
          const left = [];
          for (const o of singles) {
            const f = fresh.get(o.offerId);
            if (f && owned(f)) { stats.viaBundle++; ui.tile(o.offerId, 'bundle'); }
            else if (f && !isFree(f)) { stats.failed.push({ o, why: isSidekick(o) ? T('rSidekickUsed') : T('rNoLongerFree') }); ui.tile(o.offerId, 'failed', T('rNoLongerFree')); }
            else left.push(o);
          }
          count();
          singles = left;
        }
        if (singles.length && !state.stopped && !state.halt) {
          ui.status(T(bundles.length ? 'stSingles' : 'stItems', { n: singles.length }));
          await pool(singles, work);
        }
      }

      const endBal = await getBalance();
      const halted = state.halt ? { a: startBal, b: endBal != null && endBal < startBal ? endBal : (state.haltBal ?? endBal) } : null;
      const done = stats.claimed + stats.viaBundle + stats.owned + stats.failed.length;
      state.remaining = stats.failed.length + (targets.length - done) + plan.leftOut.length;
      const value = worth(stats.claimedList);
      saveStats(stats.claimed, stats.granted, value);
      ui.done({
        ...stats, notCovered, stopped: state.stopped || !!halted, halted, value,
        seconds: (performance.now() - t0) / 1000,
        balance: { start: startBal, end: endBal },
        retry: halted ? new Set() : new Set([...stats.failed.map((f) => f.o.offerId), ...targets.slice(done).map((o) => o.offerId)]),
      });
    } catch (e) {
      ui.error(String(e.message || e));
    } finally {
      state.running = false;
      liveShop = null;
      syncHero();
      refreshLive();
    }
  }

  // ---------- page helpers ----------
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const ICON = {
    check: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.2 4.2L19 7" fill="none" stroke="currentColor" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    bang: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 6v8" stroke="currentColor" stroke-width="3.2" stroke-linecap="round"/><circle cx="12" cy="18.5" r="1.9" fill="currentColor"/></svg>',
    lock: '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10.5" width="14" height="10" rx="3" fill="currentColor"/><path d="M8.5 10.5V8a3.5 3.5 0 0 1 7 0v2.5" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"/></svg>',
    ask: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9.2 9.3a2.9 2.9 0 1 1 4.3 2.5c-.9.5-1.5 1.2-1.5 2.2v.6" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"/><circle cx="12" cy="18.6" r="1.7" fill="currentColor"/></svg>',
    clock: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8.5" fill="none" stroke="currentColor" stroke-width="2.4"/><path d="M12 7.5V12l3 2" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"/></svg>',
    shield: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l7 3v5.5c0 4.3-3 8-7 9.5-4-1.5-7-5.2-7-9.5V6z" fill="currentColor" opacity=".2"/><path d="M8.5 12.2l2.4 2.4 4.6-4.8" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    paw: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="7" cy="9" r="2.2" fill="currentColor"/><circle cx="12" cy="6.5" r="2.2" fill="currentColor"/><circle cx="17" cy="9" r="2.2" fill="currentColor"/><path d="M12 11.5c-3 0-5.5 3.2-5.5 5.6 0 1.7 1.4 2.4 2.8 2.4 1.1 0 1.7-.6 2.7-.6s1.6.6 2.7.6c1.4 0 2.8-.7 2.8-2.4 0-2.4-2.5-5.6-5.5-5.6z" fill="currentColor"/></svg>',
  };

  function pageShop() {
    try {
      const d = window.__reactRouterDataRouter?.state?.loaderData?.[ROUTE];
      if (!d) return null;
      const offers = collectOffers(d);
      return DEMO ? { loggedIn: true, offers: demoShop(offers) } : { loggedIn: d.isLoggedIn !== false, offers };
    } catch (_) { return null; }
  }

  // "Active until Nov 28, 12:46 PM (GMT+04:00)" next to the V-Bucks balance (English site only)
  function tokenUntil() {
    const el = [...document.querySelectorAll('button, span, div')].find((e) => e.children.length < 3 && /^Active until\s/i.test((e.textContent || '').trim()));
    if (!el) return '';
    const m = el.textContent.trim().match(/^Active until\s+(.+)$/i);
    return m ? m[1].trim().slice(0, 48) : '';
  }
  function tokenUntilDate(txt) {
    if (!txt) return null;
    const clean = txt.replace(/\(GMT([+-]\d{2}):?(\d{2})\)/i, 'GMT$1$2').replace(',', '');
    const y = new Date().getFullYear();
    for (const yy of [y, y + 1]) {
      const d = new Date(clean.replace(/^([A-Za-z]+\s+\d+)/, `$1 ${yy}`));
      if (!isNaN(d) && d.getTime() > Date.now() - 864e5) return d.toISOString();
    }
    return null;
  }
  // the "Plunder the Shop!" prompt shows when the token is owned but not activated yet
  const plunderChip = () => [...document.querySelectorAll('button, a, span')].some((e) => e.children.length < 4 && /plunder the shop/i.test(e.textContent || ''));

  function tileHTML(o) {
    const src = thumb(o);
    return src ? `<img alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer" src="${esc(src)}">` : `<span class="cc-tname">${esc(o.title)}</span>`;
  }

  function countTo(el, to, dur = 450) {
    const from = Number(String(el.dataset.v || 0));
    el.dataset.v = to;
    if (reduceMotion || to === from) { el.textContent = fmtN(to); return; }
    const t0 = performance.now();
    (function step(t) {
      const p = Math.min(1, (t - t0) / dur);
      const e = 1 - Math.pow(1 - p, 3);
      el.textContent = fmtN(Math.round(from + (to - from) * e));
      if (p < 1) requestAnimationFrame(step);
    })(t0);
  }

  // ---------- styles ----------
  function ensureStyle() {
    if (typeof CC_CSS !== 'string' || document.getElementById('cc-claim-style')) return;
    const st = document.createElement('style');
    st.id = 'cc-claim-style';
    st.textContent = CC_CSS;
    (document.head || document.documentElement).appendChild(st);
  }
  function setLogo() {
    if (typeof CC_LOGO === 'string') document.documentElement.style.setProperty('--cc-logo', `url("${CC_LOGO}")`);
  }

  // ---------- hero button + floating button ----------
  let hero = null, heroWrap = null, fab = null, io = null;
  let liveShop = null, liveFetching = false;
  let liveAt = 0;
  async function refreshLive(force = true) {
    if (liveFetching || (!force && Date.now() - liveAt < 60e3)) return;   // React can drop the header often
    liveFetching = true;
    liveAt = Date.now();
    try { liveShop = await loadShop(); state.remaining = null; } catch (_) { /* ignore */ }
    finally { liveFetching = false; if (hero) hero.dataset.key = ''; updateHero(); }
  }

  function heroData() {
    const shop = liveShop || pageShop();
    if (!shop) return { n: state.remaining, block: '' };
    if (!shop.loggedIn) return { n: null, block: 'signin' };
    if (!hasToken(shop.offers)) return { n: null, block: plunderChip() ? 'notactive' : 'notoken' };
    const free = shop.offers.filter((o) => !owned(o) && isFree(o) && !excluded(o));
    const n = state.remaining != null ? Math.min(state.remaining, free.length) : free.length;
    return { n, block: '', value: worth(free) };
  }

  function buildHero() {
    const el = document.createElement('button');
    el.id = 'cc-hero';
    el.type = 'button';
    el.innerHTML = '<span class="cc-hb-logo cc-logo"></span><span class="cc-hb-label"></span><span class="cc-hb-n"></span>';
    el.addEventListener('click', () => { if (!state.running && !root) run(); });
    return el;
  }

  function publish(d) {   // read by the extension popup and reminder
    const ds = document.documentElement.dataset;
    const until = tokenUntil();
    ds.ccFree = d.n == null ? '' : String(d.n);
    ds.ccValue = d.value ? String(d.value) : '';
    ds.ccUntil = until;
    ds.ccUntilIso = tokenUntilDate(until) || '';
    ds.ccRunning = state.running ? '1' : '';
    ds.ccBlock = d.block || '';
    ds.ccStats = JSON.stringify(readJSON(STATS_KEY, {}));
    ds.ccVersion = VERSION;
  }

  function updateHero() {
    const d = heroData();
    const { n, block } = d;
    publish(d);
    if (fab) {
      const fn = fab.querySelector('.cc-fab-n');
      fn.textContent = n ? fmtN(n) : '';
      fn.hidden = !n;
      fab.querySelector('.cc-fab-tip').textContent = T('claimAll');
      fab.setAttribute('aria-label', T('claimAll'));
    }
    if (!hero) return;
    const key = `${n}|${state.running}|${block}|${lang}`;
    if (hero.dataset.key === key) return;
    const changed = hero.dataset.key && hero.dataset.key.split('|')[0] !== String(n);
    hero.dataset.key = key;
    const label = hero.querySelector('.cc-hb-label');
    const badge = hero.querySelector('.cc-hb-n');
    hero.classList.toggle('is-running', state.running);
    hero.classList.toggle('is-done', !state.running && n === 0);
    hero.classList.toggle('is-blocked', !!block);
    if (state.running) { label.textContent = T('claiming'); badge.hidden = true; }
    else if (n === 0) { label.textContent = T('allClaimed'); badge.hidden = false; badge.innerHTML = ICON.check; }
    else { label.textContent = T('claimAll'); badge.hidden = n == null; badge.textContent = n == null ? '' : fmtN(n); }
    hero.title = block === 'signin' ? T('tipSignin')
      : block ? T('tipNoToken')
      : n ? T('tipReady', { n }) : n === 0 ? T('tipNone') : T('claimAll');
    if (changed && !reduceMotion) badge.animate([{ transform: 'scale(.4)' }, { transform: 'scale(1.25)' }, { transform: 'scale(1)' }], { duration: 450, easing: 'cubic-bezier(.34,1.56,.64,1)' });
  }

  // the Buy V-Bucks link sits next to the Item Shop heading in every language
  function findRow() {
    const buy = [...document.querySelectorAll('a[href*="/item-shop/v-bucks"]')].find((a) => /\/item-shop\/v-bucks\/?(?:[?#]|$)/.test(a.getAttribute('href') || ''));   // keeps ?lang= on it
    const h1 = document.querySelector('main h1') || document.querySelector('h1');
    if (buy && h1 && buy.parentElement === h1.parentElement) return { row: h1.parentElement, buy, h1 };
    if (h1) return { row: h1.parentElement, buy: [...h1.parentElement.children].find((c) => c !== h1 && c !== heroWrap && /v-?bucks/i.test(c.textContent || '')) || null, h1 };
    return null;
  }

  function mountHero() {
    if (!SHOP_RE.test(location.pathname)) { heroWrap?.remove(); setFab(false); return; }
    ensureStyle();
    const lNow = pickLang();
    if (lNow !== lang) { lang = lNow; if (hero) hero.dataset.key = ''; }
    const found = findRow();
    if (!found) return;
    const { row, buy, h1 } = found;
    if (!hero) {
      heroWrap = document.createElement('div');
      heroWrap.id = 'cc-hero-wrap';
      hero = buildHero();
      heroWrap.appendChild(hero);
      io = new IntersectionObserver(([e]) => setFab(!e.isIntersecting && e.boundingClientRect.top < 0), { threshold: 0 });
      const cs = getComputedStyle(h1);   // Epic's heading font is variable, so copy its width and weight too
      if (cs.fontFamily) document.documentElement.style.setProperty('--cc-head', cs.fontFamily);
      if (cs.fontVariationSettings && cs.fontVariationSettings !== 'normal') document.documentElement.style.setProperty('--cc-head-fvs', cs.fontVariationSettings);
    }
    if (!heroWrap.isConnected && !state.running) refreshLive(false);
    if (heroWrap.parentElement !== row || (buy && heroWrap.nextElementSibling !== buy)) {
      row.insertBefore(heroWrap, buy || null);
      io.disconnect(); io.observe(heroWrap);
    }
    updateHero();
  }

  function setFab(on) {
    if (on && (state.running || root || !SHOP_RE.test(location.pathname) || heroData().n === 0)) on = false;
    if (on && !fab) {
      fab = document.createElement('button');
      fab.id = 'cc-fab';
      fab.type = 'button';
      fab.innerHTML = '<span class="cc-logo"></span><span class="cc-fab-n" hidden></span><span class="cc-fab-tip"></span>';
      fab.addEventListener('click', () => { if (!state.running && !root) run(); });
      document.body.appendChild(fab);
      updateHero();
    }
    if (fab) requestAnimationFrame(() => fab.classList.toggle('is-on', on));
  }

  function syncHero() { if (hero) hero.dataset.key = ''; mountHero(); setFab(false); }

  // ---------- modal ----------
  let root = null;

  function openModal() {
    root?.remove();
    ensureStyle();
    root = document.createElement('div');
    root.id = 'cc-root';
    root.lang = lang;
    root.innerHTML = `
      <div class="cc-scrim"></div>
      <div class="cc-modal" role="dialog" aria-modal="true" aria-labelledby="cc-m-title">
        <div class="cc-m-top">
          <span class="cc-blob b1"></span><span class="cc-blob b2"></span><span class="cc-blob b3"></span>
          ${DEMO ? `<div class="cc-demo">${esc(T('demoBadge'))}</div>` : ''}
          <div class="cc-m-logo"><span class="cc-logo"></span></div>
          <div class="cc-m-count" hidden><b class="cc-n">0</b><span class="cc-total"></span></div>
          <div class="cc-m-title" id="cc-m-title">${esc(T('brand'))}</div>
          <div class="cc-status" aria-live="polite"></div>
          <div class="cc-bar" hidden><i></i></div>
          <div class="cc-toast-row"></div>
          <div class="cc-summary"></div>
        </div>
        <div class="cc-body" hidden></div>
        <div class="cc-grid" hidden></div>
        <div class="cc-m-foot">
          <button type="button" class="cc-btn cc-stop">${esc(T('stop'))} <kbd>Esc</kbd></button>
        </div>
      </div>
      <canvas class="cc-confetti"></canvas>`;
    document.body.appendChild(root);
    setFab(false);
    const prevOverflow = document.documentElement.style.overflow;
    document.documentElement.style.overflow = 'hidden';
    root._restore = () => { document.documentElement.style.overflow = prevOverflow; };
    requestAnimationFrame(() => requestAnimationFrame(() => root && root.classList.add('is-open')));

    const me = root;
    const $ = (s) => me.querySelector(s);
    const tiles = new Map();
    let total = 0;
    const stopBtn = $('.cc-stop');
    stopBtn.onclick = () => { state.stopped = true; ui.status(T('stStopping')); stopBtn.disabled = true; };
    stopBtn.focus({ preventScroll: true });
    const setTitle = (t) => { $('.cc-m-title').textContent = t; };

    const ui = {
      status(text) { $('.cc-status').textContent = text; },

      // pick categories and the one free sidekick, resolves with the plan or null
      review(free, info) {
        return new Promise((resolve) => {
          me.classList.add('is-review');
          const sidekicks = free.filter(isSidekick);
          const normal = free.filter((o) => !isSidekick(o));
          const groups = CATS.map(([k]) => [k, normal.filter((o) => catOf(o) === k)]).filter(([, l]) => l.length);
          const on = new Set(groups.map(([k]) => k));
          let pick = sidekicks.length === 1 ? sidekicks[0].offerId : '';
          setTitle(T('reviewTitle', { n: free.length }));
          const meta = [];
          meta.push(T('reviewWorth', { v: worth(free) }));
          if (info.until) meta.push(T('reviewUntil', { d: info.until }));
          ui.status(meta.join(' · '));
          const body = $('.cc-body');
          body.hidden = false;
          const sk = sidekicks.length ? `
            <fieldset class="cc-sk">
              <legend><span class="cc-ic">${ICON.paw}</span>${esc(T('skTitle'))}</legend>
              <p>${esc(T('skText'))}</p>
              <div class="cc-sk-list">
                ${sidekicks.map((o) => `
                  <label class="cc-sk-opt" style="--c1:${esc(o.color1 || '#3a2140')};--c2:${esc(o.color2 || '#1b0f20')}">
                    <input type="radio" name="cc-sk" value="${esc(o.offerId)}"${pick === o.offerId ? ' checked' : ''}>
                    <span class="cc-sk-img">${tileHTML(o)}</span>
                    <span class="cc-sk-t"><b>${esc(o.title)}</b><small>${esc(isBundle(o) ? T('skBundle') : T('skSingle'))}</small></span>
                  </label>`).join('')}
                <label class="cc-sk-opt cc-sk-none">
                  <input type="radio" name="cc-sk" value=""${pick ? '' : ' checked'}>
                  <span class="cc-sk-img">${ICON.clock}</span>
                  <span class="cc-sk-t"><b>${esc(T('skNone'))}</b><small>${esc(T('skNoneSub'))}</small></span>
                </label>
              </div>
            </fieldset>` : '';
          body.innerHTML = `
            <div class="cc-sec-h"><b>${esc(T('reviewPick'))}</b>
              <span><button type="button" class="cc-link" data-all="1">${esc(T('selectAll'))}</button> · <button type="button" class="cc-link" data-all="0">${esc(T('selectNone'))}</button></span></div>
            <div class="cc-cats">
              ${groups.map(([k, l]) => `<button type="button" class="cc-cat is-on" data-k="${k}" aria-pressed="true">
                <span class="cc-cat-imgs">${l.slice(0, 3).map(tileHTML).join('')}</span>
                <span class="cc-cat-t">${esc(T('cat_' + k))}</span><b>${fmtN(l.length)}</b></button>`).join('')}
            </div>
            ${sk}
            <p class="cc-safe"><span class="cc-ic">${ICON.shield}</span>${esc(T('safety'))}${info.balance != null ? ` <b>${esc(T('balanceNow', { b: info.balance }))}</b>` : ''}</p>
            <label class="cc-quick"><input type="checkbox"${settings().quick ? ' checked' : ''}> ${esc(T('quickToggle'))}</label>`;
          const foot = $('.cc-m-foot');
          foot.innerHTML = `<button type="button" class="cc-btn cc-cancel">${esc(T('cancel'))}</button><button type="button" class="cc-btn cc-primary cc-go"></button>`;
          const go = foot.querySelector('.cc-go');
          const chosen = () => {
            const picked = normal.filter((o) => on.has(catOf(o)));
            const skOffer = sidekicks.find((o) => o.offerId === pick) || null;
            return { picked, sidekick: skOffer, leftOut: [...normal.filter((o) => !on.has(catOf(o))), ...sidekicks.filter((o) => o !== skOffer)] };
          };
          const refresh = () => {
            const c = chosen();
            const n = c.picked.length + (c.sidekick ? 1 : 0);
            go.textContent = T('start', { n });
            go.disabled = n === 0;
            body.querySelectorAll('.cc-cat').forEach((b) => { const v = on.has(b.dataset.k); b.classList.toggle('is-on', v); b.setAttribute('aria-pressed', String(v)); });
          };
          body.querySelectorAll('.cc-cat').forEach((b) => { b.onclick = () => { on.has(b.dataset.k) ? on.delete(b.dataset.k) : on.add(b.dataset.k); refresh(); }; });
          body.querySelectorAll('[data-all]').forEach((b) => { b.onclick = () => { if (b.dataset.all === '1') groups.forEach(([k]) => on.add(k)); else on.clear(); refresh(); }; });
          body.querySelectorAll('input[name="cc-sk"]').forEach((r) => { r.onchange = () => { pick = r.value; refresh(); }; });
          body.querySelector('.cc-quick input').onchange = (e) => saveSettings({ quick: e.target.checked });
          let settled = false;
          const finish = (v) => {
            if (settled) return;
            settled = true;
            me._cancelReview = null;
            me.classList.remove('is-review');
            body.hidden = true; body.innerHTML = '';
            foot.innerHTML = `<button type="button" class="cc-btn cc-stop">${esc(T('stop'))} <kbd>Esc</kbd></button>`;
            const s = foot.querySelector('.cc-stop');
            s.onclick = () => { state.stopped = true; ui.status(T('stStopping')); s.disabled = true; };
            resolve(v);
          };
          me._cancelReview = () => finish(null);
          foot.querySelector('.cc-cancel').onclick = () => finish(null);
          go.onclick = () => finish(chosen());
          refresh();
          go.focus({ preventScroll: true });
        });
      },

      build(offers) {
        total = offers.length;
        setTitle(T('brand'));
        $('.cc-m-count').hidden = false;
        $('.cc-bar').hidden = false;
        $('.cc-total').textContent = `/ ${fmtN(total)}`;
        const grid = $('.cc-grid');
        grid.hidden = false;
        grid.innerHTML = '';
        offers.forEach((o, i) => {
          const t = document.createElement('div');
          t.className = 'cc-tile' + (isBundle(o) ? ' is-bundle' : '') + (isSidekick(o) ? ' is-sk' : '');
          t.style.setProperty('--c1', o.color1 || '#4a2a52');
          t.style.setProperty('--c2', o.color2 || '#21122a');
          t.style.setProperty('--d', `${Math.min(i, 50) * 18}ms`);
          t.title = o.title;
          t.innerHTML = `${tileHTML(o)}${isBundle(o) ? `<span class="cc-tag">${esc(T('tagBundle'))}</span>` : ''}<span class="cc-badge"></span>`;
          grid.appendChild(t);
          tiles.set(o.offerId, t);
        });
      },
      tile(id, status, why) {
        const t = tiles.get(id);
        if (!t) return;
        t.classList.remove('s-working');
        const s = status === 'owned' ? 'bundle' : status;
        t.classList.add('s-' + s);
        const badge = t.querySelector('.cc-badge');
        if (s === 'claimed' || s === 'bundle') badge.innerHTML = ICON.check;
        if (s === 'failed') badge.innerHTML = ICON.bang;
        if (why) t.title += `: ${why}`;
        if (s === 'working') t.scrollIntoView?.({ block: 'nearest', behavior: reduceMotion ? 'auto' : 'smooth' });
        if (s === 'claimed' && !reduceMotion) {
          t.animate([{ transform: 'scale(.82)' }, { transform: 'scale(1.14)' }, { transform: 'scale(1)' }], { duration: 520, easing: 'cubic-bezier(.34,1.56,.64,1)' });
          sparkle(t);
        }
      },
      progress(done) {
        countTo($('.cc-n'), done);
        $('.cc-bar i').style.width = `${total ? (done / total) * 100 : 0}%`;
      },
      toast(o) {
        $('.cc-toast-row').innerHTML = `<span class="cc-toast"><span class="cc-toast-img" style="--c1:${esc(o.color1 || '#ff4fa3')};--c2:${esc(o.color2 || '#7a0a4c')}">${tileHTML(o)}</span><span class="cc-toast-t">${esc(o.title)}</span><span class="cc-toast-ok">${ICON.check}</span></span>`;
      },
      done(s) {
        me.classList.add('is-done');
        $('.cc-toast-row').innerHTML = '';
        $('.cc-bar').hidden = true;
        const got = s.claimed + s.viaBundle + s.owned;
        if (s.nothing) {
          $('.cc-m-count').hidden = true;
          setTitle(T('nothingTitle'));
          ui.status(T('nothingText'));
        } else {
          countTo($('.cc-n'), got, 300);
          const secs = s.seconds < 60 ? T('secs', { s: s.seconds.toFixed(1) }) : T('mins', { m: Math.floor(s.seconds / 60), s: Math.round(s.seconds % 60) });
          setTitle(s.halted ? T('safetyTitle') : s.stopped ? T('stoppedTitle') : got ? T('doneTitle') : T('nothingClaimed'));
          ui.status(s.halted ? T('safetyText', { a: s.halted.a, b: s.halted.b }) : s.stopped ? T('stoppedText', { n: got }) : T('doneText', { n: s.granted + s.viaBundle, t: secs }));
          if (s.halted) me.classList.add('is-halted');
        }
        const bal = s.balance && s.balance.start != null && s.balance.end != null
          ? (s.balance.end < s.balance.start
            ? `<span class="cc-chip cc-chip-bad">${ICON.bang} ${esc(T('chipVbBad', { a: s.balance.start, b: s.balance.end }))}</span>`
            : `<span class="cc-chip cc-chip-ok">${ICON.shield} ${esc(T('chipVbOk', { b: s.balance.end }))}</span>`) : '';
        const chip = (cls, n, key) => n ? `<span class="cc-chip ${cls}"><i></i>${esc(T(key, { n }))}</span>` : '';
        const chips = [
          bal,
          s.value ? `<span class="cc-chip cc-c-worth"><i></i>${esc(T('chipWorth', { v: s.value }))}</span>` : '',
          chip('cc-c-claimed', s.claimed, 'chipClaimed'),
          chip('cc-c-bundle', s.viaBundle + s.owned, 'chipBundle'),
          chip('cc-c-failed', s.failed?.length, 'chipSkipped'),
          chip('cc-c-left', s.leftOut?.length, 'chipLeftOut'),
        ].join('');
        const li = (title, why) => `<li><b>${esc(title)}</b><span>${esc(why)}</span></li>`;
        const failed = s.failed?.length ? `<details class="cc-more"><summary>${esc(T('whySkipped'))}</summary><ul>${s.failed.map((f) => li(f.o.title, f.why)).join('')}</ul></details>` : '';
        const left = s.leftOut?.length ? `<details class="cc-more"><summary>${esc(T('leftOutList'))}</summary><ul>${s.leftOut.map((o) => li(o.title, isSidekick(o) ? T('rSidekick') : T('cat_' + catOf(o)))).join('')}</ul></details>` : '';
        const nc = s.notCovered?.length ? `<details class="cc-more"><summary>${esc(T('notCovered', { n: s.notCovered.length }))}</summary><ul>${s.notCovered.map((o) =>
          li(o.title, o.pricing?.currencyType === 'RealMoney' ? T('rmPack') : excluded(o) ? T('rExcluded') : T('vbPrice', { v: o.pricing?.finalPrice ?? 0 }))).join('')}</ul></details>` : '';
        $('.cc-summary').innerHTML = `<div class="cc-chips">${chips}</div>${failed}${left}${nc}`;
        const foot = $('.cc-m-foot');
        const again = s.retry && s.retry.size;
        foot.innerHTML = `${s.claimedList?.length ? `<button type="button" class="cc-btn cc-save">${esc(T('saveList'))}</button>` : ''}
          ${again ? `<button type="button" class="cc-btn cc-again">${esc(T('retry', { n: s.retry.size }))}</button>` : ''}
          ${s.leftOut?.length && !again && !s.halted ? `<button type="button" class="cc-btn cc-again2">${esc(T('reviewAgain'))}</button>` : ''}
          <button type="button" class="cc-btn cc-primary cc-finish">${esc(T('done'))}</button>`;
        foot.querySelector('.cc-save')?.addEventListener('click', () => saveList(s));
        foot.querySelector('.cc-again')?.addEventListener('click', () => { closeModal(); run({ only: s.retry }); });
        foot.querySelector('.cc-again2')?.addEventListener('click', () => { closeModal(); run({ forceReview: true }); });
        foot.querySelector('.cc-finish').onclick = () => (DEMO || s.nothing ? closeModal() : location.reload());
        foot.querySelector('.cc-finish').focus({ preventScroll: true });
        if (!s.stopped && got > 0) confetti(me.querySelector('.cc-confetti'), $('.cc-m-logo'));
      },
      error(msg) {
        me.classList.add('is-done', 'is-error');
        me.classList.remove('is-review');
        $('.cc-bar').hidden = true;
        $('.cc-toast-row').innerHTML = '';
        setTitle(T('snagTitle'));
        ui.status(T('snagText', { msg: msg.replace(/[.\s]+$/, '') }));
        const foot = $('.cc-m-foot');
        foot.innerHTML = `<button type="button" class="cc-btn">${esc(T('close'))}</button><button type="button" class="cc-btn cc-primary">${esc(T('tryAgain'))}</button>`;
        foot.children[0].onclick = closeModal;
        foot.children[1].onclick = () => { closeModal(); run(); };
        foot.children[1].focus({ preventScroll: true });
      },
      notice(kind) {
        const n = kind === 'signin' ? { icon: ICON.lock, title: T('signinTitle'), text: T('signinText') }
          : kind === 'notactive' ? { icon: ICON.clock, title: T('notActiveTitle'), text: T('notActiveText') }
          : { icon: ICON.ask, title: T('noTokenTitle'), text: T('noTokenText') };
        me.classList.add('is-done', 'is-notice', `is-${kind}`);
        $('.cc-bar').hidden = true;
        $('.cc-toast-row').innerHTML = '';
        $('.cc-m-logo').insertAdjacentHTML('beforeend', `<span class="cc-m-tag">${n.icon}</span>`);
        setTitle(n.title);
        ui.status(n.text);
        const foot = $('.cc-m-foot');
        foot.innerHTML = kind === 'signin'
          ? `<button type="button" class="cc-btn">${esc(T('close'))}</button><button type="button" class="cc-btn cc-primary">${esc(T('signin'))}</button>`
          : `<button type="button" class="cc-btn cc-primary">${esc(T('gotIt'))}</button>`;
        foot.firstElementChild.onclick = closeModal;
        if (kind === 'signin') foot.lastElementChild.onclick = signIn;
        foot.lastElementChild.focus({ preventScroll: true });
      },
    };
    return ui;
  }

  function saveList(s) {
    const day = new Date().toISOString().slice(0, 10);
    const lines = [T('listHeader', { d: day }), ''];
    for (const o of s.claimedList) lines.push(`+ ${o.title}`);
    if (s.failed.length) { lines.push('', T('whySkipped')); for (const f of s.failed) lines.push(`- ${f.o.title}: ${f.why}`); }
    lines.push('', `Chest Counter Claim All ${VERSION} · https://chestcounter.io/claim`);
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([lines.join('\r\n')], { type: 'text/plain;charset=utf-8' }));
    a.download = `pirate-shop-claims-${day}.txt`;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 4000);
  }

  // Epic's sign-in link lives inside their nav's shadow DOM
  function signIn() {
    const a = document.querySelector('fortnite-navigation')?.shadowRoot?.querySelector('a[href*="/login"]');
    if (a) a.click(); else location.href = '/item-shop/login';
  }

  function closeModal() {
    if (!root) return;
    const r = root; root = null;
    r._cancelReview?.();
    r._restore?.();
    r.classList.remove('is-open');
    setTimeout(() => r.remove(), 300);
    syncHero();
  }

  function sparkle(tile) {
    const colors = ['#ff4fa3', '#ffb000', '#3ddc97', '#7cd4ff'];
    for (let i = 0; i < 7; i++) {
      const s = document.createElement('i');
      s.className = 'cc-spark';
      const a = (i / 7) * Math.PI * 2 + Math.random() * .5;
      const d = 26 + Math.random() * 16;
      s.style.cssText = `--x:${Math.cos(a) * d}px;--y:${Math.sin(a) * d}px;background:${colors[i % colors.length]}`;
      tile.appendChild(s);
      setTimeout(() => s.remove(), 700);
    }
  }

  function confetti(canvas, fromEl) {
    if (reduceMotion || !canvas) return;
    const ctx = canvas.getContext('2d');
    const dpr = Math.min(2, devicePixelRatio || 1);
    const W = canvas.width = innerWidth * dpr, H = canvas.height = innerHeight * dpr;
    const rc = fromEl?.getBoundingClientRect();
    const ox = rc ? (rc.left + rc.width / 2) * dpr : W / 2, oy = rc ? (rc.top + rc.height / 2) * dpr : H / 3;
    const colors = ['#ff4fa3', '#d00080', '#ffb000', '#ffd23f', '#3ddc97', '#7cd4ff'];
    const parts = Array.from({ length: 150 }, (_, i) => {
      const a = Math.random() * Math.PI * 2, sp = (Math.random() * 14 + 6) * dpr;
      return { x: ox, y: oy, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp - 8 * dpr, r: (Math.random() * 5 + 3.5) * dpr,
        rot: Math.random() * 6, vr: (Math.random() - .5) * .3, kind: i % 5 === 0 ? 'coin' : i % 3 === 0 ? 'ring' : 'dot', c: colors[i % colors.length] };
    });
    const t0 = performance.now();
    (function frame(t) {
      ctx.clearRect(0, 0, W, H);
      const life = (t - t0) / 2600;
      for (const p of parts) {
        p.vy += .38 * dpr; p.vx *= .985; p.vy *= .985; p.x += p.vx; p.y += p.vy; p.rot += p.vr;
        ctx.globalAlpha = Math.max(0, 1 - life * life);
        ctx.save(); ctx.translate(p.x, p.y); ctx.rotate(p.rot);
        if (p.kind === 'coin') {
          ctx.scale(Math.abs(Math.cos(p.rot * 2)) * .85 + .15, 1);
          ctx.fillStyle = '#ffb000'; ctx.beginPath(); ctx.arc(0, 0, p.r * 1.3, 0, 7); ctx.fill();
          ctx.fillStyle = '#ffe07a'; ctx.beginPath(); ctx.arc(0, 0, p.r * .7, 0, 7); ctx.fill();
        } else if (p.kind === 'ring') {
          ctx.strokeStyle = p.c; ctx.lineWidth = 2.2 * dpr; ctx.beginPath(); ctx.arc(0, 0, p.r, 0, 7); ctx.stroke();
        } else {
          ctx.fillStyle = p.c; ctx.beginPath(); ctx.arc(0, 0, p.r, 0, 7); ctx.fill();
        }
        ctx.restore();
      }
      if (life < 1) requestAnimationFrame(frame); else ctx.clearRect(0, 0, W, H);
    })(t0);
  }

  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape' || !root) return;
    e.stopPropagation();
    if (root.classList.contains('is-review')) closeModal();
    else if (state.running) root.querySelector('.cc-stop')?.click();
    else if (root.classList.contains('is-done')) closeModal();
  }, true);

  // hide broken thumbnails (Epic's image links expire after about a day) and keep the tile colour
  document.addEventListener('error', (e) => {
    const t = e.target;
    if (t && t.tagName === 'IMG' && t.closest && t.closest('#cc-root')) t.classList.add('is-broken');
  }, true);

  // from the extension popup (via the bridge)
  document.addEventListener('cc:claim', () => { if (!state.running && !root) run(); });
  document.addEventListener('cc:refresh', () => { lang = pickLang(); syncHero(); });

  // the popup opens the shop with ?ccclaim=1
  (async () => {
    const url = new URL(location.href);
    if (!url.searchParams.has('ccclaim')) return;
    url.searchParams.delete('ccclaim');
    history.replaceState(history.state, '', url.pathname + url.search + url.hash);
    for (let i = 0; i < 40 && !window.__reactRouterDataRouter; i++) await sleep(250);
    await sleep(900);
    if (!state.running) run();
  })();

  setLogo();
  mountHero();
  setInterval(mountHero, 800);   // React re-renders the header and drops the button
})();
