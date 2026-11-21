import type { ContributionBase, FactSet, InsuranceTitle, ParentRole, PersonRef } from '@/app/api/types'
import { getFact, userFactOf, withFact } from '@/features/review/facts'

/**
 * Odpowiedzi z kroku „Praca i ubezpieczenie” (tryb „Wprowadź dane ręcznie”) zamienione na fakty silnika.
 * Czysta funkcja — bez Reacta i bez stanu przebiegu, żeby dało się ją sprawdzić osobno.
 */

export type WorkForm = 'pracownik' | 'zleceniobiorca' | 'dzialalnosc' | 'brak'

export const workForms: WorkForm[] = ['pracownik', 'zleceniobiorca', 'dzialalnosc', 'brak']

export interface WorkInput {
  forma: WorkForm
  /** Początek umowy lub działalności (RRRR-MM-DD). Pomijany dla „brak”. */
  od?: string
  /** Dobrowolne chorobowe — tylko zlecenie i działalność; pracownik ma zawsze obowiązkowe. */
  chorobowe?: boolean
  czasOkreslony?: boolean
  umowaDo?: string
  wymiarEtatu?: number
  /** Kwota brutto miesięcznie (dla działalności: zadeklarowana podstawa składek). */
  kwota?: number
  /** Czy płatnik zgłasza więcej niż 20 osób; null = „nie wiem” (fakt nie jest ustawiany). */
  ponad20?: boolean | null
  /** Zaległe składki (zł) — tylko działalność; 0 = brak zaległości. */
  zalegleSkladki?: number
}

export interface ManualInput {
  /** Dzień oceny (RRRR-MM-DD) — dzisiaj. */
  today: string
  /** Data zdarzenia z pierwszego kroku (może jej nie być, gdy ktoś dopiero planuje). */
  eventDate?: string
  rola: ParentRole
  dataUrodzenia?: string
  ja: WorkInput
  /** Tylko gdy liczymy „dla mnie i drugiego rodzica”. Brak = dane drugiego rodzica z tego kroku są usuwane. */
  drugi?: WorkInput
}

/** Fakty osoby, którymi zarządza ten krok — przy każdym zapisie są ustawiane od nowa. */
export const managedWorkFacts = [
  'ubezpieczenie.tytuly',
  'ubezpieczenie.podstawySkladek',
  'umowa.naCzasOkreslony',
  'umowa.dataKonca',
  'platnik.liczbaUbezpieczonych',
  'skladki.zadluzenie',
] as const

/** Liczba osób zgłaszanych przez płatnika, która rozstrzyga odpowiedź „więcej niż 20” / „20 lub mniej”. */
const HEADCOUNT_ABOVE = 21
const HEADCOUNT_UP_TO = 20

/** Ile miesięcy przed okresem obliczeniowym dokładamy, gdy prawo powstaje wcześniej niż zdarzenie (np. urlop przed porodem). */
const EXTRA_MONTHS_BEFORE = 2

const pad = (n: number) => String(n).padStart(2, '0')

/** „RRRR-MM” przesunięty o `delta` miesięcy. */
export function shiftMonth(month: string, delta: number): string {
  const [y, m] = month.split('-').map(Number)
  const index = y * 12 + (m - 1) + delta
  return `${Math.floor(index / 12)}-${pad((index % 12) + 1)}`
}

/** Kwota wpisana po polsku („6 500,50”) albo null, gdy to nie jest liczba. */
export function parseAmount(text: string): number | null {
  const normalized = text.replace(/\s/g, '').replace(',', '.')
  return /^\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : null
}

export const oppositeRole = (rola: ParentRole): ParentRole => (rola === 'matka' ? 'ojciec' : 'matka')

/**
 * Miesiące podstaw składek. Silnik liczy podstawę z 12 pełnych miesięcy przed miesiącem powstania prawa,
 * a gdy prawo powstanie w przyszłości — przed dniem oceny (api/Domain/Catalog/ModuleSteps.cs). Bierzemy więc
 * 12 miesięcy przed miesiącem zdarzenia albo dzisiejszym (wcześniejszym z nich) i dwa dodatkowe wcześniej.
 * Miesiąc liczy się dopiero od pierwszego pełnego miesiąca ubezpieczenia (pierwszy dzień miesiąca ≥ „od”).
 */
export function contributionMonths(od: string, kwota: number, today: string, eventDate?: string): ContributionBase[] {
  const reference = eventDate && eventDate < today ? eventDate : today
  const referenceMonth = reference.slice(0, 7)
  const months: ContributionBase[] = []
  for (let k = 12 + EXTRA_MONTHS_BEFORE; k >= 1; k -= 1) {
    const miesiac = shiftMonth(referenceMonth, -k)
    if (`${miesiac}-01` >= od) months.push({ miesiac, kwota })
  }
  return months
}

function titleOf(work: WorkInput): InsuranceTitle | null {
  if (work.forma === 'brak' || !work.od) return null
  const employee = work.forma === 'pracownik'
  return {
    rodzajTytulu: work.forma,
    od: work.od,
    do: null,
    chorobowe: employee ? 'obowiazkowe' : work.chorobowe ? 'dobrowolne' : 'brak',
    ...(employee ? { wymiarEtatu: work.wymiarEtatu ?? 1 } : {}),
  }
}

function applyWork(facts: FactSet, person: PersonRef, work: WorkInput, today: string, eventDate?: string): FactSet {
  const title = titleOf(work)
  const set = (next: FactSet, key: string, value: unknown) => withFact(next, person, key, value === undefined ? undefined : userFactOf(value))
  // Pusta lista tytułów = silnik uznaje brak ubezpieczenia (api/Domain/Eligibility/Insurance.cs: NotInsured).
  let next = set(facts, 'ubezpieczenie.tytuly', title ? [title] : [])
  next = set(next, 'ubezpieczenie.podstawySkladek', title && work.kwota !== undefined ? contributionMonths(title.od, work.kwota, today, eventDate) : undefined)

  const hasContract = work.forma === 'pracownik' || work.forma === 'zleceniobiorca'
  next = set(next, 'umowa.naCzasOkreslony', hasContract ? work.czasOkreslony : undefined)
  next = set(next, 'umowa.dataKonca', hasContract && work.czasOkreslony && work.umowaDo ? work.umowaDo : undefined)
  const headcount = hasContract && typeof work.ponad20 === 'boolean' ? (work.ponad20 ? HEADCOUNT_ABOVE : HEADCOUNT_UP_TO) : undefined
  next = set(next, 'platnik.liczbaUbezpieczonych', headcount)
  // Saldo zadłużenia sprawdza silnik tylko przy działalności (art. 2a); pytamy o stan na dziś.
  const debt = work.forma === 'dzialalnosc' && work.zalegleSkladki !== undefined ? { naDzien: today, kwota: work.zalegleSkladki } : undefined
  return set(next, 'skladki.zadluzenie', debt)
}

/** Ustawia fakty z kroku „Praca i ubezpieczenie” (wszystkie ze źródłem 'user'). Pozostałe fakty zostają bez zmian. */
export function applyManualFacts(facts: FactSet, input: ManualInput): FactSet {
  let next = withFact(facts, 'wnioskodawca', 'osoba.rola', userFactOf(input.rola))
  next = withFact(next, 'wnioskodawca', 'osoba.dataUrodzenia', input.dataUrodzenia ? userFactOf(input.dataUrodzenia) : undefined)
  next = applyWork(next, 'wnioskodawca', input.ja, input.today, input.eventDate)

  if (input.drugi) {
    next = withFact(next, 'drugiRodzic', 'osoba.rola', userFactOf(oppositeRole(input.rola)))
    return applyWork(next, 'drugiRodzic', input.drugi, input.today, input.eventDate)
  }

  if (!next.drugiRodzic) return next
  for (const key of ['osoba.rola', ...managedWorkFacts]) {
    if (getFact(next, 'drugiRodzic', key)?.source === 'user') next = withFact(next, 'drugiRodzic', key, undefined)
  }
  if (Object.keys(next.drugiRodzic ?? {}).length === 0) {
    const rest = { ...next }
    delete rest.drugiRodzic
    return rest
  }
  return next
}

/** Odpowiedzi formularza (tekstowe) odtworzone z zapisanych faktów — żeby po powrocie do kroku pola były wypełnione. */
export interface WorkAnswers {
  forma: string
  od: string
  chorobowe: string
  czasOkreslony: string
  umowaDo: string
  etat: string
  kwota: string
  ponad20: string
  zaleglosci: string
  zaleglosciKwota: string
}

const yesNo = (value: unknown) => (value === true ? 'tak' : value === false ? 'nie' : '')

export function workAnswersFrom(facts: FactSet, person: PersonRef): WorkAnswers {
  const titles = getFact(facts, person, 'ubezpieczenie.tytuly')?.value as InsuranceTitle[] | undefined
  const bases = getFact(facts, person, 'ubezpieczenie.podstawySkladek')?.value as ContributionBase[] | undefined
  const headcount = getFact(facts, person, 'platnik.liczbaUbezpieczonych')?.value as number | undefined
  const debt = getFact(facts, person, 'skladki.zadluzenie')?.value as { kwota: number } | undefined
  const title = titles?.[0]
  const known = title && (workForms as string[]).includes(title.rodzajTytulu)
  const lastBase = bases && bases.length > 0 ? [...bases].sort((a, b) => a.miesiac.localeCompare(b.miesiac))[bases.length - 1] : undefined
  return {
    forma: titles && titles.length === 0 ? 'brak' : known ? title.rodzajTytulu : '',
    od: known ? title.od : '',
    chorobowe: known && title.rodzajTytulu !== 'pracownik' ? yesNo(title.chorobowe === 'dobrowolne') : '',
    czasOkreslony: yesNo(getFact(facts, person, 'umowa.naCzasOkreslony')?.value),
    umowaDo: String(getFact(facts, person, 'umowa.dataKonca')?.value ?? ''),
    etat: known && title.wymiarEtatu !== undefined ? String(title.wymiarEtatu) : '1',
    kwota: lastBase ? String(lastBase.kwota).replace('.', ',') : '',
    ponad20: headcount === undefined ? (known ? 'nie_wiem' : '') : headcount > HEADCOUNT_UP_TO ? 'tak' : 'nie',
    zaleglosci: debt ? yesNo(debt.kwota > 0) : '',
    zaleglosciKwota: debt && debt.kwota > 0 ? String(debt.kwota).replace('.', ',') : '',
  }
}

/** Odpowiedzi tekstowe (po walidacji formularza) zamienione na wejście `applyManualFacts`. */
export function workInputFrom(a: WorkAnswers): WorkInput {
  const forma = a.forma as WorkForm
  if (forma === 'brak') return { forma }
  const contract = forma === 'pracownik' || forma === 'zleceniobiorca'
  const czasOkreslony = contract && a.czasOkreslony ? a.czasOkreslony === 'tak' : undefined
  return {
    forma,
    od: a.od,
    chorobowe: forma === 'pracownik' ? undefined : a.chorobowe === 'tak',
    czasOkreslony,
    umowaDo: czasOkreslony && a.umowaDo ? a.umowaDo : undefined,
    wymiarEtatu: forma === 'pracownik' ? Number(a.etat) || 1 : undefined,
    kwota: parseAmount(a.kwota) ?? undefined,
    ponad20: contract ? (a.ponad20 === 'tak' ? true : a.ponad20 === 'nie' ? false : null) : undefined,
    zalegleSkladki: forma === 'dzialalnosc' && a.zaleglosci ? (a.zaleglosci === 'tak' ? (parseAmount(a.zaleglosciKwota) ?? undefined) : 0) : undefined,
  }
}
