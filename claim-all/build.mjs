// Builds the Chrome extension zip and the userscript from src/.  Run: node claim-all/build.mjs
import { readFileSync, writeFileSync, mkdirSync, rmSync, readdirSync, statSync, copyFileSync } from 'node:fs';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { deflateRawSync } from 'node:zlib';

const ROOT = dirname(fileURLToPath(import.meta.url));
const VERSION = '2.5.0';
const REPO_RAW = 'https://raw.githubusercontent.com/2xFlaw/ChestCounter/main/claim-all';
const ZIP_NAME = 'ChestCounter-ClaimAll-Chrome.zip';
const USER_NAME = 'chest-counter-claim-all.user.js';
const read = (p) => readFileSync(join(ROOT, p), 'utf8');

const en = (await import('./src/strings.en.js')).default;
const i18n = (await import('./src/strings.i18n.js')).default;
const STRINGS = { en, ...i18n };
const CSS = read('src/core.css').replace(/\s*\n\s*/g, '\n').trim();
const LOGO = `data:image/webp;base64,${readFileSync(join(ROOT, 'src/assets/logo96.webp')).toString('base64')}`;
const CORE = read('src/core.js');

const bundle = (platform) => [
  `// Chest Counter Claim All v${VERSION} (${platform}). Built from claim-all/src, do not edit by hand.`,
  '(() => {',
  `const CC_VERSION = ${JSON.stringify(VERSION)};`,
  `const CC_PLATFORM = ${JSON.stringify(platform)};`,
  `const CC_STRINGS = ${JSON.stringify(STRINGS)};`,
  `const CC_CSS = ${JSON.stringify(CSS)};`,
  `const CC_LOGO = ${JSON.stringify(LOGO)};`,
  CORE,
  '})();',
  '',
].join('\n');

// store names shown on chrome://extensions
const NAMES = {
  en: ['Chest Counter: Pirate Shop Claim All', 'Already own the Pirate Shop Access Token? Claim the whole Fortnite Item Shop with one button. Never spends V-Bucks.'],
  es: ['Chest Counter: Reclamar todo de la Tienda pirata', '¿Ya tienes el pase de acceso a la Tienda pirata? Reclama toda la tienda de objetos de Fortnite con un botón. Nunca gasta paVos.'],
  pt: ['Chest Counter: Resgatar tudo da Loja Pirata', 'Já tem o Token de Acesso à Loja Pirata? Resgate a Loja de Itens inteira do Fortnite com um botão. Nunca gasta V-Bucks.'],
  ru: ['Chest Counter: забрать всё в Пиратском магазине', 'Уже есть жетон доступа к Пиратскому магазину? Заберите весь магазин предметов Fortnite одной кнопкой. В-баксы не тратятся.'],
  fr: ['Chest Counter : Tout récupérer dans la Boutique pirate', 'Jeton d’accès à la Boutique pirate déjà en poche ? Récupérez toute la boutique Fortnite en un clic. Ne dépense jamais de V-bucks.'],
  de: ['Chest Counter: Piratenshop alles abholen', 'Hast du schon das Piratenshop-Zugangstoken? Hol dir den ganzen Fortnite-Item-Shop mit einem Klick. Gibt nie V-Bucks aus.'],
  it: ['Chest Counter: Riscatta tutto nel Negozio pirata', 'Hai già il gettone di accesso al Negozio pirata? Riscatta tutto il negozio oggetti di Fortnite con un clic. Non spende mai V-buck.'],
  pl: ['Chest Counter: Odbierz wszystko w Sklepie pirackim', 'Masz już żeton dostępu do Sklepu pirackiego? Odbierz cały sklep Fortnite jednym przyciskiem. Nigdy nie wydaje V-dolców.'],
  tr: ['Chest Counter: Korsan Dükkânı Hepsini al', 'Korsan Dükkânı erişim jetonun zaten var mı? Tüm Fortnite Eşya Dükkânı’nı tek tuşla al. Asla V-Papel harcamaz.'],
  ja: ['Chest Counter: 海賊ショップまとめて入手', '海賊ショップのアクセストークンをお持ちですか？Fortniteのアイテムショップ全体をボタン1つで入手。V-Bucksは使いません。'],
};

// ---------- tiny zip writer (deflate) ----------
const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
const crc32 = (b) => { let c = -1; for (const x of b) c = CRC[(c ^ x) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
function zip(files) {
  const parts = [], central = [];
  let off = 0;
  const d = new Date(), time = (d.getHours() << 11) | (d.getMinutes() << 5) | (d.getSeconds() >> 1), date = ((d.getFullYear() - 1980) << 9) | ((d.getMonth() + 1) << 5) | d.getDate();
  for (const [name, data] of files) {
    const nb = Buffer.from(name), comp = deflateRawSync(data, { level: 9 }), crc = crc32(data);
    const h = Buffer.alloc(30); h.writeUInt32LE(0x04034b50, 0); h.writeUInt16LE(20, 4); h.writeUInt16LE(0x0800, 6); h.writeUInt16LE(8, 8); h.writeUInt16LE(time, 10); h.writeUInt16LE(date, 12);
    h.writeUInt32LE(crc, 14); h.writeUInt32LE(comp.length, 18); h.writeUInt32LE(data.length, 22); h.writeUInt16LE(nb.length, 26);
    const c = Buffer.alloc(46); c.writeUInt32LE(0x02014b50, 0); c.writeUInt16LE(20, 4); c.writeUInt16LE(20, 6); c.writeUInt16LE(0x0800, 8); c.writeUInt16LE(8, 10); c.writeUInt16LE(time, 12); c.writeUInt16LE(date, 14);
    c.writeUInt32LE(crc, 16); c.writeUInt32LE(comp.length, 20); c.writeUInt32LE(data.length, 24); c.writeUInt16LE(nb.length, 28); c.writeUInt32LE(off, 42);
    parts.push(h, nb, comp); central.push(c, nb); off += 30 + nb.length + comp.length;
  }
  const cd = Buffer.concat(central), e = Buffer.alloc(22);
  e.writeUInt32LE(0x06054b50, 0); e.writeUInt16LE(files.length, 8); e.writeUInt16LE(files.length, 10); e.writeUInt32LE(cd.length, 12); e.writeUInt32LE(off, 16);
  return Buffer.concat([...parts, cd, e]);
}
const walk = (dir) => readdirSync(dir).flatMap((f) => { const p = join(dir, f); return statSync(p).isDirectory() ? walk(p) : [p]; });

// ---------- Chrome extension ----------
const OUT = join(ROOT, 'dist');
const EXT = join(OUT, 'chrome');
rmSync(OUT, { recursive: true, force: true });
mkdirSync(EXT, { recursive: true });
for (const f of walk(join(ROOT, 'extension'))) {
  const to = join(EXT, relative(join(ROOT, 'extension'), f));
  mkdirSync(dirname(to), { recursive: true });
  copyFileSync(f, to);
}
const manifest = JSON.parse(read('extension/manifest.json'));
manifest.version = VERSION;
writeFileSync(join(EXT, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
writeFileSync(join(EXT, 'claim.js'), bundle('chrome'));
writeFileSync(join(EXT, 'strings.js'), `// Built from claim-all/src, do not edit by hand.\nexport default ${JSON.stringify(STRINGS)};\n`);
for (const [l, [name, desc]] of Object.entries(NAMES)) {
  const dir = l === 'pt' ? 'pt_BR' : l;   // Chrome has no plain pt locale
  if (desc.length > 132 || name.length > 75) throw new Error(`${l}: name or description too long for Chrome`);
  mkdirSync(join(EXT, '_locales', dir), { recursive: true });
  writeFileSync(join(EXT, '_locales', dir, 'messages.json'), `${JSON.stringify({ extName: { message: name }, extDesc: { message: desc } }, null, 2)}\n`);
}
const files = walk(EXT).sort().map((f) => [relative(EXT, f).split('\\').join('/'), readFileSync(f)]);
writeFileSync(join(OUT, ZIP_NAME), zip(files));

// ---------- userscript (Firefox, Safari, Opera, anything with Tampermonkey or Violentmonkey) ----------
const header = `// ==UserScript==
// @name         ${NAMES.en[0]}
${Object.entries(NAMES).filter(([l]) => l !== 'en').map(([l, [n]]) => `// @name:${l === 'pt' ? 'pt-BR' : l}${' '.repeat(Math.max(1, 8 - l.length - (l === 'pt' ? 3 : 0)))}${n}`).join('\n')}
// @namespace    https://chestcounter.io
// @version      ${VERSION}
// @description  ${NAMES.en[1]}
// @author       2xFlaw
// @license      MIT
// @homepageURL  https://chestcounter.io/claim
// @supportURL   https://github.com/2xFlaw/ChestCounter/issues
// @icon         ${REPO_RAW}/extension/icons/icon48.png
// @match        https://www.fortnite.com/*
// @grant        none
// @inject-into  page
// @run-at       document-idle
// @noframes
// @updateURL    ${REPO_RAW}/dist/${USER_NAME}
// @downloadURL  ${REPO_RAW}/dist/${USER_NAME}
// ==/UserScript==
`;
writeFileSync(join(OUT, USER_NAME), header + bundle('userscript'));

// the popup and the website read this to offer updates
writeFileSync(join(ROOT, 'version.json'), `${JSON.stringify({
  version: VERSION,
  page: 'https://chestcounter.io/claim',
  chrome: `https://github.com/2xFlaw/ChestCounter/releases/latest/download/${ZIP_NAME}`,
  userscript: `${REPO_RAW}/dist/${USER_NAME}`,
}, null, 2)}\n`);

console.log(`built v${VERSION}: dist/${ZIP_NAME} (${files.length} files), dist/${USER_NAME}`);
