import type { BenefitResult, Deadline, Link, TraceStep } from '@/app/api/types'
import { benefitName, personLabels } from './labels'

export interface DeadlineEntry {
  deadline: Deadline
  benefit: BenefitResult
}

/** Id terminu wniosku o wariant 81,5% (DeadlineRules.WniosekUsrednionaRule). Przy wariancie standardowym to wybór, nie obowiązek. */
export const AVERAGED_DEADLINE = 'wniosek_usredniona'

export function benefitList(benefits: BenefitResult[], showPerson: boolean): string {
  return benefits.map((b) => benefitName(b.id) + (showPerson ? ` (${personLabels[b.person]})` : '')).join(', ')
}

/** Początek notatki silnika na karcie świadczenia warunkowego (BenefitResult.ConditionalNotePrefix w api/Domain). */
const CONDITIONAL_PREFIX = 'Świadczenie warunkowe:'
const CONDITIONAL_LEAD = 'dotyczy sytuacji, '

/** Początek uwagi silnika (RodzicielskiRules) o pokazaniu wymiaru łącznego obojga rodziców. */
export const SHARED_TOTAL_NOTE = 'Pokazany jest łączny wymiar'

/** Uwaga silnika (AmountsService) o kwocie policzonej tylko za część urlopu rodzicielskiego tej osoby. */
const OWN_SHARE_NOTE = /^Kwota obejmuje (\d{2})\.(\d{2})\.(\d{4})–(\d{2})\.(\d{2})\.(\d{4}) \((\d+) dni\)/

/** Uwaga silnika o stawce bez okresu („Kwotę policzymy, gdy będzie znany okres zasiłku…”). */
const RATE_ONLY_NOTE = 'Stawka: '

/** Uwaga silnika o samej stawce (bez okresu i kwoty) — karta pokazuje ją od razu zamiast pustego pola. */
export function rateOnlyNote(benefit: BenefitResult): string | null {
  if (benefit.period || benefit.amount || benefit.lumpSum != null) return null
  return benefit.notes.find((n) => n.startsWith(RATE_ONLY_NOTE)) ?? null
}

/**
 * Warunek świadczenia warunkowego, np. „gdy lekarz wystawi Ci zwolnienie lekarskie (L4) w okresie ciąży”.
 * Silnik ocenia wtedy sytuację „gdyby zdarzenie nastąpiło dziś” i nie podaje terminów.
 */
export function conditionOf(benefit: BenefitResult): string | null {
  const note = benefit.notes.find((n) => n.startsWith(CONDITIONAL_PREFIX))
  if (!note) return null
  const rest = note.slice(CONDITIONAL_PREFIX.length).trim().replace(/\.$/, '')
  return rest.startsWith(CONDITIONAL_LEAD) ? rest.slice(CONDITIONAL_LEAD.length) : rest
}

export interface OwnShare {
  from: string
  to: string
  days: number
}

/** Część urlopu rodzicielskiego, za którą silnik policzył kwotę (gdy karta pokazuje łączny wymiar obojga rodziców). */
export function ownShareOf(benefit: BenefitResult): { share: OwnShare; note: string } | null {
  for (const note of benefit.notes) {
    const m = OWN_SHARE_NOTE.exec(note)
    if (m) return { share: { from: `${m[3]}-${m[2]}-${m[1]}`, to: `${m[6]}-${m[5]}-${m[4]}`, days: Number(m[7]) }, note }
  }
  return null
}

/** Uwagi, które karta pokazuje w innym miejscu (nie powtarzamy ich na liście „Szczegóły”). */
export function shownElsewhere(benefit: BenefitResult): Set<string> {
  const hidden = new Set<string>()
  for (const note of benefit.notes) {
    if (note.startsWith(CONDITIONAL_PREFIX) || note.startsWith(SHARED_TOTAL_NOTE)) hidden.add(note)
    if (note.startsWith(RATE_ONLY_NOTE) && (conditionOf(benefit) || rateOnlyNote(benefit))) hidden.add(note)
  }
  const own = ownShareOf(benefit)
  if (own) hidden.add(own.note)
  return hidden
}

/** W sytuacji straty dziecka nie pokazujemy linków o urlopie rodzicielskim i rodzicielstwie. */
export function linksFor(links: Link[], difficult: boolean): Link[] {
  if (!difficult) return links
  return links.filter((l) => !/gov\.pl\/web\/rodzina|pip\.gov\.pl/i.test(l.url))
}

/** Kroki ścieżki decyzji, które pokazujemy od razu (reszta pod „Pokaż wszystkie kroki”). */
const KEY_RULES = new Set([
  'ubezpieczenie_w_dniu_zdarzenia',
  'matka_nieubezpieczona_ojciec_ubezpieczony',
  'okres_wyczekiwania',
  'macierzynski_okres',
  'rodzicielski_okres',
  'rodzicielski_nieprzenoszalne_tygodnie',
  'ojcowski_okres',
  'przyjecie_okres',
  'uzupelniajacy_okres',
  'srednia_wynagrodzen_pracownika',
  'srednia_przychodu_niepracownika',
  'kwota_za_okres',
  'zasilek_pogrzebowy',
  'martwe_urodzenie_zgon_dziecka',
])

export const KEY_STEP_LIMIT = 6

/** Kroki z wynikiem „nie”/„brak danych”, które nie blokują świadczenia (np. brak podstawy od płatnika → szacunek z danych ZUS). */
const NON_BLOCKING = new Set(['podstawa_ustalona_przez_platnika', 'podwyzszenie_do_swiadczenia_rodzicielskiego', 'kwoty_brutto'])

export function keySteps(trace: TraceStep[]): TraceStep[] {
  const failing = trace.filter((s) => s.met !== true && !NON_BLOCKING.has(s.rule))
  const key = trace.filter((s) => s.met === true && (KEY_RULES.has(s.rule) || s.rule.startsWith('stawka_')) && s.rule !== 'stawka_dzienna')
  const chosen = new Set([...failing, ...key].slice(0, KEY_STEP_LIMIT))
  return trace.filter((s) => chosen.has(s))
}

/** Trace kroki, które warto pokazać na wierzchu karty (np. prawo ojca mimo braku ubezpieczenia matki). */
export function highlightedSteps(benefit: BenefitResult): TraceStep[] {
  return benefit.trace
    .filter((s) => s.rule === 'matka_nieubezpieczona_ojciec_ubezpieczony' && s.met === true)
    .filter((s, i, all) => all.findIndex((x) => x.text === s.text) === i)
}

/** Ten sam termin (id, data, treść) przy kilku świadczeniach pokazujemy raz, z listą świadczeń. */
export interface MergedDeadline {
  key: string
  entry: DeadlineEntry
  benefits: BenefitResult[]
}

export function mergeDeadlines(entries: DeadlineEntry[]): MergedDeadline[] {
  const map = new Map<string, MergedDeadline>()
  for (const entry of entries) {
    const key = `${entry.deadline.id}|${entry.deadline.date}|${entry.deadline.text}|${entry.benefit.person}`
    const found = map.get(key)
    if (found) found.benefits.push(entry.benefit)
    else map.set(key, { key, entry, benefits: [entry.benefit] })
  }
  return [...map.values()]
}

/** Ryzyka, które powtarzają termin widoczny już w „Co zrobić i do kiedy”. */
const RISK_DUPLICATES_DEADLINE: Record<string, string> = {
  termin_21_dni: 'wniosek_usredniona',
  niewykorzystany_ojcowski: 'wykorzystanie_ojcowskiego',
  zadluzenie_skladkowe: 'splata_zadluzenia',
}

export function riskDuplicatesDeadline(riskId: string, entries: DeadlineEntry[]): boolean {
  const deadlineId = RISK_DUPLICATES_DEADLINE[riskId]
  return deadlineId !== undefined && entries.some((e) => e.deadline.id === deadlineId)
}

/** Krótkie objaśnienia skrótów i pojęć, które pojawiają się na wyniku. */
export const glossary: { term: string; test: RegExp; text: string }[] = [
  { term: 'ZAM', test: /\bZAM\b/, text: 'wniosek o zasiłek macierzyński. Składasz go u płatnika składek albo w ZUS — w karcie świadczenia piszemy, gdzie.' },
  { term: 'Płatnik składek', test: /płatnik\w* składek/i, text: 'ten, kto zgłasza Cię do ubezpieczeń i płaci za Ciebie składki — zwykle pracodawca albo zleceniodawca; przy własnej działalności to Ty.' },
  { term: 'e-ZLA', test: /e-ZLA/, text: 'elektroniczne zwolnienie lekarskie (L4). Lekarz wystawia je w systemie, a ZUS i pracodawca widzą je od razu.' },
  { term: 'PUE/eZUS', test: /PUE|eZUS/, text: 'Twoje konto w serwisie internetowym ZUS. Sprawdzisz tam składki, saldo i złożysz wnioski.' },
  { term: 'Podstawa wymiaru zasiłku', test: /podstaw\w* wymiaru/i, text: 'kwota, od której liczy się zasiłek — zwykle przeciętne wynagrodzenie z 12 miesięcy po odliczeniu składek na ubezpieczenia społeczne.' },
  { term: 'Z-3 / Z-3a', test: /\bZ-3a?\b/, text: 'zaświadczenie płatnika składek o Twoim wynagrodzeniu. Wystawia je pracodawca (Z-3) albo zleceniodawca (Z-3a) i przekazuje do ZUS.' },
  { term: 'Przedawnienie', test: /przedawni/i, text: 'termin, po którym nie można już domagać się wypłaty zasiłku za dany okres.' },
]

/** Etykieta przycisku z objaśnionym skrótem przy pierwszym użyciu. */
export function explainLabel(label: string): string {
  return label.replace('wniosku ZAM', 'wniosku o zasiłek macierzyński (ZAM)')
}
