// Service worker: toolbar badge, daily "shop refreshed" reminder while the Pirate Shop is active.
import STR from './strings.js';

const LANGS = ['en', 'es', 'pt', 'ru', 'fr', 'de', 'it', 'pl', 'tr', 'ja'];
const lang = () => { const b = (chrome.i18n.getUILanguage() || 'en').toLowerCase().split(/[-_]/)[0]; return LANGS.includes(b) ? b : 'en'; };
const T = (k) => (STR[lang()] && STR[lang()][k]) || STR.en[k] || k;
const DAY = 864e5;
const SHOP = 'https://www.fortnite.com/item-shop';

function schedule() {
  const now = new Date();
  const next = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1, 0, 2);   // 00:02 UTC, just after the shop refresh
  chrome.alarms.create('cc-daily', { when: next, periodInMinutes: 1440 });
}
chrome.runtime.onInstalled.addListener(schedule);
chrome.runtime.onStartup.addListener(schedule);

chrome.runtime.onMessage.addListener((msg, sender) => {
  if (msg?.type !== 'cc:seen') return;
  const tabId = sender.tab?.id;
  if (tabId != null) {
    const text = msg.running ? '…' : msg.free ? (msg.free > 999 ? '999+' : String(msg.free)) : '';
    chrome.action.setBadgeBackgroundColor({ tabId, color: '#d00080' });
    chrome.action.setBadgeText({ tabId, text });
  }
  const patch = { stats: msg.stats || {} };
  if (!msg.block && msg.free != null) {
    patch.activeSeen = Date.now();
    if (msg.untilIso) patch.untilIso = msg.untilIso;
  }
  chrome.storage.local.set(patch);
});

chrome.alarms.onAlarm.addListener(async (a) => {
  if (a.name !== 'cc-daily') return;
  const s = await chrome.storage.local.get({ reminder: false, activeSeen: 0, untilIso: '' });
  if (!s.reminder || !s.activeSeen) return;
  const end = s.untilIso ? Date.parse(s.untilIso) : s.activeSeen + 14 * DAY;
  if (Date.now() > end) return;
  chrome.notifications.create('cc-shop', { type: 'basic', iconUrl: 'icons/icon128.png', title: T('nTitle'), message: T('nText'), priority: 1 });
});

chrome.notifications.onClicked.addListener((id) => {
  if (id !== 'cc-shop') return;
  chrome.tabs.create({ url: SHOP });
  chrome.notifications.clear(id);
});
