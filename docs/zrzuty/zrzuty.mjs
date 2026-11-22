// Zrzuty ekranu „Kompasu Rodzica” do zgłoszenia i prezentacji (docs/zrzuty).
//
// Wymaga pakietu `playwright` (z przeglądarką Chromium). Uruchamiaj z katalogu, w którym jest zainstalowany,
// albo przez npx, np. z katalogu repozytorium:
//   npx -y -p playwright@1.49.1 node docs/zrzuty/zrzuty.mjs http://localhost:5173 docs/zrzuty
//   (za pierwszym razem: npx -y playwright@1.49.1 install chromium)
//
// Argumenty: <adres aplikacji> <katalog wyjściowy> [fragment nazwy pliku …] — bez fragmentów robi wszystkie zrzuty.
// Panel /admin: hasło z zmiennej środowiskowej ADMIN_PASSWORD (lokalna kopia); bez niej zrzuty panelu są pomijane.
// Aplikacja musi działać na fikcyjnych osobach z fixtures; zrzut sprawy ZUS jest tylko przyciętą ramką porównania.
import fs from 'node:fs'
import path from 'node:path'
import { createRequire } from 'node:module'
import { pathToFileURL } from 'node:url'

// Playwright szukamy obok skryptu, w bieżącym katalogu albo w katalogu pakietów npx (z PATH).
async function loadPlaywright() {
  try { return await import('playwright') } catch { /* szukamy dalej */ }
  const dirs = [process.cwd(), ...(process.env.PATH ?? '').split(path.delimiter).filter((d) => /node_modules[\\/]\.bin$/.test(d)).map((d) => path.dirname(d))]
  for (const dir of dirs) {
    try { return await import(pathToFileURL(createRequire(path.join(dir, 'noop.js')).resolve('playwright')).href) } catch { /* następny */ }
  }
  throw new Error('Nie znaleziono pakietu playwright — uruchom przez npx -p playwright@1.49.1 albo z katalogu, w którym jest zainstalowany.')
}
const pw = await loadPlaywright()
const { chromium, devices } = pw.default ?? pw

const base = (process.argv[2] ?? 'http://localhost:5173').replace(/\/$/, '')
const outDir = path.resolve(process.argv[3] ?? 'docs/zrzuty')
const only = process.argv.slice(4)
const adminPassword = process.env.ADMIN_PASSWORD
fs.mkdirSync(outDir, { recursive: true })

const browser = await chromium.launch()
const problems = []
const made = []

// --- konteksty -------------------------------------------------------------
const DESK = { viewport: { width: 1280, height: 900 } }
const SLIDE = { viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.25 }
const SHARP = (width = 1280, height = 900) => ({ viewport: { width, height }, deviceScaleFactor: 2 })
const PHONE = { viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true, userAgent: devices['iPhone 13'].userAgent }

async function page(opts) {
  const ctx = await browser.newContext({ locale: 'pl-PL', timezoneId: 'Europe/Warsaw', ...opts })
  const p = await ctx.newPage()
  p.setDefaultTimeout(20000)
  p.on('pageerror', (e) => problems.push(`błąd strony ${p.url()}: ${e.message}`))
  return p
}
const at = (p, pathname) => p.waitForFunction((x) => location.pathname === x, pathname)
async function settle(p) {
  await p.waitForLoadState('networkidle')
  await p.evaluate(() => document.fonts.ready)
  // Bez animacji, mrugającego kursora i przypadkowego fokusu/podświetlenia.
  await p.addStyleTag({ content: '*,*::before,*::after{transition:none!important;animation:none!important;caret-color:transparent!important}' })
  await p.evaluate(() => document.activeElement instanceof HTMLElement && document.activeElement.blur())
  await p.mouse.move(0, 0)
}
const hideChatButton = (p) => p.addStyleTag({ content: 'button[aria-label="Zapytaj o przepisy"]{visibility:hidden!important}' })

async function shot(name, fn) {
  if (only.length && !only.some((f) => name.includes(f))) return
  try {
    await fn(path.join(outDir, name))
    made.push(name)
    console.log('ok   ', name)
  } catch (e) {
    problems.push(`${name}: ${e.message.split('\n')[0]}`)
    console.log('BŁĄD ', name, e.message.split('\n')[0])
  }
}
const save = async (p, file, extra = {}) => { await settle(p); await p.screenshot({ path: file, ...extra }); await p.context().close() }
const saveEl = async (loc, file, extra = {}) => { const p = loc.page(); await settle(p); await loc.scrollIntoViewIfNeeded(); await loc.screenshot({ path: file, ...extra }); await p.context().close() }
// Zrzut fragmentu strony: od górnej krawędzi elementu `from` (z marginesem) na zadaną wysokość.
async function saveFrom(loc, file, height, { margin = 16, x = 0, width } = {}) {
  const p = loc.page(); await settle(p)
  const box = await loc.evaluate((el) => { const r = el.getBoundingClientRect(); return { top: r.top + scrollY } })
  const vw = p.viewportSize().width
  await p.screenshot({ path: file, fullPage: true, clip: { x, y: Math.max(0, box.top - margin), width: width ?? vw - x, height } })
  await p.context().close()
}

// Zrzut prostokąta obejmującego elementy (z marginesem), w współrzędnych strony.
async function saveBox(locs, file, { pad = 16, maxHeight } = {}) {
  const p = (Array.isArray(locs) ? locs[0] : locs).page(); await settle(p)
  const boxes = []
  for (const l of [].concat(locs)) boxes.push(await l.evaluate((el) => { const r = el.getBoundingClientRect(); return { x: r.left + scrollX, y: r.top + scrollY, r: r.right + scrollX, b: r.bottom + scrollY } }))
  const x = Math.max(0, Math.min(...boxes.map((b) => b.x)) - pad), y = Math.max(0, Math.min(...boxes.map((b) => b.y)) - pad)
  const width = Math.max(...boxes.map((b) => b.r)) + pad - x
  let height = Math.max(...boxes.map((b) => b.b)) + pad - y
  if (maxHeight) height = Math.min(height, maxHeight)
  await p.screenshot({ path: file, fullPage: true, clip: { x, y, width, height } })
  await p.context().close()
}

// --- przepływy ---------------------------------------------------------------
async function login(p, person, { consent = true } = {}) {
  await p.goto(base + '/logowanie')
  await p.getByText(person, { exact: true }).first().click()
  await p.getByRole('button', { name: /Zaloguj przez mObywatela/ }).click()
  await p.getByRole('button', { name: /Połącz i pobierz dane/ }).waitFor()
  if (!consent) return
  await p.getByRole('button', { name: /Połącz i pobierz dane/ }).click()
  await at(p, '/sprawdz-dane')
  await p.getByRole('button', { name: /Dane się zgadzają/ }).waitFor()
}
async function toQuestions(p, person) {
  await login(p, person)
  await p.getByRole('button', { name: /Dane się zgadzają/ }).click()
  await p.getByRole('button', { name: /Pokaż wynik/ }).waitFor()
  await p.waitForFunction(() => !document.body.innerText.includes('Sprawdzam, czego jeszcze brakuje'))
}
// Odpowiedzi: { 'fragment pytania': 'odpowiedź' } — dla pytań tak/nie i wyboru.
async function answer(p, answers) {
  for (const [question, value] of Object.entries(answers)) {
    const group = p.getByRole('radiogroup', { name: new RegExp(question) }).or(p.getByRole('group', { name: new RegExp(question) })).first()
    await group.getByText(value, { exact: true }).click()
    await p.waitForLoadState('networkidle')
  }
}
const ANSWERS = { Anna: { 'czas określony': 'Nie' } }
async function toResult(p, person) {
  await toQuestions(p, person)
  if (ANSWERS[person]) await answer(p, ANSWERS[person])
  await p.getByRole('button', { name: /Pokaż wynik/ }).click()
  await at(p, '/wynik')
  await p.getByRole('heading', { name: 'Twój plan' }).waitFor()
  await p.waitForLoadState('networkidle')
}
const card = (p, title) => p.locator('article').filter({ has: p.getByRole('heading', { name: title, exact: true }) }).first()
const pick = (p, label) => p.locator('label').filter({ has: p.getByText(label, { exact: true }) }).first().click()
async function openChatFaq(p, question = 'Czy można pracować na urlopie rodzicielskim?') {
  await p.getByRole('button', { name: 'Zapytaj o przepisy' }).click()
  const dialog = p.getByRole('dialog')
  await dialog.getByRole('button', { name: question }).click()
  const sources = dialog.getByRole('button', { expanded: false }).filter({ hasNotText: /Nowa rozmowa/ })
  await dialog.getByText('Źródła:').first().waitFor()
  return { dialog, sources }
}
async function adminLogin(p) {
  await p.goto(base + '/admin')
  await p.getByLabel('Hasło administratora').fill(adminPassword)
  await p.getByRole('button', { name: 'Wejdź do panelu' }).click()
  await p.getByLabel('Hasło administratora').waitFor({ state: 'detached' })
  await p.waitForLoadState('networkidle')
}

// === Slajdy ==================================================================
const MAT = 'Zasiłek macierzyński za okres urlopu macierzyńskiego'
await shot('slajd-01-start.png', async (f) => { const p = await page(SLIDE); await p.goto(base + '/'); await save(p, f) })
await shot('slajd-02-odmowa.png', async (f) => {
  const p = await page(SHARP()); await toResult(p, 'Edyta'); await hideChatButton(p)
  await saveBox(card(p, MAT), f)
})
await shot('slajd-03-logowanie.png', async (f) => { const p = await page(SLIDE); await p.goto(base + '/logowanie'); await p.getByText('Anna', { exact: true }).waitFor(); await save(p, f) })
// 16:9 (800×450 px, ostrość ×2): pytanie uzupełniające z zaznaczoną odpowiedzią.
await shot('slajd-03-pytania-169.png', async (f) => {
  const p = await page(SHARP(800, 450)); await toQuestions(p, 'Anna'); await answer(p, ANSWERS.Anna)
  await saveFrom(p.getByText(/Krok 2 z 3/).first(), f, 450, { margin: 20 })
})
await shot('slajd-03-wynik.png', async (f) => { const p = await page(SLIDE); await toResult(p, 'Anna'); await save(p, f) })
// „W skrócie” i pierwsza karta świadczenia z panelem „Co dalej?” obok.
await shot('slajd-04-karta.png', async (f) => {
  const p = await page(SHARP()); await toResult(p, 'Anna'); await hideChatButton(p)
  const summary = p.locator('div, section').filter({ has: p.getByText('W skrócie', { exact: true }) }).filter({ hasText: 'Kwoty są szacunkowe' }).last()
  await saveBox([summary, card(p, MAT)], f)
})
// Rozwinięta ścieżka decyzji (najważniejsze kroki i „Pokaż wszystkie kroki”).
await shot('slajd-04-sciezka.png', async (f) => {
  const p = await page(SHARP(1280, 900)); await toResult(p, 'Anna'); await hideChatButton(p)
  const c = card(p, MAT)
  await c.getByRole('button', { name: /Szczegóły świadczenia/ }).click()
  const trace = c.getByRole('button', { name: /Ścieżka decyzji/ })
  await trace.click()
  await c.getByRole('button', { name: /Pokaż wszystkie kroki/ }).waitFor()
  await saveBox(trace.locator('xpath=..'), f)
})
async function toScenarios(p, person) {
  await toResult(p, person)
  await p.getByRole('link', { name: 'Porównaj warianty' }).first().click()
  await at(p, '/porownaj-warianty')
  await p.getByRole('heading', { name: 'Porównaj warianty' }).waitFor()
  await p.waitForLoadState('networkidle')
}
await shot('slajd-05-roznica.png', async (f) => {
  const p = await page(SHARP()); await toScenarios(p, 'Anna'); await hideChatButton(p)
  await pick(p, 'Wariant wypłaty')
  await pick(p, 'Uśredniony')
  await p.getByRole('button', { name: 'Porównaj z planem bazowym' }).click()
  const diff = p.getByRole('heading', { name: /^Różnica: plan bazowy/ })
  await diff.waitFor()
  await saveBox(p.locator('section, div').filter({ has: diff }).filter({ hasText: 'Zastosuj ten wariant' }).last(), f)
})
await shot('slajd-05-scenariusze.png', async (f) => {
  const p = await page(SHARP()); await toScenarios(p, 'Anna'); await hideChatButton(p)
  await saveBox(p.locator('div.grid').filter({ has: p.locator('input[name=scenariusz]') }).first(), f)
})
// Lista spraw ZUS z oznaczeniami zgodności — bez podglądu dokumentu, bez kwot i dat ze spraw.
await shot('slajd-05-sprawy-zus.png', async (f) => {
  const p = await page(SLIDE); await p.goto(base + '/sprawy'); await p.getByText('Sprawdź tę sprawę').first().waitFor(); await save(p, f)
})
await shot('slajd-06-telefon-wynik.png', async (f) => { const p = await page(PHONE); await toResult(p, 'Anna'); await save(p, f) })
await shot('slajd-06-wnioski-top.png', async (f) => {
  const p = await page(SHARP(720, 1000)); await login(p, 'Anna')
  await p.goto(base + '/wnioski'); await p.getByRole('heading', { level: 1 }).waitFor(); await hideChatButton(p)
  await saveFrom(p.getByRole('heading', { level: 1 }), f, 800, { margin: 16 })
})
await shot('slajd-07-faq-telefon.png', async (f) => {
  const p = await page(PHONE); await p.goto(base + '/baza-wiedzy'); await p.waitForLoadState('networkidle')
  const { sources } = await openChatFaq(p); await sources.first().click(); await save(p, f)
})
// Czat: pytanie z FAQ, źródła jako etykiety, jeden cytat rozwinięty. Na komputerze okno ma stałą wysokość 32rem
// i cała odpowiedź z cytatem się w nim nie mieści, dlatego zrzut jest w układzie pełnoekranowym (szerokość 480 px).
await shot('slajd-08-czat.png', async (f) => {
  const p = await page(SHARP(480, 800)); await p.goto(base + '/baza-wiedzy'); await p.waitForLoadState('networkidle')
  const { sources } = await openChatFaq(p); await sources.first().click()
  await p.getByRole('region', { name: /^Źródło:/ }).waitFor()
  await save(p, f)
})
if (adminPassword) {
  await shot('slajd-09-admin.png', async (f) => {
    const p = await page(SLIDE); await adminLogin(p)
    await saveFrom(p.getByRole('tablist'), f, 900, { margin: 10 })
  })
} else if (!only.length || only.some((x) => 'slajd-09-admin.png'.includes(x) || 'admin'.includes(x))) problems.push('slajd-09-admin.png i zrzuty panelu: brak ADMIN_PASSWORD — pominięte')
await shot('slajd-10-telefon-start.png', async (f) => { const p = await page(PHONE); await p.goto(base + '/'); await save(p, f) })

// === Lista z README (01–33) =================================================
const view = async (opts, url, ready) => { const p = await page(opts); await p.goto(base + url); if (ready) await p.getByText(ready).first().waitFor(); await p.waitForLoadState('networkidle'); return p }
const section = (p, heading) => p.locator('section').filter({ has: p.getByRole('heading', { name: heading, exact: true }) }).first()

await shot('01-start.png', async (f) => save(await view(DESK, '/', 'Sprawdź, co Ci przysługuje'), f))
await shot('02-wpis-reczny.png', async (f) => save(await view(DESK, '/wprowadz-dane', 'Co się u Ciebie dzieje?'), f))
await shot('03-logowanie.png', async (f) => save(await view(DESK, '/logowanie', 'Osoby testowe'), f))
await shot('04-zgoda.png', async (f) => { const p = await page(DESK); await login(p, 'Anna', { consent: false }); await p.evaluate(() => scrollTo(0, 0)); await save(p, f) })
await shot('05-potwierdz-dane.png', async (f) => { const p = await page(DESK); await login(p, 'Anna'); await save(p, f) })
await shot('06-uzupelnij-dane.png', async (f) => { const p = await page(DESK); await toQuestions(p, 'Anna'); await answer(p, ANSWERS.Anna); await save(p, f) })
await shot('07-wynik-przysluguje.png', async (f) => { const p = await page(DESK); await toResult(p, 'Anna'); await save(p, f) })
await shot('08-szczegoly-sciezka-decyzji.png', async (f) => {
  const p = await page(DESK); await toResult(p, 'Anna'); await hideChatButton(p)
  const c = card(p, MAT)
  await c.getByRole('button', { name: /Szczegóły świadczenia/ }).click()
  await c.getByRole('button', { name: /Ścieżka decyzji/ }).click()
  await c.getByRole('button', { name: /Pokaż wszystkie kroki/ }).waitFor()
  await saveBox(c, f)
})
await shot('09-terminy.png', async (f) => {
  const p = await page(DESK); await toResult(p, 'Anna'); await hideChatButton(p)
  await saveBox(section(p, 'Co zrobić i do kiedy'), f, { maxHeight: 1000 })
})
await shot('10-ryzyka-wariant.png', async (f) => {
  const p = await page(DESK); await toResult(p, 'Anna'); await hideChatButton(p)
  const t = p.getByRole('button', { name: /Ryzyka i wariant wypłaty/ }); await t.click()
  await saveBox(t.locator('xpath=..'), f)
})
await shot('11-odmowa.png', async (f) => { const p = await page(DESK); await toResult(p, 'Edyta'); await hideChatButton(p); await saveBox(card(p, MAT), f) })
async function compare(p, person, scenario, setup) {
  await toScenarios(p, person); await hideChatButton(p)
  await pick(p, scenario)
  if (setup) await setup(p)
  await p.getByRole('button', { name: 'Porównaj z planem bazowym' }).click()
  await p.getByRole('heading', { name: /^Różnica: plan bazowy/ }).waitFor()
  await p.waitForLoadState('networkidle')
}
await shot('12-porownaj-warianty.png', async (f) => {
  const p = await page(DESK); await compare(p, 'Anna', 'Wariant wypłaty', (q) => pick(q, 'Uśredniony'))
  await saveFrom(p.getByRole('heading', { name: /^Różnica: plan bazowy/ }), f, 900, { margin: 40 })
})
await shot('13-porownanie-zmiana-statusu.png', async (f) => {
  const p = await page(DESK)
  await compare(p, 'Edyta', 'Spłacam zadłużenie', async (q) => { const d = q.getByRole('textbox', { name: /W którym dniu spłacisz/ }); await d.click(); await d.pressSequentially('01042027') })
  await saveFrom(p.getByRole('heading', { name: /^Różnica: plan bazowy/ }), f, 900, { margin: 40 })
})
// Wpis ręczny dla obojga rodziców (oboje na umowie o pracę), potem wariant „Drugi rodzic bierze część urlopu rodzicielskiego”.
async function manualBothParents(p) {
  const radio = (group, label) => p.getByRole('group', { name: group }).getByText(label, { exact: true }).first().click()
  const next = async () => { const h = await p.getByRole('heading', { level: 1 }).first().innerText(); await p.getByRole('button', { name: /^(Dalej|Zapisz i przejdź dalej)$/ }).click(); await p.waitForFunction((x) => document.querySelector('h1')?.innerText !== x || location.pathname !== '/wprowadz-dane', h) }
  const work = async () => {
    await radio(/Jak pracuj/, 'Umowa o pracę')
    await p.getByRole('textbox', { name: /Od kiedy trwa ta umowa/ }).pressSequentially('01022021')
    await radio(/na czas określony/, 'Nie')
    await p.getByRole('textbox', { name: /zarabia/ }).fill('8000')
    await radio(/20 osób/, 'Tak')
  }
  await p.goto(base + '/wprowadz-dane')
  await p.getByText('Jestem w ciąży', { exact: true }).click(); await next()
  await p.getByRole('textbox', { name: /Kiedy masz termin porodu/ }).first().pressSequentially('15032027'); await next()
  await radio(/Dla kogo sprawdzić/, 'Dla mnie i drugiego rodzica')
  await p.getByRole('textbox', { name: /Twoja data urodzenia/ }).pressSequentially('12051994'); await next()
  await work(); await next()
  await work(); await next()
  await at(p, '/sprawdz-dane')
  const confirm = p.getByRole('button', { name: /Dane się zgadzają/ })
  if (await confirm.count()) await confirm.click()
  await p.getByRole('button', { name: /Pokaż wynik/ }).click()
  await at(p, '/wynik'); await p.getByRole('heading', { name: 'Twój plan' }).waitFor(); await p.waitForLoadState('networkidle')
}
await shot('14-porownanie-drugi-rodzic.png', async (f) => {
  const p = await page(DESK); await manualBothParents(p)
  await p.getByRole('link', { name: 'Porównaj warianty' }).first().click(); await at(p, '/porownaj-warianty'); await hideChatButton(p)
  await pick(p, 'Drugi rodzic bierze część urlopu rodzicielskiego')
  const who = p.getByRole('group', { name: /Kto z Was bierze/ })
  await who.locator('label').nth(1).click()
  await p.getByRole('spinbutton', { name: /tygodni/i }).or(p.getByRole('textbox', { name: /tygodni/i })).first().fill('16')
  await p.getByRole('button', { name: 'Porównaj z planem bazowym' }).click()
  const diff = p.getByRole('heading', { name: /^Różnica: plan bazowy/ }); await diff.waitFor(); await p.waitForLoadState('networkidle')
  await saveFrom(p.getByRole('heading', { name: /^2\. Ustaw wariant/ }), f, 1300, { margin: 24 })
})

await shot('15-wnioski.png', async (f) => {
  const p = await page(DESK); await login(p, 'Anna'); await p.goto(base + '/wnioski'); await p.getByRole('heading', { level: 1 }).waitFor(); await hideChatButton(p)
  await save(p, f, { fullPage: true })
})
await shot('16-wniosek-pdf.png', async (f) => {
  const p = await page(DESK); await toResult(p, 'Anna'); await p.goto(base + '/wnioski'); await p.getByRole('heading', { level: 1 }).waitFor(); await hideChatButton(p)
  await p.getByRole('link', { name: /^Wypełnij/ }).first().click()
  await pick(p, 'Drugi rodzic nie zamierza korzystać w tym okresie')
  // Formularz podpowiada koniec całego urlopu obojga rodziców (41 tyg.) i sam go odrzuca; wpisujemy koniec części matki z planu.
  await p.getByRole('textbox', { name: /Urlop rodzicielski lub jego część — do/ }).fill('12.03.2028')
  await p.getByRole('button', { name: 'Przygotuj PDF' }).click()
  await p.getByText('PDF jest gotowy').first().waitFor()
  await save(p, f)
})
await shot('17-sprawy-zus.png', async (f) => {
  const p = await view(DESK, '/sprawy', 'Sprawdź tę sprawę')
  await p.getByText(/Masz plik PDF sprawy ZUS\? Rozpoznaj go/).click()
  await save(p, f, { fullPage: true })
})
// Tylko nagłówek ramki i werdykt (bez tabeli okresów, kwot i dat ze sprawy).
await shot('19-sprawa-porownanie-z-zus.png', async (f) => {
  const p = await view(DESK, '/sprawy', 'Sprawdź tę sprawę')
  await p.getByRole('button', { name: /^Wybierz sprawę/ }).first().click()
  await at(p, '/sprawdz-dane')
  await p.getByRole('button', { name: /Dane się zgadzają/ }).click()
  await p.getByRole('button', { name: /Pokaż wynik/ }).click()
  await at(p, '/wynik'); await p.waitForLoadState('networkidle')
  const frame = p.locator('section').filter({ has: p.getByRole('heading', { name: 'Porównanie z decyzją ZUS (sprawa testowa)' }) }).first()
  await frame.waitFor(); await hideChatButton(p)
  await saveBox([frame.getByRole('heading'), frame.locator('dl').first()], f)
})
await shot('21-moje-dane-zus.png', async (f) => {
  const p = await page(DESK); await login(p, 'Halina'); await p.goto(base + '/moje-dane-zus'); await p.getByText('Saldo składek').first().waitFor(); await hideChatButton(p)
  await save(p, f, { fullPage: true })
})
await shot('22-moje-konto.png', async (f) => {
  const p = await page(DESK); await toResult(p, 'Anna'); await p.getByRole('link', { name: 'Moje konto' }).click(); await at(p, '/moje-konto'); await save(p, f)
})
await shot('23-wsparcie.png', async (f) => save(await view(DESK, '/wsparcie', 'Twoja sytuacja'), f))
await shot('24-trudna-sytuacja.png', async (f) => {
  const p = await view(DESK, '/wsparcie', 'Twoja sytuacja')
  await p.getByLabel('Twoja sytuacja').selectOption({ label: 'Martwe urodzenie' }); await p.waitForLoadState('networkidle'); await hideChatButton(p)
  await save(p, f, { fullPage: true })
})
await shot('25-baza-wiedzy.png', async (f) => save(await view(DESK, '/baza-wiedzy', 'Oficjalne źródła'), f))
await shot('26-czat-przepisy.png', async (f) => {
  const p = await view(DESK, '/baza-wiedzy'); const { dialog, sources } = await openChatFaq(p); await sources.first().click()
  await dialog.getByRole('region', { name: /^Źródło:/ }).waitFor()
  await dialog.locator('[role=log]').evaluate((log) => { log.scrollTop = log.scrollHeight })
  await save(p, f)
})
if (adminPassword) {
  await shot('27-admin-parametry.png', async (f) => { const p = await page(DESK); await adminLogin(p); await save(p, f) })
  for (const [name, tab] of [['28-admin-historia.png', 'Historia zmian'], ['29-admin-organizacje.png', 'Organizacje']]) {
    await shot(name, async (f) => {
      const p = await page(DESK); await adminLogin(p); await p.getByRole('tab', { name: tab }).click(); await p.waitForLoadState('networkidle'); await save(p, f)
    })
  }
}
await shot('30-telefon-start.png', async (f) => save(await view(PHONE, '/', 'Sprawdź, co Ci przysługuje'), f))
await shot('31-telefon-wynik.png', async (f) => {
  const p = await page(PHONE); await toResult(p, 'Anna'); await card(p, MAT).evaluate((el) => scrollTo(0, el.getBoundingClientRect().top + scrollY - 8)); await save(p, f)
})
await shot('32-telefon-odmowa.png', async (f) => {
  const p = await page(PHONE); await toResult(p, 'Edyta'); await card(p, MAT).evaluate((el) => scrollTo(0, el.getBoundingClientRect().top + scrollY - 8)); await save(p, f)
})
await shot('33-telefon-sprawy.png', async (f) => save(await view(PHONE, '/sprawy', 'Sprawdź tę sprawę'), f))

await browser.close()
console.log(`\nZrobione: ${made.length}`)
if (problems.length) { console.log('Problemy:\n- ' + problems.join('\n- ')); process.exitCode = 1 }
