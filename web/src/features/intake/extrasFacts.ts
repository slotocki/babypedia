import { z } from 'zod'
import type { FactSet, InsuranceTitle, LeavePart, PartTimeWork, PersonRef, SickLeave } from '@/app/api/types'
import { isValidIsoDate } from '@/features/review/dates'
import { getFact, userFactOf, withFact } from '@/features/review/facts'

/**
 * Dodatkowe dane z trybu „Wprowadź dane ręcznie”: koniec tytułu, kolejne tytuły ubezpieczenia, zwolnienia e-ZLA,
 * podział urlopu rodzicielskiego, praca na część etatu i rezygnacja matki z części urlopu macierzyńskiego.
 * Czyste funkcje (bez Reacta) — formularz trzyma odpowiedzi jako tekst, tu zamieniamy je na fakty silnika i z powrotem.
 */

export const extraTitleForms = ['pracownik', 'zleceniobiorca', 'dzialalnosc', 'inny'] as const
export const sicknessValues = ['obowiazkowe', 'dobrowolne', 'brak'] as const
export const payerValues = ['ten_sam', 'inny'] as const

export const MAX_EXTRA_TITLES = 4
export const MAX_SICK_LEAVES = 10
/** Limit części sprawdza też silnik (parametr rodzicielski_max_czesci); tu tylko ograniczamy formularz. */
export const MAX_LEAVE_PARTS = 5
export const MAX_PART_TIME = 5
const MAX_WEEKS = 52

const extraTitleSchema = z.object({ forma: z.string(), od: z.string(), do: z.string(), chorobowe: z.string(), etat: z.string(), platnik: z.string() })
const sickLeaveSchema = z.object({ od: z.string(), do: z.string(), kodB: z.boolean(), szpital: z.boolean() })
const leavePartSchema = z.object({ tygodnie: z.string(), od: z.string() })
const partTimeSchema = z.object({ wymiar: z.string(), tygodnie: z.string(), od: z.string() })

export const extrasSchema = z.object({
  tytulDo: z.string(),
  tytuly: z.array(extraTitleSchema),
  zwolnienia: z.array(sickLeaveSchema),
  czesci: z.array(leavePartSchema),
  pracaCzesc: z.array(partTimeSchema),
  rezygnacja: z.boolean(),
  rezygnacjaPoTyg: z.string(),
})

export type ExtraTitleAnswers = z.infer<typeof extraTitleSchema>
export type SickLeaveAnswers = z.infer<typeof sickLeaveSchema>
export type LeavePartAnswers = z.infer<typeof leavePartSchema>
export type PartTimeAnswers = z.infer<typeof partTimeSchema>
export type ExtrasAnswers = z.infer<typeof extrasSchema>

export const emptyExtraTitle = (): ExtraTitleAnswers => ({ forma: '', od: '', do: '', chorobowe: '', etat: '1', platnik: '' })
export const emptySickLeave = (): SickLeaveAnswers => ({ od: '', do: '', kodB: true, szpital: false })
export const emptyLeavePart = (): LeavePartAnswers => ({ tygodnie: '', od: '' })
export const emptyPartTime = (): PartTimeAnswers => ({ wymiar: '', tygodnie: '', od: '' })

/** Fakty osoby, którymi zarządzają dodatkowe sekcje. Brak odpowiedzi usuwa tylko fakty wpisane wcześniej przez użytkownika. */
export const managedExtraFacts = ['ezla.zwolnienia', 'plan.rodzicielskiCzesci', 'plan.pracaNaCzescEtatu', 'plan.rezygnacjaZMacierzynskiegoPoTyg'] as const

/** Ułamek etatu wpisany po polsku: „1”, „0,63”, „0.5” albo „5/8”. Null, gdy to nie jest liczba z przedziału (0; 1]. */
export function parseFraction(text: string): number | null {
  const t = text.replace(/\s/g, '')
  const ratio = /^(\d+)\/(\d+)$/.exec(t)
  let value: number
  if (ratio) {
    const denominator = Number(ratio[2])
    if (denominator === 0) return null
    value = Number(ratio[1]) / denominator
  } else {
    const normalized = t.replace(',', '.')
    if (!/^\d+(\.\d+)?$/.test(normalized)) return null
    value = Number(normalized)
  }
  return value > 0 && value <= 1 ? Math.round(value * 10000) / 10000 : null
}

export const formatFraction = (value: number) => String(value).replace('.', ',')

function parseWeeks(text: string): number | null {
  const t = text.trim()
  if (!/^\d+$/.test(t)) return null
  const n = Number(t)
  return n >= 1 && n <= MAX_WEEKS ? n : null
}

export const emptyExtras = (): ExtrasAnswers => ({ tytulDo: '', tytuly: [], zwolnienia: [], czesci: [], pracaCzesc: [], rezygnacja: false, rezygnacjaPoTyg: '' })

/** Odpowiedzi odtworzone z zapisanych faktów — żeby po powrocie do kroku sekcje były wypełnione. */
export function extrasAnswersFrom(facts: FactSet, person: PersonRef): ExtrasAnswers {
  const value = <T,>(key: string) => getFact(facts, person, key)?.value as T | undefined
  const titles = value<InsuranceTitle[]>('ubezpieczenie.tytuly') ?? []
  const main = titles[0]
  const leaves = value<SickLeave[]>('ezla.zwolnienia') ?? []
  const parts = value<LeavePart[]>('plan.rodzicielskiCzesci') ?? []
  const partTime = value<PartTimeWork[]>('plan.pracaNaCzescEtatu') ?? []
  const resignation = value<number>('plan.rezygnacjaZMacierzynskiegoPoTyg')
  return {
    tytulDo: main?.do ?? '',
    tytuly: titles.slice(1).map((t) => {
      const forma = (extraTitleForms as readonly string[]).includes(t.rodzajTytulu) ? t.rodzajTytulu : 'inny'
      return {
        forma,
        od: t.od,
        do: t.do ?? '',
        chorobowe: forma === 'pracownik' ? 'obowiazkowe' : t.chorobowe,
        etat: t.wymiarEtatu !== undefined ? formatFraction(t.wymiarEtatu) : '1',
        platnik: forma === 'pracownik' && main?.rodzajTytulu === 'pracownik' ? ((t.platnikId ?? '') === (main.platnikId ?? '') ? 'ten_sam' : 'inny') : '',
      }
    }),
    zwolnienia: leaves.map((l) => ({ od: l.od, do: l.do, kodB: (l.kod ?? '').toUpperCase() === 'B', szpital: l.szpital })),
    czesci: parts.map((p) => ({ tygodnie: String(p.tygodnie), od: p.od ?? '' })),
    pracaCzesc: partTime.map((p) => ({ wymiar: formatFraction(p.wymiar), tygodnie: String(p.tygodnie), od: p.od ?? '' })),
    rezygnacja: resignation !== undefined,
    rezygnacjaPoTyg: resignation !== undefined ? String(resignation) : '',
  }
}

/** Co pokazujemy i zapisujemy — zależy od zdarzenia i od tego, czy osoba jest matką. */
export interface ExtrasScope {
  rodzaj: string
  mother: boolean
  /**
   * Wszystkie dzieci z porodu urodziły się martwe. Silnik nie liczy wtedy urlopu rodzicielskiego
   * (api/Domain/Periods/PeriodsService.cs, Rodzicielski), a urlop macierzyński trwa najwyżej 8 tygodni po porodzie
   * (KP art. 180¹), więc rezygnacja po 14 tygodniach i podział urlopu rodzicielskiego nie mają zastosowania.
   */
  stillborn?: boolean
}

export const showsSickLeaves = (s: ExtrasScope) => s.mother && (s.rodzaj === 'ciaza' || s.rodzaj === 'urodzenie')
export const showsLeavePlan = (s: ExtrasScope) => !s.stillborn && (s.rodzaj === 'ciaza' || s.rodzaj === 'urodzenie' || s.rodzaj === 'przyjecie')
export const showsResignation = (s: ExtrasScope) => !s.stillborn && s.mother && (s.rodzaj === 'ciaza' || s.rodzaj === 'urodzenie')

/**
 * Pierwszy tytuł (z głównych pytań) uzupełniony o datę końca i lista kolejnych tytułów.
 * Syntetyczny platnikId („p1”, „p2”…) ustawiamy tylko przy kilku umowach o pracę: ta sama wartość = ten sam pracodawca.
 */
export function titlesWithExtras(main: InsuranceTitle, extras: ExtrasAnswers | undefined): InsuranceTitle[] {
  if (!extras) return [main]
  const first: InsuranceTitle = { ...main, do: extras.tytulDo || null }
  const employment = main.rodzajTytulu === 'pracownik' && extras.tytuly.some((t) => t.forma === 'pracownik')
  if (employment) first.platnikId = 'p1'
  let nextPayer = 2
  const rest = extras.tytuly.map((t): InsuranceTitle => {
    const employee = t.forma === 'pracownik'
    const title: InsuranceTitle = {
      rodzajTytulu: t.forma as InsuranceTitle['rodzajTytulu'],
      od: t.od,
      do: t.do || null,
      chorobowe: employee ? 'obowiazkowe' : (t.chorobowe as InsuranceTitle['chorobowe']),
    }
    if (employee) {
      title.wymiarEtatu = parseFraction(t.etat) ?? 1
      title.platnikId = employment && t.platnik === 'ten_sam' ? 'p1' : `p${nextPayer++}`
    }
    return title
  })
  return [first, ...rest]
}

/** Zapisuje zwolnienia i plan urlopów (źródło 'user'). Sekcja pusta lub niewidoczna usuwa tylko fakty wpisane przez użytkownika. */
export function applyExtraFacts(facts: FactSet, person: PersonRef, extras: ExtrasAnswers | undefined, scope: ExtrasScope): FactSet {
  const set = (current: FactSet, key: string, value: unknown) =>
    value !== undefined
      ? withFact(current, person, key, userFactOf(value))
      : getFact(current, person, key)?.source === 'user'
        ? withFact(current, person, key, undefined)
        : current

  const leaves: SickLeave[] | undefined =
    extras && showsSickLeaves(scope) && extras.zwolnienia.length > 0
      ? extras.zwolnienia.map((l) => ({ od: l.od, do: l.do, kod: l.kodB ? 'B' : null, szpital: l.szpital }))
      : undefined
  const plan = extras && showsLeavePlan(scope)
  const parts: LeavePart[] | undefined =
    plan && extras.czesci.length > 0 ? extras.czesci.map((p) => ({ tygodnie: Number(p.tygodnie), ...(p.od ? { od: p.od } : {}) })) : undefined
  const partTime: PartTimeWork[] | undefined =
    plan && extras.pracaCzesc.length > 0
      ? extras.pracaCzesc.map((p) => ({ wymiar: parseFraction(p.wymiar) ?? 0, tygodnie: Number(p.tygodnie), ...(p.od ? { od: p.od } : {}) }))
      : undefined
  const resignation = extras && showsResignation(scope) && extras.rezygnacja ? Number(extras.rezygnacjaPoTyg) : undefined

  let next = set(facts, 'ezla.zwolnienia', leaves)
  next = set(next, 'plan.rodzicielskiCzesci', parts)
  next = set(next, 'plan.pracaNaCzescEtatu', partTime)
  return set(next, 'plan.rezygnacjaZMacierzynskiegoPoTyg', resignation)
}

type Issue = (path: string, message: string) => void

const DATE_HINT = 'Wpisz ją jako dzień, miesiąc i rok, np. 15.03.2027, albo wybierz z kalendarza.'

/** Walidacja dodatkowych sekcji; `path` to prefiks pola w formularzu (np. „praca.extras”). */
export function checkExtras(e: ExtrasAnswers, path: string, mainOd: string, mainForma: string, scope: ExtrasScope, issue: Issue) {
  const at = (field: string, message: string) => issue(`${path}.${field}`, message)
  const date = (value: string, field: string, required: boolean, label: string) => {
    if (!value) {
      if (required) at(field, `Podaj ${label}. ${DATE_HINT}`)
      return false
    }
    if (!isValidIsoDate(value)) {
      at(field, `Ta data wygląda nieprawidłowo. ${DATE_HINT}`)
      return false
    }
    return true
  }
  const range = (from: string, to: string, base: string, requiredTo: boolean, label: string) => {
    const okFrom = date(from, `${base}.od`, true, `datę początku ${label}`)
    if (date(to, `${base}.do`, requiredTo, `datę końca ${label}`) && okFrom && to < from)
      at(`${base}.do`, 'Data końca nie może być wcześniejsza niż data początku.')
  }

  if (date(e.tytulDo, 'tytulDo', false, '') && isValidIsoDate(mainOd) && e.tytulDo < mainOd)
    at('tytulDo', 'Data końca nie może być wcześniejsza niż data początku.')

  e.tytuly.forEach((t, i) => {
    const base = `tytuly.${i}`
    if (!(extraTitleForms as readonly string[]).includes(t.forma)) at(`${base}.forma`, 'Wybierz rodzaj tytułu ubezpieczenia.')
    range(t.od, t.do, base, false, 'tytułu')
    if (t.forma && t.forma !== 'pracownik' && !(sicknessValues as readonly string[]).includes(t.chorobowe))
      at(`${base}.chorobowe`, 'Wybierz, czy ten tytuł obejmuje ubezpieczenie chorobowe.')
    if (t.forma === 'pracownik') {
      if (parseFraction(t.etat) === null) at(`${base}.etat`, 'Wpisz wymiar etatu od 0 do 1, np. 1, 0,63 albo 5/8.')
      if (mainForma === 'pracownik' && !(payerValues as readonly string[]).includes(t.platnik)) at(`${base}.platnik`, 'Wybierz jedną z odpowiedzi.')
    }
  })

  if (showsSickLeaves(scope)) e.zwolnienia.forEach((l, i) => range(l.od, l.do, `zwolnienia.${i}`, true, 'zwolnienia'))

  if (showsLeavePlan(scope)) {
    e.czesci.forEach((p, i) => {
      if (parseWeeks(p.tygodnie) === null) at(`czesci.${i}.tygodnie`, `Wpisz liczbę tygodni od 1 do ${MAX_WEEKS}.`)
      date(p.od, `czesci.${i}.od`, false, '')
    })
    e.pracaCzesc.forEach((p, i) => {
      if (parseFraction(p.wymiar) === null) at(`pracaCzesc.${i}.wymiar`, 'Wpisz wymiar etatu od 0 do 1, np. 0,5 albo 1/2.')
      if (parseWeeks(p.tygodnie) === null) at(`pracaCzesc.${i}.tygodnie`, `Wpisz liczbę tygodni od 1 do ${MAX_WEEKS}.`)
      date(p.od, `pracaCzesc.${i}.od`, false, '')
    })
  }

  if (showsResignation(scope) && e.rezygnacja && parseWeeks(e.rezygnacjaPoTyg) === null)
    at('rezygnacjaPoTyg', `Wpisz, po ilu tygodniach od porodu kończysz urlop macierzyński (liczba od 1 do ${MAX_WEEKS}).`)
}
