#!/usr/bin/env node
// Konwerter analiz spraw ZUS (analiza/<sprawa>/dane.json) na format aplikacji:
//   <out>/cases.json            — lista spraw z rozpoznaniem pliku (SHA-256, nazwa, liczba stron)
//   <out>/personas/<id>.json    — osoba z zestawem faktów dla silnika
//   <out>/raport-konwersji.md   — co przeliczono, czego zabrakło
//
// Użycie: node tools/cases/convert.mjs --analiza <dir> --pdf <dir> --out <dir> [--tylko 007,022,...]
//
// Katalog <out> to CASES_PRIVATE_DIR. Musi leżeć POZA repozytorium: dokumentów ZUS i danych z nich
// nie wolno rozpowszechniać. Skrypt niczego nie zgaduje — pole, którego nie ma w analizie, zostaje
// pominięte (silnik zada wtedy pytanie) i trafia do raportu. Ręczne uzupełnienia z dokumentu:
// <out>/poprawki/<id>.json = { "fakty": {...}, "potwierdzoneOkresy": [...], "oczekiwanaDecyzja": "...", "uwagi": "..." }
// (fakty są nakładane klucz po kluczu na wynik konwersji; wartość null usuwa klucz).
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const args = Object.fromEntries(process.argv.slice(2).reduce((acc, a, i, all) => {
  if (a.startsWith('--')) acc.push([a.slice(2), all[i + 1]]);
  return acc;
}, []));
for (const required of ['analiza', 'pdf', 'out']) {
  if (!args[required]) {
    console.error('Użycie: node tools/cases/convert.mjs --analiza <dir> --pdf <dir> --out <dir> [--tylko 007,022]');
    process.exit(2);
  }
}
const analizaDir = resolve(args.analiza);
const pdfDir = resolve(args.pdf);
const outDir = resolve(args.out);
const only = args.tylko ? new Set(args.tylko.split(',').map((s) => s.trim())) : null;
// --zgodnosc 008:kwota,004:okres,021:decyzja — co w danej sprawie sprawdzono z dokumentami ZUS (wynik raportu silnika).
const agreement = new Map((args.zgodnosc ?? '').split(',').filter(Boolean).map((pair) => pair.trim().split(':')));

const repoRoot = resolve(import.meta.dirname, '..', '..');
if (outDir.toLowerCase().startsWith(repoRoot.toLowerCase())) {
  console.error('Katalog wyjściowy leży w repozytorium. Dane spraw ZUS muszą zostać poza nim.');
  process.exit(2);
}

const ISO = /^\d{4}-\d{2}-\d{2}$/;
const isDate = (v) => typeof v === 'string' && ISO.test(v);
const fact = (value, source, page) => (page ? { value, source, page } : { value, source });
const days = (from, to) => Math.round((Date.parse(to) - Date.parse(from)) / 86400000) + 1;
const norm = (s) => (s ?? '').toString().toLowerCase();

const TITLES = [
  [/współprac/, 'osoba_wspolpracujaca', 'dobrowolne', 'osoba współpracująca'],
  [/działalno/, 'dzialalnosc', 'dobrowolne', 'pozarolnicza działalność'],
  [/zleceni/, 'zleceniobiorca', 'dobrowolne', 'zleceniobiorca'],
  [/prac/, 'pracownik', 'obowiazkowe', 'pracownik'],
];

const BENEFIT_PERIODS = [
  ['urlop_macierzynski', 'macierzynski'],
  ['urlop_uzupelniajacy', 'macierzynski_uzupelniajacy'],
  ['urlop_rodzicielski', 'rodzicielski'],
  ['urlop_ojcowski', 'ojcowski'],
];

function convert(folder, dane, basisConfirmed) {
  const missing = [];
  const notes = [];
  const k = dane.klasyfikacja ?? {};
  const ub = dane.ubezpieczenie ?? {};
  const dz = dane.dziecko ?? {};
  const ok = dane.okresy ?? {};
  const events = [].concat(k.zdarzenie ?? []).map(norm).join(' | ');
  const fileName = dane.plik_zrodlowy ?? '';
  const context = `${events} | ${norm(fileName)}`;

  // --- osoba
  const role = /ojciec|ojca/.test(norm(k.odbiorca)) ? 'ojciec'
    : /matka/.test(norm(k.odbiorca)) ? 'matka'
    : norm(dane.beneficjent?.plec).startsWith('m') ? 'ojciec'
    : norm(dane.beneficjent?.plec).startsWith('k') ? 'matka' : null;
  if (!role) missing.push('rola osoby (matka/ojciec)');

  const person = {};
  if (role) person['osoba.rola'] = fact(role, 'zus');

  const titleText = norm(ub.tytul ?? k.tytul_ubezpieczenia);
  const title = TITLES.find(([re]) => re.test(titleText));
  const covered = !/nie podlega/.test(norm(ub.status));
  // W wydrukach ZUS tytuł „kończy się” w przeddzień zasiłku, bo zastępuje go tytuł „osoba pobierająca zasiłek”.
  // To nie jest ustanie ubezpieczenia, więc takiej daty końca nie przenosimy.
  const benefitStarts = ['urlop_macierzynski_od', 'urlop_rodzicielski_od', 'urlop_ojcowski_od', 'zwolnienie_lekarskie_od']
    .map((key) => ok[key]).filter(isDate).map((d) => new Date(Date.parse(d) - 86400000).toISOString().slice(0, 10));
  const titleEnd = isDate(ub.okres_do) && !(covered && benefitStarts.includes(ub.okres_do)) ? ub.okres_do : null;
  const granted = norm(dane.typ_sprawy).startsWith('przyzna');
  if (!title) {
    missing.push('tytuł ubezpieczenia');
  } else if (!isDate(ub.okres_od)) {
    missing.push('data początku ubezpieczenia (zaczerniona lub nieodczytana)');
  } else {
    person['ubezpieczenie.tytuly'] = fact([{
      opis: title[3],
      rodzajTytulu: title[1],
      od: ub.okres_od,
      do: titleEnd,
      chorobowe: title[2],
    }], 'zus');
    if (!covered && !isDate(ub.okres_do)) {
      notes.push('ZUS stwierdził brak ubezpieczenia, ale analiza nie podaje daty ustania tytułu — uzupełnij w poprawkach');
    }
  }
  if (!covered) person['ubezpieczenie.podlegaChorobowemu'] = fact(false, 'zus');

  // Podstawę z analizy przenosimy tylko wtedy, gdy potwierdza ją wydruk ZUS (--zgodnosc NNN:kwota). W pozostałych
  // analizach to pole bywa wynagrodzeniem brutto albo podstawą składek, czyli kwotą przed odliczeniem 13,71%.
  const basis = dane.zasilek?.podstawa_wymiaru_pln;
  if (typeof basis === 'number' && basis > 0 && basisConfirmed) {
    person['podstawa.ustalonaPrzezPlatnika'] = fact(Math.round(basis * 100) / 100, 'zus');
  } else {
    missing.push('podstawa wymiaru (kwoty nie da się porównać)');
  }

  if (isDate(ok.zwolnienie_lekarskie_od) && isDate(ok.zwolnienie_lekarskie_do)) {
    person['ezla.zwolnienia'] = fact([{
      od: ok.zwolnienie_lekarskie_od, do: ok.zwolnienie_lekarskie_do, kod: /ciąż/.test(context) ? 'B' : null, szpital: false,
    }], 'ezla');
  }

  const weeksOf = (prefix) => {
    const from = ok[`${prefix}_od`];
    const to = ok[`${prefix}_do`];
    if (!isDate(from) || !isDate(to)) return null;
    const d = days(from, to);
    if (d % 7 !== 0) notes.push(`${prefix}: ${d} dni nie dzieli się na pełne tygodnie — sprawdź daty`);
    return { tygodnie: Math.max(1, Math.round(d / 7)), od: from };
  };
  const parental = weeksOf('urlop_rodzicielski');
  if (parental) person['plan.rodzicielskiCzesci'] = fact([parental], 'user');
  const paternity = weeksOf('urlop_ojcowski');
  if (paternity) person['plan.ojcowskiCzesci'] = fact([paternity], 'user');

  // --- sprawa
  const sprawa = { 'zdarzenie.dlaKogo': fact('ja', 'assumption') };
  const adoption = /przyjęci|przysposob|adopc|rodzin\w* zastępcz/.test(context);
  const birthDate = isDate(dz.data_porodu) ? dz.data_porodu : isDate(dz.data_urodzenia) ? dz.data_urodzenia : null;
  if (adoption) {
    sprawa['zdarzenie.rodzaj'] = fact('przyjecie', 'zus');
    if (isDate(ok.urlop_macierzynski_od)) {
      sprawa['zdarzenie.data'] = fact(ok.urlop_macierzynski_od, 'zus');
      notes.push('data przyjęcia dziecka przyjęta jako pierwszy dzień zasiłku — sprawdź w dokumencie');
    } else missing.push('data przyjęcia dziecka');
    if (birthDate) sprawa['dziecko.dataUrodzenia'] = fact(birthDate, 'usc');
    else missing.push('data urodzenia przyjmowanego dziecka');
    if (/rodzin\w* zastępcz/.test(context)) notes.push('rodzina zastępcza: ustaw przyjecie.sciezka (zawodowa/niezawodowa) w poprawkach');
    else sprawa['przyjecie.sciezka'] = fact('przysposobienie', 'zus');
  } else if (birthDate) {
    sprawa['zdarzenie.rodzaj'] = fact('urodzenie', 'usc');
    sprawa['zdarzenie.data'] = fact(birthDate, 'usc');
  } else if (/ciąż/.test(context)) {
    sprawa['zdarzenie.rodzaj'] = fact('ciaza', 'zus');
    if (isDate(ok.pdt)) sprawa['zdarzenie.data'] = fact(ok.pdt, 'zus');
    else missing.push('przewidywana data porodu');
  } else {
    missing.push('rodzaj i data zdarzenia');
  }
  if (Number.isInteger(dz.liczba_dzieci) && dz.liczba_dzieci > 0) {
    sprawa['dziecko.liczba'] = fact(dz.liczba_dzieci, 'usc');
  } else {
    sprawa['dziecko.liczba'] = fact(1, 'assumption');
    notes.push('liczba dzieci nieodczytana — przyjęto 1 (założenie)');
  }
  if (granted && title && title[2] === 'dobrowolne' && sprawa['zdarzenie.data']) {
    person['skladki.zadluzenie'] = fact({ naDzien: sprawa['zdarzenie.data'].value, kwota: 0 }, 'zus');
    notes.push('brak zadłużenia przyjęty na podstawie przyznania zasiłku przez ZUS');
  }
  if (/martw/.test(norm(dz.porod))) sprawa['dziecko.martweUrodzenie'] = fact(true, 'usc');
  if (Number.isInteger(dz.tydzien_ciazy) && dz.tydzien_ciazy >= 20) sprawa['dziecko.tydzienCiazyPrzyPorodzie'] = fact(dz.tydzien_ciazy, 'zus');
  if (Number.isInteger(dz.masa_urodzeniowa_g) && dz.masa_urodzeniowa_g > 0) sprawa['dziecko.masaUrodzeniowaG'] = fact(dz.masa_urodzeniowa_g, 'zus');
  if (/za życiem/.test(context)) sprawa['dziecko.zaZyciem'] = fact(true, 'zus');
  if (/likwidacj/.test(context)) person['ubezpieczenie.ustaniePrzyczyna'] = fact('likwidacja_pracodawcy', 'zus');

  // --- sprawa na liście
  const confirmed = BENEFIT_PERIODS.flatMap(([prefix, benefit]) => {
    const from = ok[`${prefix}_od`];
    const to = ok[`${prefix}_do`];
    if (!isDate(from) || !isDate(to)) return [];
    const d = days(from, to);
    return [{ swiadczenie: adoption && benefit === 'macierzynski' ? 'przyjecie_dziecka' : benefit, od: from, do: to, dni: d, tygodnie: Math.round((d / 7) * 100) / 100 }];
  });
  const type = norm(dane.typ_sprawy);
  const decision = type.startsWith('odmow') || dane.odmowa?.czy_odmowa === true ? 'odmowa'
    : type.startsWith('przyzna') ? 'przyznanie' : 'do_odczytania';

  return { person, sprawa, confirmed, decision, missing, notes, fileName };
}

function overlay(target, patch) {
  for (const [key, value] of Object.entries(patch ?? {})) {
    if (value === null) delete target[key];
    else target[key] = value;
  }
}

function fictionalPesel(number) {
  const digits = `0000000${number}`.split('').map(Number);
  const weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
  const valid = (10 - (digits.reduce((sum, d, i) => sum + d * weights[i], 0) % 10)) % 10;
  return `${digits.join('')}${(valid + 1) % 10}`;
}

function sha256(file) {
  return createHash('sha256').update(readFileSync(file)).digest('hex');
}

mkdirSync(join(outDir, 'personas'), { recursive: true });
mkdirSync(join(outDir, 'poprawki'), { recursive: true });

const cases = [];
const report = ['# Raport konwersji spraw', '', `Źródło: ${analizaDir}`, ''];
const seenFiles = new Set();

for (const folder of readdirSync(analizaDir).sort()) {
  const number = /^(\d{3})-/.exec(folder)?.[1];
  if (!number) continue; // starsze, próbne analizy (01-…05-) mają odpowiedniki z numerem trzycyfrowym
  if (only && !only.has(number)) continue;
  const file = join(analizaDir, folder, 'dane.json');
  if (!existsSync(file)) continue;

  let dane;
  try {
    dane = JSON.parse(readFileSync(file, 'utf8'));
  } catch (error) {
    report.push(`## ${folder}`, '', `POMINIĘTO: plik dane.json nie jest poprawnym JSON (${error.message}).`, '');
    continue;
  }

  const id = `zus-${folder.split('-').slice(0, 3).join('-')}`.replace(/[^a-z0-9-]/g, '');
  const result = convert(folder, dane, agreement.get(number) === 'kwota');
  const patchFile = join(outDir, 'poprawki', `${id}.json`);
  const patch = existsSync(patchFile) ? JSON.parse(readFileSync(patchFile, 'utf8')) : null;
  if (patch) {
    overlay(result.sprawa, patch.fakty?.sprawa);
    overlay(result.person, patch.fakty?.wnioskodawca);
    if (patch.potwierdzoneOkresy) result.confirmed = patch.potwierdzoneOkresy;
    if (patch.oczekiwanaDecyzja) result.decision = patch.oczekiwanaDecyzja;
  }

  const pdf = join(pdfDir, result.fileName);
  const plik = { nazwa: result.fileName };
  if (Number.isInteger(dane.liczba_stron)) plik.liczbaStron = dane.liczba_stron;
  if (result.fileName && existsSync(pdf)) {
    plik.sha256 = [sha256(pdf)];
  } else {
    result.notes.push('nie znaleziono pliku PDF — rozpoznanie tylko po nazwie i liczbie stron');
  }
  if (seenFiles.has(result.fileName)) result.notes.push('ten sam plik źródłowy co inna sprawa');
  seenFiles.add(result.fileName);

  const firstName = (result.fileName.split(/[\s_]/)[0] || 'Osoba').replace(/[^\p{L}]/gu, '') || 'Osoba';
  const title = result.fileName.replace(/\.pdf$/i, '').replace(/^[^_]*_(\d+_)?/, '').trim() || folder;
  const persona = {
    id,
    opis: `Sprawa ZUS nr ${number}: ${title}`,
    status: 'gotowe',
    sprawa: id,
    tozsamosc: {
      imie: firstName,
      nazwisko: `(sprawa ${number})`,
      pesel: fictionalPesel(number), // umowny, celowo niezgodny z sumą kontrolną; dane osobowe w dokumentach są zaczernione
      dataUrodzenia: isDate(dane.beneficjent?.data_urodzenia) ? dane.beneficjent.data_urodzenia : '1990-01-01',
    },
    fakty: { sprawa: result.sprawa, wnioskodawca: result.person },
    uwagi: [
      isDate(dane.beneficjent?.data_urodzenia) ? null : 'Data urodzenia osoby zaczerniona — przyjęta umownie.',
      patch?.uwagi ?? null,
    ].filter(Boolean).join(' ') || undefined,
  };
  writeFileSync(join(outDir, 'personas', `${id}.json`), `${JSON.stringify(persona, null, 2)}\n`);

  const entry = { id, tytul: title, oczekiwanaDecyzja: result.decision, persona: id, plik };
  if (agreement.has(number)) entry.zgodnosc = agreement.get(number);
  if (result.confirmed.length) entry.potwierdzoneOkresy = result.confirmed;
  cases.push(entry);

  report.push(`## ${id} — ${title}`, '',
    `- decyzja ZUS: ${result.decision}; pewność odczytu: ${dane.pewnosc ?? 'brak oceny'}${patch ? '; zastosowano poprawki ręczne' : ''}`,
    `- okresy potwierdzone przez ZUS: ${result.confirmed.map((p) => `${p.swiadczenie} ${p.od}–${p.do} (${p.dni} dni)`).join('; ') || 'brak'}`,
    `- brakuje: ${result.missing.join('; ') || 'nic'}`,
    ...(result.notes.length ? [`- uwagi: ${result.notes.join('; ')}`] : []), '');
}

writeFileSync(join(outDir, 'cases.json'), `${JSON.stringify({ cases }, null, 2)}\n`);
writeFileSync(join(outDir, 'raport-konwersji.md'), `${report.join('\n')}\n`);
console.log(`Przeliczono spraw: ${cases.length}. Wynik: ${outDir}`);
