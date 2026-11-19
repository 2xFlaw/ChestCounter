import STR from './strings.js';

const LANGS = ['en', 'es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
const lang = (() => { const b = (chrome.i18n.getUILanguage() || 'en').toLowerCase().split(/[-_]/)[0]; return LANGS.includes(b) ? b : 'en'; })();
const fmt = (n) => Number(n || 0).toLocaleString(lang === 'en' ? 'en-GB' : lang);
function T(key, vars = {}) {
  let v = STR[lang]?.[key] ?? STR.en[key] ?? key;
  if (v && typeof v === 'object') { let f = 'other'; try { f = new Intl.PluralRules(lang).select(Number(vars.n || 0)); } catch (_) { /* ignore */ } v = v[f] ?? v.other ?? STR.en[key].other; }
  return String(v).replace(/\{(\w+)\}/g, (_, k) => (k in vars ? (typeof vars[k] === 'number' ? fmt(vars[k]) : vars[k]) : ''));
}
const $ = (id) => document.getElementById(id);
const SHOP = 'https://www.fortnite.com/item-shop';
const VERSION = chrome.runtime.getManifest().version;
document.documentElement.lang = lang;

$('title').textContent = T('pTitle');
$('ver').textContent = `v${VERSION}`;
$('remT').textContent = T('pReminder');
$('remS').textContent = T('pReminderSub');
$('quickT').textContent = T('pQuick');
$('foot').textContent = T('pFooter');

const store = await chrome.storage.local.get({ reminder: false, quick: false, stats: {} });
$('reminder').checked = store.reminder;
$('quick').checked = store.quick;
$('reminder').onchange = (e) => chrome.storage.local.set({ reminder: e.target.checked });
$('quick').onchange = (e) => chrome.storage.local.set({ quick: e.target.checked });

const isShop = (u) => { try { const x = new URL(u); return x.hostname === 'www.fortnite.com' && /^\/(?:[a-z]{2}(?:-[a-z]{2,4})?\/)?item-shop\/?$/i.test(x.pathname); } catch (_) { return false; } };
const tabs = await chrome.tabs.query({ url: 'https://www.fortnite.com/*' });
const [active] = await chrome.tabs.query({ active: true, currentWindow: true });
const tab = (active && isShop(active.url) ? active : null) || tabs.find((t) => isShop(t.url)) || null;

let st = null;
if (tab) { try { st = await chrome.tabs.sendMessage(tab.id, { type: 'cc:status' }); } catch (_) { st = null; } }

function lifetime(stats) {
  if (!stats || !stats.offers) return;
  $('life').hidden = false;
  $('life').textContent = T('pLifetime', { n: stats.offers, v: stats.vbucks || 0 });
}
lifetime(st?.stats?.offers ? st.stats : store.stats);

const go = $('go');
const openShop = (claim) => {
  if (tab) { chrome.tabs.update(tab.id, { active: true }); chrome.windows.update(tab.windowId, { focused: true }); }
  else chrome.tabs.create({ url: claim ? `${SHOP}?ccclaim=1` : SHOP });
};
if (!st || !st.ok) {
  $('line').textContent = T('pNoShop');
  go.textContent = T('pOpen');
  go.onclick = () => { openShop(false); window.close(); };
} else if (st.running) {
  $('line').textContent = T('pRunning');
  go.textContent = T('pRunning');
  go.disabled = true;
} else if (st.block) {
  $('line').textContent = st.block === 'signin' ? T('pSignin') : st.block === 'notactive' ? T('pNotActive') : T('pNoToken');
  go.textContent = T('pOpen');
  go.classList.remove('primary');
  go.onclick = () => { openShop(false); window.close(); };
} else if (st.free === 0) {
  $('line').textContent = T('pAllDone');
  go.textContent = T('pOpen');
  go.classList.remove('primary');
  go.onclick = () => { openShop(false); window.close(); };
} else {
  if (st.free != null) { $('big').hidden = false; $('big').textContent = fmt(st.free); $('line').textContent = T('pWaiting', { n: st.free }); }
  if (st.until) { $('until').hidden = false; $('until').textContent = T('pUntil', { d: st.until }); }
  go.textContent = T('pClaim');
  go.onclick = async () => {
    await chrome.tabs.update(tab.id, { active: true });
    chrome.windows.update(tab.windowId, { focused: true });
    chrome.tabs.sendMessage(tab.id, { type: 'cc:claim' });
    window.close();
  };
}

// a new version shows up here; unpacked extensions don't update on their own
try {
  const r = await fetch('https://raw.githubusercontent.com/2xFlaw/ChestCounter/main/claim-all/version.json', { cache: 'no-store' });
  if (r.ok) {
    const v = await r.json();
    const newer = (a, b) => { const x = a.split('.').map(Number), y = b.split('.').map(Number); for (let i = 0; i < 3; i++) { if ((x[i] || 0) !== (y[i] || 0)) return (x[i] || 0) > (y[i] || 0); } return false; };
    if (v.version && newer(v.version, VERSION)) {
      const a = $('upd');
      a.hidden = false;
      a.href = v.page || 'https://chestcounter.io/claim';
      a.textContent = `${T('pUpdate', { v: v.version })} · ${T('pUpdateGet')}`;
    }
  }
} catch (_) { /* offline */ }
