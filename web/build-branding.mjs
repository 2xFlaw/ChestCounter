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
  writeFileSync(path, brand(html));
 }
}
visit(publicDir);
const appPath = join(publicDir, 'app.js');
writeFileSync(appPath, brand(readFileSync(appPath, 'utf8')));
