// Generuje docs/PREZENTACJA.pdf z docs/PREZENTACJA.html (jedna sekcja = jedna strona 1920x1080).
// Wymaga pakietu playwright z Chromium w katalogu nadrzędnym skryptu (import ESM nie widzi npx -p):
//   npm i --no-save playwright@1.49.1 && npx playwright install chromium
//   node docs/prezentacja-pdf.mjs            (opcjonalnie: DOCS_DIR=<katalog z PREZENTACJA.html>)
// Opcjonalnie argument: katalog na podglądy PNG każdego slajdu.
import { chromium } from 'playwright';
import { fileURLToPath, pathToFileURL } from 'url';
import path from 'path';
import fs from 'fs';

const dir = process.env.DOCS_DIR || path.dirname(fileURLToPath(import.meta.url));
const html = path.join(dir, 'PREZENTACJA.html');
const pdf = path.join(dir, 'PREZENTACJA.pdf');
const prev = process.argv[2];

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 } });
await page.goto(pathToFileURL(html).href, { waitUntil: 'networkidle' });
await page.evaluate(async () => {
  await document.fonts.ready;
  await Promise.all([...document.images].map(i => i.complete ? 0 : new Promise(r => { i.onload = i.onerror = r; })));
});
if (prev) {
  fs.mkdirSync(prev, { recursive: true });
  const secs = await page.$$('section');
  for (let k = 0; k < secs.length; k++) {
    await secs[k].screenshot({ path: path.join(prev, `s${String(k + 1).padStart(2, '0')}.png`) });
  }
  // przepełnienie: elementy wystające poza sekcję lub nachodzące na stopkę
  const over = await page.evaluate(() => [...document.querySelectorAll('section')].map((s, k) => {
    const r = s.getBoundingClientRect(), f = s.querySelector('.foot').getBoundingClientRect();
    let bad = [];
    s.querySelectorAll('.row > *, .points, .grid2, .steps, .box, img').forEach(e => {
      const b = e.getBoundingClientRect();
      if (b.bottom > f.top - 8 || b.right > r.right - 100) bad.push(e.tagName + '.' + e.className + ' bottom=' + Math.round(b.bottom - r.top));
    });
    return (k + 1) + ': ' + (bad.join(' | ') || 'ok');
  }));
  console.log(over.join('\n'));
}
await page.pdf({ path: pdf, width: '1920px', height: '1080px', printBackground: true });
await browser.close();
console.log('PDF:', pdf);
