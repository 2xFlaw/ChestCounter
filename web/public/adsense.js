(() => {
  'use strict';
  if (window.__ccAdSenseGate) return;
  window.__ccAdSenseGate = true;
  const src = 'https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-3617574685495506';
  let loaded = false, subscribed = false, attempts = 0;
  const decide = (data, success) => {
    if (!success || !data || data.cmpStatus !== 'loaded') return;
    if (!['tcloaded', 'useractioncomplete'].includes(data.eventStatus)) return;
    const allowed = data.gdprApplies === false ||
      (data.gdprApplies === true && data.purpose?.consents?.[1] === true && data.vendor?.consents?.[755] === true);
    if (!allowed) {
      // Stop the advertising integration on the next document after a choice is withdrawn.
      if (loaded) location.reload();
      return;
    }
    if (loaded) return;
    if (document.querySelector('script[src*="pagead2.googlesyndication.com/pagead/js/adsbygoogle.js"]')) return;
    loaded = true;
    const script = document.createElement('script');
    script.async = true;
    script.src = src;
    script.crossOrigin = 'anonymous';
    document.head.append(script);
  };
  const subscribe = () => {
    if (subscribed) return;
    if (typeof window.__tcfapi === 'function') {
      try {
        window.__tcfapi('addEventListener', 2, decide);
        subscribed = true;
        clearInterval(timer);
      } catch (error) { console.warn('Chest Counter: consent platform not ready; ads remain unloaded.'); }
    }
    if (++attempts >= 120) clearInterval(timer);
  };
  const timer = setInterval(subscribe, 250);
  subscribe();
})();
