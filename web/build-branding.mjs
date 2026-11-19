import { copyFileSync, readFileSync, writeFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
const root = dirname(fileURLToPath(import.meta.url));
const publicDir = join(root, 'public');
copyFileSync(join(root, '../src/ChestCounter/app.ico'), join(publicDir, 'favicon.ico'));
const image = '<img class="cc-brand-icon" src="/favicon.ico" width="36" height="36" alt="" decoding="async">';
function brand(html) {
 return html.replace(/(<a\b[^>]*class="brand"[^>]*>)(?!<img class="cc-brand-icon")/g, '$1' + image);
}
function visit(dir) {
 for (const entry of readdirSync(dir, { withFileTypes: true })) {
  const path = join(dir, entry.name);
  if (entry.isDirectory()) { visit(path); continue; }
  if (!entry.name.endsWith('.html')) continue;
  let html = readFileSync(path, 'utf8');
  html = html.replace(/href=(["'])https:\/\/github\.com\/2xFlaw\/ChestCounter\/releases\/latest\/?\1/g, 'href="https://github.com/2xFlaw/ChestCounter/releases/latest/download/ChestCounter.exe"');
  html = html.replace(/<link\b[^>]*rel=["'](?:shortcut )?icon["'][^>]*>/gi, '');
  html = html.replace('</head>', '<link rel="icon" type="image/x-icon" href="/favicon.ico?v=app1"></head>');
  if (!html.includes('href="/branding.css"')) html = html.replace('</head>', '<link rel="stylesheet" href="/branding.css"></head>');
  if (entry.name === 'privacy.html') {
   if (!html.includes('id="language-storage"')) {
    const anchor='<li><strong>Local storage:</strong>';
    if (!html.includes(anchor)) throw Error('Privacy storage integration point changed');
    html=html.replace(anchor,'<li id="language-storage">Language preferences: the cc_lang cookie remembers a language choice for up to one year. The cc.lang local-storage entry remains until changed or cleared. The language in a supported URL takes priority; without a saved choice, the main pages may use the country supplied by Cloudflare to choose a default, with browser language or English as a fallback. You can change the language with the picker or clear these preferences through browser settings.</li>'+anchor);
   }
   if (!html.includes('src="/privacy-i18n.js"')) html=html.replace('</body>','<script type="module" src="/privacy-i18n.js"></script></body>');
   html=html.replace('<script type="module" src="/privacy-es.js"></script>','');
   if (!html.includes('src="/privacy-policy.js"')) html=html.replace('</body>','<script type="module" src="/privacy-policy.js"></script></body>');
  }
  writeFileSync(path, brand(html));
 }
}
const spanish=readFileSync(join(publicDir,'privacy-es.js'),'utf8').replace(/\r\n/g,'\n');
const begin=spanish.indexOf('const content=['),end=spanish.indexOf('\nconst main=',begin);
if(begin<0||end<begin)throw Error('Spanish policy data boundaries changed');
writeFileSync(join(publicDir,'privacy-es-data.js'),spanish.slice(begin,end).replace('const content=','export const sections='));
const i18nPath=join(publicDir,'i18n.js');
let i18n=readFileSync(i18nPath,'utf8');
if(!i18n.includes("from '/i18n-extra.js'")){
 const anchor='const dict = new Map(rows(ROWS).map(r => [r[0], r.slice(1)]));';
 if(!i18n.includes(anchor))throw Error('Shared translation dictionary integration point changed');
 i18n="import { EXTRA } from '/i18n-extra.js';\n"+i18n.replace(anchor,anchor+`
for(const [source, translations] of EXTRA){
 const values=dict.get(source)||[];
 for(const [code,value] of Object.entries(translations)){
  const col=COLS.indexOf(code);
  if(col<0||typeof value!=='string'||!value.trim())throw Error('Invalid supplemental translation');
  values[col]=value;
 }
 dict.set(source,values);
}
`);
}
if(!i18n.includes("from '/i18n-stats.js'")){
 const anchor='for(const [source, translations] of EXTRA){';
 if(!i18n.includes(anchor))throw Error('Stats translation integration point changed');
 i18n="import { STATS } from '/i18n-stats.js';\n"+i18n.replace(anchor,'for(const [source, translations] of [...EXTRA,...STATS]){');
}
if(!i18n.includes("from '/i18n-stats-patterns.js'")){
 const anchor='const pats = rows(PATS).map(r => [new RegExp(r[0]), r.slice(1)]);';
 if(!i18n.includes(anchor))throw Error('Stats pattern integration point changed');
 i18n="import { STATS_PATTERNS } from '/i18n-stats-patterns.js';\n"+i18n.replace(anchor,anchor+"\npats.unshift(...STATS_PATTERNS.map(([re,translations])=>[re,COLS.map(code=>translations[code]||'')]));");
}
writeFileSync(i18nPath,i18n);
visit(publicDir);
const appPath = join(publicDir, 'app.js');
writeFileSync(appPath, brand(readFileSync(appPath, 'utf8')));
