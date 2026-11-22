import { z } from 'zod'
import type {
  FactSet,
  InsuranceTitle,
  PaymentVariant,
  PersonFacts,
  PersonRef,
  ScenarioChange,
  ScenarioId,
} from '@/app/api/types'

export interface ScenarioInfo {
  id: ScenarioId
  title: string
  description: string
}

export const scenarioList: ScenarioInfo[] = [
  {
    id: 'odchodze_z_pracy',
    title: 'Odchodzę z pracy',
    description: 'Sprawdź, co się zmieni, jeśli Twoje ubezpieczenie chorobowe skończy się w wybranym dniu.',
  },
  {
    id: 'umowa_nie_zostanie_przedluzona',
    title: 'Umowa nie zostanie przedłużona',
    description: 'Sprawdź, co się zmieni, jeśli umowa na czas określony skończy się w planowanym terminie i nikt jej nie przedłuży.',
  },
  {
    id: 'przechodze_na_dzialalnosc',
    title: 'Przechodzę na działalność',
    description: 'Sprawdź skutki zmiany pracy na własną działalność, z dobrowolnym ubezpieczeniem chorobowym albo bez niego.',
  },
  {
    id: 'drugi_rodzic_bierze',
    title: 'Drugi rodzic bierze część urlopu rodzicielskiego',
    description: 'Wskaż, kto bierze ile tygodni urlopu rodzicielskiego (reszta przypada drugiemu rodzicowi), i zobacz, jak zmienią się okresy i kwoty.',
  },
  {
    id: 'praca_na_czesc_etatu',
    title: 'Praca na część etatu w czasie urlopu rodzicielskiego',
    description: 'Pracuj najwyżej na pół etatu i zobacz, o ile wydłuży się urlop i jak zmieni się zasiłek.',
  },
  {
    id: 'wariant_wyplaty',
    title: 'Wariant wypłaty',
    description: 'Porównaj wypłatę standardową (100% i 70%) z uśrednioną (81,5% w całym okresie).',
  },
  {
    id: 'splacam_zadluzenie',
    title: 'Spłacam zadłużenie',
    description: 'Sprawdź, co się zmieni, jeśli spłacisz zaległe składki w wybranym dniu.',
  },
]

const workTitles = ['pracownik', 'zleceniobiorca']

const titlesOf = (person: PersonFacts | undefined): InsuranceTitle[] | undefined => person?.['ubezpieczenie.tytuly']?.value

function hasWork(person: PersonFacts | undefined): boolean {
  const titles = titlesOf(person)
  if (titles === undefined) return true
  return titles.some((t) => workTitles.includes(t.rodzajTytulu) && !t.do)
}

export function personsInCalculation(facts: FactSet): PersonRef[] {
  return facts.sprawa['zdarzenie.dlaKogo']?.value === 'ja_i_drugi_rodzic' ? ['wnioskodawca', 'drugiRodzic'] : ['wnioskodawca']
}

function reasonUnavailable(id: ScenarioId, facts: FactSet, person: PersonRef): string | null {
  const p = facts[person]
  switch (id) {
    case 'odchodze_z_pracy':
      return hasWork(p) ? null : 'Nie widzimy umowy o pracę ani zlecenia, z której dałoby się odejść.'
    case 'umowa_nie_zostanie_przedluzona':
      return p?.['umowa.naCzasOkreslony']?.value === true ? null : 'Dotyczy tylko umowy na czas określony, a w danych jej nie ma.'
    case 'przechodze_na_dzialalnosc':
      return hasWork(p) ? null : 'Nie widzimy umowy o pracę ani zlecenia, z której dałoby się przejść na działalność.'
    case 'praca_na_czesc_etatu': {
      const titles = titlesOf(p)
      return titles !== undefined && titles.length === 0 ? 'Nie widzimy tytułu ubezpieczenia, z którym dałoby się łączyć pracę z urlopem.' : null
    }
    case 'splacam_zadluzenie': {
      const debt = p?.['skladki.zadluzenie']?.value
      return debt && debt.kwota > 0 ? null : 'W danych nie ma zadłużenia składkowego do spłaty.'
    }
    default:
      return null
  }
}

export interface ScenarioAvailability {
  id: ScenarioId
  persons: PersonRef[]
  reason: string | null
}

export function availability(id: ScenarioId, facts: FactSet): ScenarioAvailability {
  const scope = personsInCalculation(facts)
  if (id === 'drugi_rodzic_bierze') {
    return { id, persons: scope.length > 1 ? scope : ['drugiRodzic'], reason: null }
  }
  const results = scope.map((person) => ({ person, reason: reasonUnavailable(id, facts, person) }))
  const ok = results.filter((r) => r.reason === null).map((r) => r.person)
  return { id, persons: ok, reason: ok.length > 0 ? null : (results[0]?.reason ?? null) }
}

export function totalLeaveWeeks(facts: FactSet): number {
  const many = (facts.sprawa['dziecko.liczba']?.value ?? 1) > 1
  if (facts.sprawa['dziecko.zaZyciem']?.value === true) return many ? 67 : 65
  return many ? 43 : 41
}

export const EXCLUSIVE_WEEKS = 9

export interface FormValues {
  person: string
  data: string
  dobrowolne: boolean
  tygodnie: string
  wymiar: string
  wariant: string
}

export const emptyValues: FormValues = { person: '', data: '', dobrowolne: false, tygodnie: '', wymiar: '', wariant: 'usredniony' }

export const parseNumber = (text: string): number => Number(text.trim().replace(',', '.'))

function isValidDate(text: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) return false
  const d = new Date(`${text}T00:00:00Z`)
  if (Number.isNaN(d.getTime()) || d.toISOString().slice(0, 10) !== text) return false
  const year = d.getUTCFullYear()
  return year >= 2000 && year <= 2100
}

export const primaryField: Record<ScenarioId, keyof FormValues | null> = {
  odchodze_z_pracy: 'data',
  umowa_nie_zostanie_przedluzona: null,
  przechodze_na_dzialalnosc: 'data',
  drugi_rodzic_bierze: 'tygodnie',
  praca_na_czesc_etatu: 'tygodnie',
  wariant_wyplaty: 'wariant',
  splacam_zadluzenie: 'data',
}

export function buildSchema(id: ScenarioId, totalWeeks: number) {
  return z
    .object({ person: z.string(), data: z.string(), dobrowolne: z.boolean(), tygodnie: z.string(), wymiar: z.string(), wariant: z.string() })
    .superRefine((v, ctx) => {
      const fail = (path: keyof FormValues, message: string) => ctx.addIssue({ code: 'custom', path: [path], message })

      if (id === 'odchodze_z_pracy' || id === 'przechodze_na_dzialalnosc' || id === 'splacam_zadluzenie') {
        if (v.data.trim() === '') fail('data', 'Podaj datę.')
        else if (!isValidDate(v.data)) fail('data', 'Ta data jest nieprawidłowa lub spoza lat 2000–2100.')
      }

      if (id === 'drugi_rodzic_bierze' || id === 'praca_na_czesc_etatu') {
        const text = v.tygodnie.trim()
        const weeks = Number(text)
        if (text === '') fail('tygodnie', 'Podaj liczbę tygodni.')
        else if (!/^\d+$/.test(text) || weeks < 1) fail('tygodnie', 'Liczba tygodni musi być całkowita i większa od zera.')
        else if (weeks > totalWeeks) fail('tygodnie', `Urlop rodzicielski trwa łącznie najwyżej ${totalWeeks} tygodni.`)
      }

      if (id === 'praca_na_czesc_etatu') {
        const text = v.wymiar.trim()
        const size = parseNumber(text)
        if (text === '') fail('wymiar', 'Podaj wymiar etatu.')
        else if (!Number.isFinite(size) || size <= 0 || size > 0.5) fail('wymiar', 'Wymiar etatu musi być większy od 0 i najwyżej 0,5 (pół etatu).')
      }

      if (id === 'wariant_wyplaty' && v.wariant !== 'standardowy' && v.wariant !== 'usredniony') fail('wariant', 'Wybierz jeden z wariantów wypłaty.')
    })
}

export function buildChange(id: ScenarioId, v: FormValues, person: PersonRef | undefined): ScenarioChange {
  const change: ScenarioChange = { id }
  if (person) change.person = person
  switch (id) {
    case 'odchodze_z_pracy':
    case 'splacam_zadluzenie':
      change.params = { data: v.data }
      break
    case 'przechodze_na_dzialalnosc':
      change.params = { data: v.data, dobrowolneChorobowe: v.dobrowolne }
      break
    case 'drugi_rodzic_bierze':
      change.params = { tygodnie: Number(v.tygodnie) }
      break
    case 'praca_na_czesc_etatu':
      change.params = { wymiar: parseNumber(v.wymiar), tygodnie: Number(v.tygodnie) }
      break
    case 'wariant_wyplaty':
      change.params = { wariant: v.wariant as PaymentVariant }
      break
    case 'umowa_nie_zostanie_przedluzona':
      break
  }
  return change
}

export function valuesFromChange(change: ScenarioChange): FormValues {
  const p = change.params ?? {}
  return {
    person: change.person ?? '',
    data: p.data ?? '',
    dobrowolne: p.dobrowolneChorobowe ?? false,
    tygodnie: p.tygodnie !== undefined ? String(p.tygodnie) : '',
    wymiar: p.wymiar !== undefined ? String(p.wymiar).replace('.', ',') : '',
    wariant: p.wariant ?? 'usredniony',
  }
}

const relatedFacts: Record<ScenarioId, string[]> = {
  odchodze_z_pracy: ['umowa.planowaneOdejscieData'],
  umowa_nie_zostanie_przedluzona: [],
  przechodze_na_dzialalnosc: ['umowa.planowaneOdejscieData', 'ubezpieczenie.tytuly'],
  drugi_rodzic_bierze: ['plan.rodzicielskiCzesci'],
  praca_na_czesc_etatu: ['plan.pracaNaCzescEtatu'],
  wariant_wyplaty: ['plan.wariantWyplaty', 'plan.dataWnioskuUsrednionego'],
  splacam_zadluzenie: ['skladki.dataSplaty'],
}

export function isFieldIssue(id: ScenarioId, fact: string | null | undefined): boolean {
  return primaryField[id] !== null && (!fact || relatedFacts[id].includes(fact))
}
