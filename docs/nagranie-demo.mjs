// Nagranie demo Kompasu Rodzica (ok. 2:40, 1280x720, napisy lektora w kadrze) + plik napisów .srt.
// Wymaga playwright z Chromium (npm i --no-save playwright@1.49.1 && npx playwright install chromium) i działającej aplikacji
// (np. dotnet run --project api/Api z CASES_PRIVATE_DIR i ADMIN_PASSWORD + cd web && npm run dev).
// Użycie: ADMIN_PASSWORD=... node docs/nagranie-demo.mjs http://localhost:5173 <katalog_wyjściowy>  (KADRY=1 zapisuje kontrolne kadry)
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';

const base = process.argv[2] ?? 'http://localhost:5173';
const out = process.argv[3] ?? '.';
const ADMIN = process.env.ADMIN_PASSWORD ?? 'lokalne-demo';
fs.mkdirSync(out, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: { width: 1280, height: 720 },
  recordVideo: { dir: out, size: { width: 1280, height: 720 } },
  locale: 'pl-PL',
});
await context.addInitScript(() => {
  if (window.top !== window) return;
  window.__cap = (text) => {
    let el = document.getElementById('__cap');
    if (!el) {
      el = document.createElement('div');
      el.id = '__cap';
      el.style.cssText = 'position:fixed;left:50%;bottom:18px;transform:translateX(-50%);max-width:1100px;' +
        'background:rgba(16,24,40,.86);color:#fff;font:500 20px/1.35 system-ui,Segoe UI,sans-serif;padding:10px 18px;' +
        'border-radius:10px;z-index:2147483647;text-align:center;pointer-events:none;box-shadow:0 4px 18px rgba(0,0,0,.25)';
      document.documentElement.appendChild(el);
    }
    el.textContent = text;
    el.style.display = text ? 'block' : 'none';
    try { sessionStorage.setItem('__cap', text); } catch {}
  };
  const restore = () => { try { const t = sessionStorage.getItem('__cap'); if (t) window.__cap(t); } catch {} };
  document.addEventListener('DOMContentLoaded', restore);
});

const page = await context.newPage();
const t0 = Date.now();
const cues = [];
let current = null;
const stamp = () => (Date.now() - t0) / 1000;

async function cap(text) {
  if (current) current.end = stamp();
  current = text ? { start: stamp(), end: null, text } : null;
  if (current) cues.push(current);
  await page.evaluate(t => window.__cap?.(t), text ?? '').catch(() => {});
  if (text && process.env.KADRY) {
    fs.mkdirSync(path.join(out, 'kadry'), { recursive: true });
    setTimeout(() => page.screenshot({ path: path.join(out, 'kadry', `${String(cues.length).padStart(2, '0')}.png`) }).catch(() => {}), 2500);
  }
}
const pause = ms => page.waitForTimeout(ms);
const at = p => page.waitForFunction(x => location.pathname.includes(x), p, { timeout: 20000 });

async function flash(locator) {
  await locator.scrollIntoViewIfNeeded().catch(() => {});
  await locator.evaluate(el => {
    const r = el.getBoundingClientRect();
    const d = document.createElement('div');
    d.style.cssText = `position:fixed;left:${r.left - 6}px;top:${r.top - 6}px;width:${r.width + 12}px;height:${r.height + 12}px;` +
      'border:3px solid #f59e0b;border-radius:12px;z-index:2147483646;pointer-events:none;transition:opacity .4s';
    document.body.appendChild(d);
    setTimeout(() => { d.style.opacity = '0'; setTimeout(() => d.remove(), 450); }, 700);
  }).catch(() => {});
  await pause(650);
}
async function click(locator, wait = 900) {
  const l = locator.first();
  await flash(l);
  await l.click();
  await pause(wait);
}
async function scrollBy(y, ms = 1600) {
  await page.evaluate(dy => window.scrollBy({ top: dy, behavior: 'smooth' }), y);
  await pause(ms);
}
async function card(html, ms) {
  await page.setContent(`<!doctype html><html lang="pl"><body style="margin:0;height:100vh;display:grid;place-items:center;
    background:linear-gradient(135deg,#0f3d6e,#0f766e);color:#fff;font-family:system-ui,Segoe UI,sans-serif;text-align:center">
    <div style="max-width:1000px;padding:40px">${html}</div></body></html>`);
  await pause(ms);
}

// 0. Plansza
await card(`<div style="font-size:64px;font-weight:700;margin-bottom:16px">Kompas Rodzica</div>
  <div style="font-size:28px;opacity:.95">Zasiłki i urlopy dla przyszłych rodziców — dla konkretnej osoby</div>
  <div style="font-size:20px;margin-top:28px;opacity:.8">HackNation 2026 · zadanie ZUS „Zasiłkowy Przewodnik Przyszłego Rodzica” · zespół 2Stosy</div>`, 4500);

// 1. Start
await page.goto(base + '/');
await page.waitForLoadState('networkidle');
await cap('Rodzic często nie wie, co mu przysługuje i do kiedy złożyć wniosek. Kompas Rodzica odpowiada na to dla konkretnej osoby.');
await pause(4500);
await cap('Dane można wpisać ręcznie albo pobrać przez mObywatela i ZUS (w demo: symulacja na fikcyjnych osobach).');
await click(page.getByText('Połącz się przez mObywatela i ZUS'), 1500);
if (!page.url().includes('logowanie')) await page.goto(base + '/logowanie');

// 2. Logowanie i zgoda
await click(page.getByText('Anna', { exact: true }), 900);
await click(page.getByRole('button', { name: /Zaloguj przez mObywatela/ }), 1200);
await cap('Użytkownik sam wskazuje, skąd pobieramy dane: ZUS, e-ZLA, rejestr stanu cywilnego.');
await pause(2500);
await click(page.getByRole('button', { name: /Połącz i pobierz dane/ }), 400);
await at('sprawdz-dane');
await pause(1200);

// 3. Sprawdź dane i pytania
await cap('Każda informacja ma źródło i można ją poprawić.');
await scrollBy(320, 2200);
await click(page.getByRole('button', { name: /Dane się zgadzają/ }), 1500);
await cap('Pytamy tylko o to, czego ZUS nie wie — wszystko na jednym ekranie.');
const nie = page.locator('label', { hasText: /^Nie$/ });
if (await nie.count()) await click(nie, 1200);
await click(page.getByRole('button', { name: /Pokaż wynik/ }), 400);
await at('wynik');
await page.waitForLoadState('networkidle');
await pause(800);

// 4. Wynik
await cap('Twój plan: co przysługuje, na jak długo i ile — liczy deterministyczny silnik reguł, nie model językowy.');
await pause(4500);
await scrollBy(330, 2500);
await cap('Kwota i okres dla każdego świadczenia; przy urlopie rodzicielskim — Twoja część i cały wymiar obojga rodziców.');
await pause(3500);
const details = page.getByRole('button', { name: /Szczegóły świadczenia/ });
if (await details.count()) {
  await click(details, 1200);
  await cap('Szczegóły: stawka, podstawa, kto wypłaca, gdzie złożyć wniosek, dokumenty i ścieżka decyzji z przepisami.');
  await scrollBy(380, 3500);
  const path1 = page.getByRole('button', { name: /Ścieżka decyzji/ });
  if (await path1.count()) { await click(path1, 1200); await scrollBy(300, 3000); }
}
await cap('„Co zrobić i do kiedy”: terminy z przepisami i skutkiem przekroczenia.');
const deadlines = page.getByText('Co zrobić i do kiedy').first();
if (await deadlines.count()) { await deadlines.scrollIntoViewIfNeeded(); await pause(3800); }

// 5. Porównaj warianty
await page.goto(base + '/porownaj-warianty');
await page.waitForLoadState('networkidle');
await cap('„Co jeśli”: porównanie planu bazowego z wariantem — tu wypłata uśredniona 81,5%.');
await click(page.getByText('Wariant wypłaty', { exact: true }), 1200);
const step2 = page.getByText(/Ustaw wariant/).first();
if (await step2.count()) { await step2.scrollIntoViewIfNeeded(); await page.evaluate(() => window.scrollBy(0, 120)); await pause(1200); }
const avg = page.locator('label', { hasText: /uśredn/i });
if (await avg.count()) await click(avg, 800);
const go = page.getByRole('button', { name: /Porównaj|Sprawdź|Policz|Pokaż/ });
if (await go.count()) await click(go, 2000);
const diff = page.getByText(/Różnica/).first();
if (await diff.count()) { await diff.scrollIntoViewIfNeeded(); await page.evaluate(() => window.scrollBy(0, 160)); }
await pause(4500);

// 6. Sprawy ZUS
await page.goto(base + '/sprawy');
await page.waitForLoadState('networkidle');
await page.addStyleTag({ content: 'a span.text-xs.font-semibold.text-ink-soft, button span.text-xs.font-semibold.text-ink-soft { visibility: hidden !important }' });
await cap('Sprawdzenie na zanonimizowanych sprawach z zestawu ZUS: wynik kalkulatora porównany z decyzją ZUS.');
await pause(3000);
const andrzej = page.locator('a, button').filter({ hasText: /Okres i kwota zgodne z wydrukiem ZUS/ });
if (await andrzej.count()) await click(andrzej, 1500); else await click(page.getByText(/Sprawdź tę sprawę/), 1500);
await page.waitForLoadState('networkidle');
for (const name of [/Dane się zgadzają/, /Załaduj dane sprawy/, /Potwierdź dane/]) {
  const b = page.getByRole('button', { name });
  if (await b.count()) { await click(b, 1500); break; }
}
for (let i = 0; i < 3 && !page.url().includes('wynik'); i++) {
  const show = page.getByRole('button', { name: /Pokaż wynik/ });
  if (await show.count()) await click(show, 1500); else break;
}
if (page.url().includes('wynik')) {
  const cmp = page.getByText(/Porównanie z decyzją ZUS/).first();
  if (await cmp.count()) await cmp.scrollIntoViewIfNeeded();
  await cap('Ramka porównania z decyzją ZUS (sprawa testowa): wynik zgodny z aktami.');
  await pause(5000);
}

// 7. Odmowa — Edyta
await page.goto(base + '/logowanie');
await page.waitForLoadState('networkidle');
await cap('Odmowa też jest odpowiedzią: Edyta prowadzi działalność i ma 120 zł zaległych składek.');
await click(page.getByText('Edyta', { exact: true }), 700);
await click(page.getByRole('button', { name: /Zaloguj przez mObywatela/ }), 1000);
await click(page.getByRole('button', { name: /Połącz i pobierz dane/ }), 400);
await at('sprawdz-dane');
await pause(800);
await click(page.getByRole('button', { name: /Dane się zgadzają/ }), 1500);
for (let i = 0; i < 2 && !page.url().includes('wynik'); i++) {
  const show = page.getByRole('button', { name: /Pokaż wynik/ });
  if (await show.count()) await click(show, 1500);
}
await page.waitForLoadState('networkidle');
await cap('Powód, przepis i to, co musiałoby się zmienić: spłata zaległości do wskazanej daty.');
const refusal = page.getByText(/Nie przysługuje/).first();
if (await refusal.count()) await refusal.scrollIntoViewIfNeeded();
await pause(5500);

// 8. Wnioski
await page.goto(base + '/wnioski');
await page.waitForLoadState('networkidle');
await cap('Wnioski o urlopy, dane do ZAM, zapytanie do ZUS i zaświadczenie o składkach — jako PDF (wysyłka symulowana).');
await pause(3000);
await scrollBy(400, 2500);

// 9. Wsparcie i czat
await page.goto(base + '/wsparcie');
await page.waitForLoadState('networkidle');
await cap('W trudnej sytuacji aplikacja zmienia ton i pokazuje zweryfikowane organizacje wsparcia.');
const sel = page.getByRole('combobox').first();
if (await sel.count()) {
  const opts = await sel.locator('option').allTextContents();
  const stillbirth = opts.find(o => /martwe/i.test(o));
  if (stillbirth) { await flash(sel); await sel.selectOption({ label: stillbirth }); }
}
await pause(4000);
await cap('Czat „Zapytaj o przepisy”: najpierw gotowe odpowiedzi z cytatami sprawdzonymi słowo w słowo, źródła zwinięte.');
await click(page.getByRole('button', { name: /Zapytaj o przepisy/ }), 1200);
const q = page.getByRole('button', { name: /Czy można pracować na urlopie rodzicielskim/ });
if (await q.count()) await click(q, 2200);
const chip = page.getByRole('button', { name: /Kodeks pracy art/ });
if (await chip.count()) await click(chip, 2500);
await pause(2500);

// 10. Panel parametrów
await page.goto(base + '/admin');
await page.waitForLoadState('networkidle');
await cap('Panel parametrów: wartości z przepisów mają datę obowiązywania i historię zmian — bez programisty.');
const pwd = page.locator('input[type=password]').first();
if (await pwd.count()) {
  await pwd.fill(ADMIN);
  await click(page.getByRole('button', { name: /Wejdź do panelu/ }), 1500);
}
await scrollBy(350, 3500);

// 11. Telefon
await cap('Całość działa też na telefonie.');
await page.setContent(`<!doctype html><html><body style="margin:0;height:100vh;display:grid;place-items:center;background:#e8eef5">
  <div style="width:404px;height:690px;border-radius:38px;background:#111;padding:14px;box-shadow:0 20px 50px rgba(0,0,0,.3)">
  <iframe id="f" src="${base}/" style="width:390px;height:690px;border:0;border-radius:26px;background:#fff"></iframe></div></body></html>`);
await page.evaluate(t => window.__cap?.(t), 'Całość działa też na telefonie.').catch(() => {});
await pause(3500);
await page.evaluate(u => { document.getElementById('f').src = u; }, base + '/wynik');
await pause(1500);
await page.evaluate(() => { const f = document.getElementById('f'); f.contentWindow.scrollBy({ top: 500, behavior: 'smooth' }); }).catch(() => {});
await pause(3500);

// 12. Plansza końcowa
await cap('');
await card(`<div style="font-size:48px;font-weight:700;margin-bottom:18px">Kompas Rodzica</div>
  <div style="font-size:24px;line-height:1.5">Informuje, nie orzeka — a gdy przepis nie daje odpowiedzi, mówi „nie rozstrzygam”.</div>
  <div style="font-size:20px;margin-top:26px;opacity:.9">kompasrodzica.polandcentral.cloudapp.azure.com · github.com/slotocki/babypedia</div>
  <div style="font-size:15px;margin-top:26px;opacity:.85;line-height:1.5">AI w aplikacji: tylko czat „Zapytaj o przepisy” (opcjonalnie Claude Haiku, Anthropic) — wyjaśnienie obok sprawdzonych cytatów;
  uprawnienia, okresy i kwoty liczy silnik reguł bez AI. Kod, testy i dokumentację napisały modele Claude w Claude Code pod nadzorem zespołu.</div>`, 6000);
if (current) current.end = stamp();

const video = page.video();
await context.close();
const raw = await video.path();
await browser.close();
const target = path.join(out, 'demo.webm');
if (fs.existsSync(target)) fs.unlinkSync(target);
fs.renameSync(raw, target);

const ts = s => {
  const ms = Math.round(s * 1000);
  const h = String(Math.floor(ms / 3600000)).padStart(2, '0');
  const m = String(Math.floor(ms / 60000) % 60).padStart(2, '0');
  const sec = String(Math.floor(ms / 1000) % 60).padStart(2, '0');
  return `${h}:${m}:${sec},${String(ms % 1000).padStart(3, '0')}`;
};
fs.writeFileSync(path.join(out, 'demo.srt'),
  cues.filter(c => c.text).map((c, i) => `${i + 1}\n${ts(c.start)} --> ${ts(c.end ?? stamp())}\n${c.text}\n`).join('\n'), 'utf8');
console.log('OK', target, 'czas', stamp().toFixed(1), 's');
