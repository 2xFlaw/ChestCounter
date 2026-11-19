// Isolated-world helper: relays the popup to the page script and mirrors settings and state.
(() => {
  const SETTINGS_KEY = 'ccClaim.settings';
  const ds = () => document.documentElement.dataset;
  const readLocal = (k) => { try { return JSON.parse(localStorage.getItem(k) || '{}'); } catch (_) { return {}; } };

  // chrome.storage is the source of truth for settings, the page script reads localStorage
  function mirrorSettings(s) {
    const cur = readLocal(SETTINGS_KEY);
    const next = { ...cur, quick: !!s.quick };
    try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(next)); } catch (_) { /* ignore */ }
    document.dispatchEvent(new CustomEvent('cc:refresh'));
  }
  chrome.storage.local.get({ quick: false }, mirrorSettings);
  chrome.storage.onChanged.addListener((ch, area) => {
    if (area === 'local' && 'quick' in ch) mirrorSettings({ quick: ch.quick.newValue });
  });

  const status = () => {
    const d = ds();
    let stats = {};
    try { stats = JSON.parse(d.ccStats || '{}'); } catch (_) { /* ignore */ }
    return {
      ok: d.ccVersion != null,
      free: d.ccFree === '' || d.ccFree == null ? null : Number(d.ccFree),
      value: Number(d.ccValue || 0),
      until: d.ccUntil || '',
      untilIso: d.ccUntilIso || '',
      running: d.ccRunning === '1',
      block: d.ccBlock || '',
      stats,
    };
  };

  chrome.runtime.onMessage.addListener((msg, _sender, reply) => {
    if (msg?.type === 'cc:status') reply(status());
    else if (msg?.type === 'cc:claim') { document.dispatchEvent(new CustomEvent('cc:claim')); reply({ ok: true }); }
  });

  // tell the background what the page shows, for the toolbar badge and the daily reminder
  let last = '';
  const report = () => {
    const s = status();
    if (!s.ok) return;
    const key = JSON.stringify([s.free, s.block, s.untilIso, s.running, s.stats.offers]);
    if (key === last) return;
    last = key;
    try { chrome.runtime.sendMessage({ type: 'cc:seen', ...s }); } catch (_) { /* extension reloaded */ }
  };
  const start = () => {
    new MutationObserver(report).observe(document.documentElement, { attributes: true, attributeFilter: ['data-cc-free', 'data-cc-block', 'data-cc-until-iso', 'data-cc-running', 'data-cc-stats'] });
    report();
  };
  if (document.documentElement) start(); else document.addEventListener('DOMContentLoaded', start);
})();
