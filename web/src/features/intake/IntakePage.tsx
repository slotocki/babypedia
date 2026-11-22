import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState, type FormEvent } from 'react'
import { useForm, type Control, type FieldErrors, type FieldPath, type UseFormRegister } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { z } from 'zod'
import type { EventKind, FactSet, MotherSituationKind, ParentRole } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { addDaysIso, isValidIsoDate, todayIso } from '@/features/review/dates'
import { calculationScopeLabels, eventKindLabels } from '@/features/review/fact-labels'
import { getFact, userFactOf, withFact } from '@/features/review/facts'
import { Button, Card, DateInput, Field, MoneyInput, NumberInput, PageHeader, RadioGroup, Select, TextInput, type ChoiceOption } from '@/ui'
import { applyManualFacts, parseAmount, workAnswersFrom, workInputFrom, type WorkAnswers } from './manualFacts'
import { useFocusHeading } from './useFocusHeading'
import { WorkExtras } from './WorkExtras'
import { checkExtras, extrasSchema, parseFraction } from './extrasFacts'

const eventKinds = Object.keys(eventKindLabels) as EventKind[]
const scopes = ['ja', 'ja_i_drugi_rodzic']
const bothParents = 'ja_i_drugi_rodzic'
const roles = ['matka', 'ojciec']
const workFormValues = ['pracownik', 'zleceniobiorca', 'dzialalnosc', 'brak']
const childStates = ['zyje', 'martwe', 'zmarlo']
const motherSituationValues = ['brak', 'pobyt_w_szpitalu', 'niezdolnosc_do_samodzielnej_egzystencji', 'zgon', 'porzucenie_dziecka']
/** Jak daleko wstecz przyjmujemy termin porodu przy „Jestem w ciąży” (sprawy z poprzednich lat). */
const PAST_PREGNANCY_YEARS = 5
/** Krok „Szczególne okoliczności” ma sens tylko przy ciąży, porodzie i przyjęciu dziecka. */
const hasCircumstances = (rodzaj: string) => rodzaj === 'ciaza' || rodzaj === 'urodzenie' || rodzaj === 'przyjecie'

const workSchema = z.object({
  forma: z.string(),
  od: z.string(),
  chorobowe: z.string(),
  czasOkreslony: z.string(),
  umowaDo: z.string(),
  etat: z.string(),
  kwota: z.string(),
  ponad20: z.string(),
  zaleglosci: z.string(),
  zaleglosciKwota: z.string(),
  extras: extrasSchema,
}) satisfies z.ZodType<WorkAnswers>

type Issue = (path: string, message: string) => void

function checkWork(w: WorkAnswers, prefix: 'praca' | 'drugi', issue: Issue, today: string, scope: { rodzaj: string; mother: boolean }) {
  const at = (field: keyof WorkAnswers, message: string) => issue(`${prefix}.${field}`, message)
  if (!workFormValues.includes(w.forma)) {
    at('forma', 'Wybierz formę pracy. Jeśli nie pracujesz, wybierz „Nie pracuję”.')
    return
  }
  if (w.forma === 'brak') return
  if (!w.od) at('od', 'Podaj datę początku umowy lub działalności. Wybierz ją z kalendarza albo wpisz w formacie rrrr-mm-dd.')
  else if (!isValidIsoDate(w.od)) at('od', `Ta data wygląda nieprawidłowo. Wpisz ją w formacie rrrr-mm-dd, np. ${today}.`)
  else if (w.od > today) at('od', 'Ta data jest z przyszłości. Podaj, od kiedy umowa lub działalność już trwa.')

  if (w.forma !== 'pracownik' && w.chorobowe !== 'tak' && w.chorobowe !== 'nie') at('chorobowe', 'Wybierz „Tak” albo „Nie”.')
  const contract = w.forma === 'pracownik' || w.forma === 'zleceniobiorca'
  if (contract) {
    if (w.czasOkreslony !== 'tak' && w.czasOkreslony !== 'nie') at('czasOkreslony', 'Wybierz „Tak” albo „Nie”.')
    if (w.czasOkreslony === 'tak' && w.umowaDo) {
      if (!isValidIsoDate(w.umowaDo)) at('umowaDo', 'Ta data wygląda nieprawidłowo. Wpisz ją w formacie rrrr-mm-dd albo zostaw pole puste.')
      else if (isValidIsoDate(w.od) && w.umowaDo < w.od) at('umowaDo', 'Umowa nie może skończyć się przed jej początkiem.')
    }
    if (!['tak', 'nie', 'nie_wiem'].includes(w.ponad20)) at('ponad20', 'Wybierz odpowiedź. Jeśli nie wiesz, wybierz „Nie wiem”.')
  }
  if (w.forma === 'pracownik' && parseFraction(w.etat) === null) at('etat', 'Wpisz wymiar etatu od 0 do 1, np. 1, 0,63 albo 5/8.')
  checkExtras(w.extras, `${prefix}.extras`, w.od, w.forma, scope, issue)

  const amount = parseAmount(w.kwota)
  if (amount === null || amount <= 0 || amount > 1_000_000) at('kwota', 'Wpisz kwotę w złotych, np. 6500 albo 6500,50.')

  if (w.forma === 'dzialalnosc') {
    if (w.zaleglosci !== 'tak' && w.zaleglosci !== 'nie') at('zaleglosci', 'Wybierz „Tak” albo „Nie”.')
    const debt = parseAmount(w.zaleglosciKwota)
    if (w.zaleglosci === 'tak' && (debt === null || debt <= 0)) at('zaleglosciKwota', 'Wpisz kwotę zaległości w złotych, np. 1200.')
  }
}

const schema = z
  .object({
    rodzaj: z.string(),
    data: z.string(),
    dataDziecka: z.string(),
    liczbaDzieci: z.string(),
    dlaKogo: z.string(),
    rola: z.string(),
    dataUrodzenia: z.string(),
    zaZyciem: z.string(),
    stanDziecka: z.string(),
    dataZgonu: z.string(),
    liczbaZmarlych: z.string(),
    szpitalDziecka: z.string(),
    szpitalOd: z.string(),
    szpitalDo: z.string(),
    tydzienCiazy: z.string(),
    sytuacjaMatki: z.string(),
    sytuacjaMatkiOd: z.string(),
    sytuacjaMatkiDo: z.string(),
    praca: workSchema,
    drugi: workSchema,
  })
  .superRefine((v, ctx) => {
    const issue: Issue = (path, message) => ctx.addIssue({ code: z.ZodIssueCode.custom, path: path.split('.'), message })
    const today = todayIso()
    const checkDate = (value: string, path: string, required: boolean, label: string) => {
      if (!value) {
        if (required) issue(path, `Podaj ${label}. Wybierz datę z kalendarza albo wpisz ją w formacie rrrr-mm-dd.`)
        return false
      }
      if (!isValidIsoDate(value)) {
        issue(path, `Ta data wygląda nieprawidłowo. Wybierz ją z kalendarza albo wpisz w formacie rrrr-mm-dd, np. ${today}.`)
        return false
      }
      return true
    }

    if (!eventKinds.includes(v.rodzaj as EventKind)) issue('rodzaj', 'Wybierz, co się dzieje — od tego zależą dalsze pytania.')
    if (!scopes.includes(v.dlaKogo)) issue('dlaKogo', 'Wybierz, dla kogo liczymy — dla Ciebie czy dla Ciebie i drugiego rodzica.')

    switch (v.rodzaj) {
      case 'planuje':
        if (checkDate(v.data, 'data', false, 'planowaną datę') && v.data < today)
          issue('data', 'Ta data już minęła. Podaj datę z przyszłości albo zostaw pole puste.')
        break
      case 'ciaza':
        if (checkDate(v.data, 'data', true, 'przewidywaną datę porodu')) {
          // Termin z przeszłości jest dozwolony (sprawy z ubiegłych lat): silnik liczy okresy od tej daty, a zwolnienia z ciąży wpisuje się w kroku o pracy.
          if (v.data < addDaysIso(today, -365 * PAST_PREGNANCY_YEARS))
            issue('data', `Ta data jest sprzed ponad ${PAST_PREGNANCY_YEARS} lat. Jeśli dziecko się urodziło, wybierz „Wróć”, a potem „Dziecko się urodziło”.`)
          else if (v.data > addDaysIso(today, 330))
            issue('data', 'Ta data jest zbyt odległa jak na termin porodu. Sprawdź ją w karcie ciąży.')
        }
        break
      case 'urodzenie':
        if (checkDate(v.data, 'data', true, 'datę urodzenia dziecka') && v.data > today)
          issue('data', 'Data urodzenia nie może być z przyszłości. Jeśli dziecko jeszcze się nie urodziło, wybierz „Jestem w ciąży”.')
        break
      case 'przyjecie': {
        const received = checkDate(v.data, 'data', true, 'datę przyjęcia dziecka')
        if (received && v.data > addDaysIso(today, 365)) issue('data', 'Ta data jest zbyt odległa. Sprawdź ją jeszcze raz.')
        const born = checkDate(v.dataDziecka, 'dataDziecka', true, 'datę urodzenia przyjmowanego dziecka')
        if (born && v.dataDziecka > today) issue('dataDziecka', 'Data urodzenia dziecka nie może być z przyszłości.')
        else if (born && received && v.dataDziecka > v.data)
          issue('dataDziecka', 'Dziecko nie mogło urodzić się po dacie przyjęcia. Sprawdź obie daty.')
        break
      }
    }

    const children = Number(v.liczbaDzieci)
    if (!/^\d+$/.test(v.liczbaDzieci.trim()) || children < 1 || children > 10) issue('liczbaDzieci', 'Wpisz liczbę dzieci od 1 do 10.')

    if (hasCircumstances(v.rodzaj) && v.zaZyciem !== 'tak' && v.zaZyciem !== 'nie') issue('zaZyciem', 'Wybierz „Tak” albo „Nie”.')
    if (v.rodzaj === 'urodzenie') {
      const born = isValidIsoDate(v.data) ? v.data : undefined
      if (!childStates.includes(v.stanDziecka)) issue('stanDziecka', 'Wybierz jedną z odpowiedzi.')
      if (v.stanDziecka === 'zmarlo' && checkDate(v.dataZgonu, 'dataZgonu', true, 'datę zgonu dziecka')) {
        if (v.dataZgonu > today) issue('dataZgonu', 'Data zgonu nie może być z przyszłości.')
        else if (born && v.dataZgonu < born) issue('dataZgonu', 'Data zgonu nie może być wcześniejsza niż data urodzenia.')
      }
      if (children > 1 && (v.stanDziecka === 'martwe' || v.stanDziecka === 'zmarlo')) {
        const dead = Number(v.liczbaZmarlych)
        if (!/^\d+$/.test(v.liczbaZmarlych.trim()) || dead < 1 || dead > children) issue('liczbaZmarlych', `Wpisz liczbę od 1 do ${children}.`)
      }
      if (v.szpitalDziecka !== 'tak' && v.szpitalDziecka !== 'nie') issue('szpitalDziecka', 'Wybierz „Tak” albo „Nie”.')
      if (v.szpitalDziecka === 'tak') {
        const from = checkDate(v.szpitalOd, 'szpitalOd', true, 'datę przyjęcia dziecka do szpitala')
        if (from && born && v.szpitalOd < born) issue('szpitalOd', 'Pobyt w szpitalu nie może zacząć się przed urodzeniem dziecka.')
        if (checkDate(v.szpitalDo, 'szpitalDo', true, 'datę wypisu (jeśli pobyt trwa — przewidywaną)') && from && v.szpitalDo < v.szpitalOd)
          issue('szpitalDo', 'Data wypisu nie może być wcześniejsza niż data przyjęcia.')
      }
      const week = v.tydzienCiazy.trim()
      if (week && (!/^\d+$/.test(week) || Number(week) < 20 || Number(week) > 45))
        issue('tydzienCiazy', 'Wpisz tydzień ciąży od 20 do 45 albo zostaw pole puste.')
      if (!motherSituationValues.includes(v.sytuacjaMatki)) issue('sytuacjaMatki', 'Wybierz jedną z odpowiedzi.')
      if (v.sytuacjaMatki !== 'brak' && checkDate(v.sytuacjaMatkiOd, 'sytuacjaMatkiOd', true, 'datę początku tej sytuacji')) {
        if (v.sytuacjaMatkiDo && checkDate(v.sytuacjaMatkiDo, 'sytuacjaMatkiDo', false, 'datę końca') && v.sytuacjaMatkiDo < v.sytuacjaMatkiOd)
          issue('sytuacjaMatkiDo', 'Data końca nie może być wcześniejsza niż data początku.')
      }
    }

    if (!roles.includes(v.rola)) issue('rola', 'Wybierz, czy jesteś matką, czy ojcem dziecka.')
    if (checkDate(v.dataUrodzenia, 'dataUrodzenia', true, 'swoją datę urodzenia')) {
      if (v.dataUrodzenia > addDaysIso(today, -365 * 13)) issue('dataUrodzenia', 'Sprawdź tę datę — wygląda na zbyt późną.')
      else if (v.dataUrodzenia < '1940-01-01') issue('dataUrodzenia', 'Sprawdź tę datę — wygląda na zbyt wczesną.')
    }

    checkWork(v.praca, 'praca', issue, today, { rodzaj: v.rodzaj, mother: v.rola === 'matka' })
    if (v.dlaKogo === bothParents) checkWork(v.drugi, 'drugi', issue, today, { rodzaj: v.rodzaj, mother: v.rola === 'ojciec' })
  })

export type FormValues = z.infer<typeof schema>
type StepId = 'rodzaj' | 'data' | 'okolicznosci' | 'dlaKogo' | 'praca' | 'drugi'

const workFields = (prefix: 'praca' | 'drugi') =>
  (Object.keys(workSchema.shape) as (keyof WorkAnswers)[]).map((field) => `${prefix}.${field}` as FieldPath<FormValues>)

const allQuestions: { id: StepId; title: string; fields: FieldPath<FormValues>[] }[] = [
  { id: 'rodzaj', title: 'Co się u Ciebie dzieje?', fields: ['rodzaj'] },
  { id: 'data', title: 'Podaj datę', fields: ['data', 'dataDziecka', 'liczbaDzieci'] },
  {
    id: 'okolicznosci',
    title: 'Szczególne okoliczności',
    fields: ['zaZyciem', 'stanDziecka', 'dataZgonu', 'liczbaZmarlych', 'szpitalDziecka', 'szpitalOd', 'szpitalDo', 'tydzienCiazy', 'sytuacjaMatki', 'sytuacjaMatkiOd', 'sytuacjaMatkiDo'],
  },
  { id: 'dlaKogo', title: 'Dla kogo sprawdzić świadczenia?', fields: ['dlaKogo'] },
  { id: 'praca', title: 'Praca i ubezpieczenie', fields: ['rola', ...workFields('praca'), 'dataUrodzenia'] },
  { id: 'drugi', title: 'Praca drugiego rodzica', fields: workFields('drugi') },
]

const questionsFor = (dlaKogo: string, rodzaj: string) =>
  allQuestions.filter((q) => (q.id !== 'drugi' || dlaKogo === bothParents) && (q.id !== 'okolicznosci' || hasCircumstances(rodzaj)))

const eventOptions: ChoiceOption[] = eventKinds.map((value) => ({ value, label: eventKindLabels[value] }))

const scopeOptions: ChoiceOption[] = [
  { value: 'ja', label: calculationScopeLabels.ja },
  { value: 'ja_i_drugi_rodzic', label: calculationScopeLabels.ja_i_drugi_rodzic },
]

const roleOptions: ChoiceOption[] = [
  { value: 'matka', label: 'Matką' },
  { value: 'ojciec', label: 'Ojcem' },
]

const workOptions: ChoiceOption[] = [
  { value: 'pracownik', label: 'Umowa o pracę' },
  { value: 'zleceniobiorca', label: 'Umowa zlecenie' },
  { value: 'dzialalnosc', label: 'Własna działalność gospodarcza' },
  { value: 'brak', label: 'Nie pracuję', description: 'Nie ma umowy ani działalności, z której płacone są składki.' },
]

const yesNoOptions: ChoiceOption[] = [
  { value: 'tak', label: 'Tak' },
  { value: 'nie', label: 'Nie' },
]

const childStateOptions: ChoiceOption[] = [
  { value: 'zyje', label: 'Nie, dziecko żyje' },
  { value: 'martwe', label: 'Dziecko urodziło się martwe' },
  { value: 'zmarlo', label: 'Dziecko zmarło po porodzie' },
]

const motherSituationOptions: { value: MotherSituationKind | 'brak'; label: string }[] = [
  { value: 'brak', label: 'Nie dotyczy' },
  { value: 'pobyt_w_szpitalu', label: 'Matka przebywa w szpitalu' },
  { value: 'niezdolnosc_do_samodzielnej_egzystencji', label: 'Matka ma orzeczoną niezdolność do samodzielnej egzystencji' },
  { value: 'zgon', label: 'Matka zmarła' },
  { value: 'porzucenie_dziecka', label: 'Matka porzuciła dziecko' },
]

const headcountOptions: ChoiceOption[] = [...yesNoOptions, { value: 'nie_wiem', label: 'Nie wiem', description: 'Zapytamy o to później.' }]

const dateCopy: Record<EventKind, { label: string; hint: string; children: string }> = {
  planuje: {
    label: 'Kiedy planujesz, że dziecko się urodzi? (jeśli wiesz)',
    hint: 'Możesz zostawić to pole puste. Z przybliżoną datą łatwiej policzyć okresy.',
    children: 'Zwykle 1. Więcej, jeśli spodziewasz się ciąży mnogiej.',
  },
  ciaza: {
    label: 'Kiedy masz termin porodu?',
    hint: 'Znajdziesz ją w karcie ciąży albo zapytaj lekarza prowadzącego. Możesz podać termin, który już minął (np. przy sprawie z poprzedniego roku) — wtedy w kroku o pracy dodaj zwolnienia lekarskie z ciąży.',
    children: 'Przy ciąży mnogiej wpisz liczbę dzieci.',
  },
  urodzenie: {
    label: 'Kiedy urodziło się dziecko?',
    hint: 'Jeśli to poród mnogi, podaj datę urodzenia pierwszego dziecka.',
    children: 'Przy porodzie mnogim wpisz liczbę dzieci.',
  },
  przyjecie: {
    label: 'Kiedy dziecko zostało przyjęte do Twojej rodziny?',
    hint: 'Datę znajdziesz w postanowieniu sądu albo w umowie z ośrodkiem.',
    children: 'Wpisz, ile dzieci przyjmujesz jednocześnie.',
  },
}

const AVERAGE_HINT =
  'Podstawa zasiłku to średnia z 12 miesięcy przed powstaniem prawa do świadczenia, pomniejszona o składki na ubezpieczenia społeczne (13,71%). Przyjmujemy tę samą kwotę w każdym miesiącu — to szacunek, który możesz później poprawić.'

function applyAnswers(facts: FactSet, v: FormValues): FactSet {
  let next = withFact(facts, 'sprawa', 'zdarzenie.rodzaj', userFactOf(v.rodzaj))
  next = withFact(next, 'sprawa', 'zdarzenie.data', v.data ? userFactOf(v.data) : undefined)
  next = withFact(next, 'sprawa', 'zdarzenie.dlaKogo', userFactOf(v.dlaKogo))
  next = withFact(next, 'sprawa', 'dziecko.liczba', userFactOf(Number(v.liczbaDzieci)))
  if (v.rodzaj === 'przyjecie') {
    next = withFact(next, 'sprawa', 'dziecko.dataUrodzenia', userFactOf(v.dataDziecka))
  } else if (getFact(next, 'sprawa', 'dziecko.dataUrodzenia')?.source === 'user') {
    next = withFact(next, 'sprawa', 'dziecko.dataUrodzenia', undefined)
  }
  next = applyCircumstances(next, v)
  return applyManualFacts(next, {
    today: todayIso(),
    eventDate: v.data || undefined,
    rola: v.rola as ParentRole,
    rodzaj: v.rodzaj,
    dataUrodzenia: v.dataUrodzenia,
    ja: workInputFrom(v.praca),
    drugi: v.dlaKogo === bothParents ? workInputFrom(v.drugi) : undefined,
  })
}

/** Zapisuje odpowiedzi z kroku „Szczególne okoliczności”; odpowiedź przecząca usuwa tylko fakty wpisane wcześniej przez użytkownika. */
function applyCircumstances(facts: FactSet, v: FormValues): FactSet {
  const set = (current: FactSet, key: string, value: unknown) =>
    value !== undefined
      ? withFact(current, 'sprawa', key, userFactOf(value))
      : getFact(current, 'sprawa', key)?.source === 'user'
        ? withFact(current, 'sprawa', key, undefined)
        : current
  const birth = v.rodzaj === 'urodzenie'
  const lost = birth && (v.stanDziecka === 'martwe' || v.stanDziecka === 'zmarlo')
  const children = Number(v.liczbaDzieci)

  let next = set(facts, 'dziecko.zaZyciem', hasCircumstances(v.rodzaj) ? v.zaZyciem === 'tak' : undefined)
  next = set(next, 'dziecko.martweUrodzenie', birth ? v.stanDziecka === 'martwe' : undefined)
  next = set(next, 'dziecko.dataZgonu', birth && v.stanDziecka === 'zmarlo' ? v.dataZgonu : undefined)
  next = set(next, 'dziecko.liczbaZmarlych', lost ? (children > 1 ? Number(v.liczbaZmarlych) : 1) : undefined)
  next = set(next, 'dziecko.pobytyWSzpitalu', birth && v.szpitalDziecka === 'tak' ? [{ od: v.szpitalOd, do: v.szpitalDo }] : undefined)
  next = set(next, 'dziecko.tydzienCiazyPrzyPorodzie', birth && v.tydzienCiazy.trim() ? Number(v.tydzienCiazy) : undefined)
  next = set(
    next,
    'matka.sytuacjaSzczegolna',
    birth && v.sytuacjaMatki !== 'brak' ? { rodzaj: v.sytuacjaMatki, od: v.sytuacjaMatkiOd, do: v.sytuacjaMatkiDo || null } : undefined,
  )
  return next
}

const errorAt = (errors: FieldErrors<FormValues>, field: string): unknown =>
  field.split('.').reduce<unknown>((node, key) => (node as Record<string, unknown> | undefined)?.[key], errors)

interface WorkFieldsProps {
  prefix: 'praca' | 'drugi'
  values: WorkAnswers
  errors: FieldErrors<WorkAnswers> | undefined
  register: UseFormRegister<FormValues>
  control: Control<FormValues>
  rodzaj: string
  rola: string
  bothParents: boolean
}

/** Pytania o pracę — pełna wersja dla wnioskodawcy, krótsza (bez wymiaru etatu) dla drugiego rodzica. */
function WorkFields({ prefix, values, errors, register, control, rodzaj, rola, bothParents: both }: WorkFieldsProps) {
  const self = prefix === 'praca'
  const name = (field: keyof WorkAnswers) => `${prefix}.${field}` as const
  const { forma } = values
  const contract = forma === 'pracownik' || forma === 'zleceniobiorca'
  const working = workFormValues.includes(forma) && forma !== 'brak'
  const radio = (field: Exclude<keyof WorkAnswers, 'extras'>) => ({ ...register(name(field)), value: values[field] })

  const amountLabel =
    forma === 'dzialalnosc'
      ? self ? 'Od jakiej kwoty płacisz składki miesięcznie?' : 'Od jakiej kwoty drugi rodzic płaci składki miesięcznie?'
      : forma === 'zleceniobiorca'
        ? 'Ile wynosi wynagrodzenie z umowy zlecenia (brutto, miesięcznie)?'
        : self ? 'Ile zarabiasz miesięcznie brutto?' : 'Ile drugi rodzic zarabia miesięcznie brutto?'

  return (
    <div className="space-y-5">
      <Field group label={self ? 'Jak pracujesz?' : 'Jak pracuje drugi rodzic?'} error={errors?.forma}>
        <RadioGroup {...radio('forma')} options={workOptions} appearance="rows" />
      </Field>

      {working ? (
        <>
          <Field
            label="Od kiedy trwa ta umowa lub działalność?"
            description="Data z umowy albo z wpisu do CEIDG."
            help="Od tej daty liczymy pełne miesiące ubezpieczenia. Z nich wychodzi podstawa zasiłku, a przy zasiłku chorobowym także okres wyczekiwania: 30 dni przy obowiązkowym ubezpieczeniu chorobowym i 90 dni przy dobrowolnym."
            error={errors?.od}
          >
            <DateInput {...register(name('od'))} className="sm:max-w-xs" />
          </Field>

          {forma !== 'pracownik' ? (
            <Field
              group
              label={self ? 'Czy płacisz dobrowolne ubezpieczenie chorobowe?' : 'Czy drugi rodzic płaci dobrowolne ubezpieczenie chorobowe?'}
              description="Sprawdzisz to w eZUS albo u zleceniodawcy."
              help="Przy umowie zlecenia i działalności ubezpieczenie chorobowe jest dobrowolne. Bez niego nie przysługuje ani zasiłek chorobowy, ani macierzyński."
              error={errors?.chorobowe}
            >
              <RadioGroup {...radio('chorobowe')} options={yesNoOptions} columns={2} />
            </Field>
          ) : null}

          {contract ? (
            <>
              <Field group label="Czy umowa jest na czas określony?" error={errors?.czasOkreslony}>
                <RadioGroup {...radio('czasOkreslony')} options={yesNoOptions} columns={2} />
              </Field>
              {values.czasOkreslony === 'tak' ? (
                <Field label="Do kiedy trwa umowa?" optional error={errors?.umowaDo}>
                  <DateInput {...register(name('umowaDo'))} className="sm:max-w-xs" />
                </Field>
              ) : null}
            </>
          ) : null}

          {forma === 'pracownik' ? (
            <Field label="Wymiar etatu" description="Wpisz liczbę albo ułamek, np. 1 (pełny etat), 0,63 albo 5/8." error={errors?.etat}>
              <TextInput {...register(name('etat'))} inputMode="decimal" autoComplete="off" className="sm:max-w-32" />
            </Field>
          ) : null}

          <Field
            label={amountLabel}
            description={forma === 'dzialalnosc' ? 'Zadeklarowana podstawa składek, a nie przychód.' : 'Kwota brutto, przed potrąceniami.'}
            help={AVERAGE_HINT}
            error={errors?.kwota}
          >
            <MoneyInput {...register(name('kwota'))} className="sm:max-w-xs" />
          </Field>

          {contract ? (
            <Field
              group
              label={forma === 'pracownik' ? 'Czy pracodawca zgłasza do ubezpieczenia więcej niż 20 osób?' : 'Czy zleceniodawca zgłasza do ubezpieczenia więcej niż 20 osób?'}
              help="Jeśli płatnik zgłasza do ubezpieczenia chorobowego więcej niż 20 osób, zasiłek wypłaca on sam. Przy 20 osobach lub mniej wypłaca ZUS, a wniosek składasz przez płatnika, który przekazuje go do ZUS."
              error={errors?.ponad20}
            >
              <RadioGroup {...radio('ponad20')} options={headcountOptions} appearance="rows" />
            </Field>
          ) : null}

          {forma === 'dzialalnosc' ? (
            <>
              <Field
                group
                label={self ? 'Czy masz zaległości w składkach ZUS?' : 'Czy drugi rodzic ma zaległości w składkach ZUS?'}
                help="Przy działalności ZUS sprawdza saldo składek na dzień powstania prawa do zasiłku. Jeśli zaległość przekracza 1% płacy minimalnej, zasiłek przysługuje dopiero po jej spłacie."
                error={errors?.zaleglosci}
              >
                <RadioGroup {...radio('zaleglosci')} options={yesNoOptions} columns={2} />
              </Field>
              {values.zaleglosci === 'tak' ? (
                <Field label="Ile wynosi zaległość?" error={errors?.zaleglosciKwota}>
                  <MoneyInput {...register(name('zaleglosciKwota'))} className="sm:max-w-xs" />
                </Field>
              ) : null}
            </>
          ) : null}

          <WorkExtras
            prefix={prefix}
            self={self}
            forma={forma}
            rodzaj={rodzaj}
            mother={self ? rola === 'matka' : rola === 'ojciec'}
            bothParents={both}
            values={values.extras}
            errors={errors?.extras}
            register={register}
            control={control}
          />
        </>
      ) : null}
    </div>
  )
}

export function IntakePage() {
  const { facts, setFacts } = useCaseState()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const pageRef = useFocusHeading<HTMLDivElement>()

  const stored = (key: string) => String(getFact(facts, 'sprawa', key)?.value ?? '')
  const storedPerson = (key: string) => String(getFact(facts, 'wnioskodawca', key)?.value ?? '')
  const [step, setStep] = useState(() =>
    Math.max(0, questionsFor(stored('zdarzenie.dlaKogo'), stored('zdarzenie.rodzaj')).findIndex((q) => q.id === params.get('krok'))),
  )
  const storedValue = <T,>(key: string) => getFact(facts, 'sprawa', key)?.value as T | undefined
  const storedStay = storedValue<{ od: string; do: string }[]>('dziecko.pobytyWSzpitalu')?.[0]
  const storedMother = storedValue<{ rodzaj: string; od: string; do?: string | null }>('matka.sytuacjaSzczegolna')
  const { register, handleSubmit, trigger, watch, formState, control } = useForm<FormValues>({
    resolver: zodResolver(schema),
    shouldFocusError: false,
    defaultValues: {
      rodzaj: stored('zdarzenie.rodzaj'),
      data: stored('zdarzenie.data'),
      dataDziecka: stored('dziecko.dataUrodzenia'),
      liczbaDzieci: stored('dziecko.liczba') || '1',
      dlaKogo: stored('zdarzenie.dlaKogo'),
      rola: storedPerson('osoba.rola'),
      dataUrodzenia: storedPerson('osoba.dataUrodzenia'),
      zaZyciem: storedValue<boolean>('dziecko.zaZyciem') ? 'tak' : 'nie',
      stanDziecka: storedValue<boolean>('dziecko.martweUrodzenie') ? 'martwe' : storedValue('dziecko.dataZgonu') ? 'zmarlo' : 'zyje',
      dataZgonu: stored('dziecko.dataZgonu'),
      liczbaZmarlych: stored('dziecko.liczbaZmarlych'),
      szpitalDziecka: storedStay ? 'tak' : 'nie',
      szpitalOd: storedStay?.od ?? '',
      szpitalDo: storedStay?.do ?? '',
      tydzienCiazy: stored('dziecko.tydzienCiazyPrzyPorodzie'),
      sytuacjaMatki: storedMother?.rodzaj ?? 'brak',
      sytuacjaMatkiOd: storedMother?.od ?? '',
      sytuacjaMatkiDo: storedMother?.do ?? '',
      praca: workAnswersFrom(facts, 'wnioskodawca'),
      drugi: workAnswersFrom(facts, 'drugiRodzic'),
    },
  })

  const rodzaj = watch('rodzaj') as EventKind | ''
  const dlaKogo = watch('dlaKogo')
  const rola = watch('rola')
  const praca = watch('praca')
  const drugi = watch('drugi')
  const zaZyciem = watch('zaZyciem')
  const stanDziecka = watch('stanDziecka')
  const szpitalDziecka = watch('szpitalDziecka')
  const sytuacjaMatki = watch('sytuacjaMatki')
  const liczbaDzieci = Number(watch('liczbaDzieci'))
  const questions = questionsFor(dlaKogo, rodzaj)
  const current = questions[Math.min(step, questions.length - 1)]
  const copy = rodzaj ? dateCopy[rodzaj] : undefined
  const { errors, isSubmitting, isValidating } = formState
  const rodzajField = register('rodzaj')
  const dlaKogoField = register('dlaKogo')
  const rolaField = register('rola')
  const lastStep = questions.length - 1

  useEffect(() => {
    pageRef.current?.querySelector('h1')?.focus()
  }, [pageRef, step])

  const focusInvalid = () => {
    requestAnimationFrame(() => pageRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus())
  }

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (step < lastStep) {
      if (await trigger(current.fields)) setStep(step + 1)
      else focusInvalid()
      return
    }

    await handleSubmit(
      (values) => {
        setFacts(applyAnswers(facts, values))
        navigate(paths.review)
      },
      (invalid) => {
        const firstInvalid = questions.findIndex(({ fields }) => fields.some((field) => errorAt(invalid, field)))
        if (firstInvalid >= 0) setStep(firstInvalid)
        focusInvalid()
      },
    )()
  }

  const description =
    current.id === 'rodzaj'
      ? 'Sprawdź, jakie świadczenia mogą Ci przysługiwać.'
      : current.id === 'okolicznosci'
        ? 'Te informacje zmieniają wymiar urlopów i zasiłków. Jeśli żadna z sytuacji Cię nie dotyczy, po prostu przejdź dalej.'
      : current.id === 'praca'
        ? 'Te dane zwykle pobieramy z ZUS. Wpisz je w przybliżeniu — poprawisz je później.'
        : current.id === 'drugi'
          ? `Kilka pytań o ${rola === 'ojciec' ? 'matkę' : rola === 'matka' ? 'ojca' : 'drugiego rodzica'} dziecka.`
          : undefined

  return (
    <div ref={pageRef} className="mx-auto max-w-xl space-y-4">
      <Card className="space-y-5">
        <div className="space-y-2">
          <p className="text-sm font-medium text-ink-soft">Pytanie {step + 1} z {questions.length}</p>
          <div className="flex gap-1.5" aria-hidden="true">
            {questions.map(({ id }, index) => (
              <span key={id} className={`h-1.5 flex-1 rounded-full ${index <= step ? 'bg-brand' : 'bg-line'}`} />
            ))}
          </div>
        </div>
        <PageHeader title={current.id === 'data' ? (copy?.label ?? current.title) : current.title} description={description} />

        <form onSubmit={(event) => void onSubmit(event)} noValidate className="space-y-5">
          {current.id === 'rodzaj' ? (
            <Field group label={<span className="sr-only">Twoja sytuacja</span>} error={errors.rodzaj}>
              <RadioGroup {...rodzajField} value={rodzaj} options={eventOptions} appearance="rows" />
            </Field>
          ) : null}

          {current.id === 'data' && copy ? (
            <div className="space-y-5">
              <Field label={<span className="sr-only">{copy.label}</span>} description={copy.hint} error={errors.data}>
                <DateInput {...register('data')} className="sm:max-w-xs" />
              </Field>
              {rodzaj === 'przyjecie' ? (
                <Field
                  label="Kiedy urodziło się przyjmowane dziecko?"
                  help="Urlop na warunkach urlopu macierzyńskiego trwa do 20 tygodni (przy jednym dziecku), ale nie dłużej niż do 7. urodzin dziecka w rodzinie zastępczej (10. przy odroczonym obowiązku szkolnym) albo do 14. urodzin przy przysposobieniu. Nie krócej jednak niż 9 tygodni."
                  error={errors.dataDziecka}
                >
                  <DateInput {...register('dataDziecka')} className="sm:max-w-xs" />
                </Field>
              ) : null}
              <Field label="Ile dzieci?" description={copy.children} error={errors.liczbaDzieci}>
                <NumberInput {...register('liczbaDzieci')} min={1} max={10} step={1} inputMode="numeric" className="sm:max-w-32" />
              </Field>
            </div>
          ) : null}

          {current.id === 'okolicznosci' ? (
            <div className="space-y-5">
              <Field
                group
                label="Czy dziecko ma zaświadczenie „Za życiem”?"
                help="Zaświadczenie lekarskie o ciężkim i nieodwracalnym upośledzeniu albo nieuleczalnej chorobie zagrażającej życiu (ustawa „Za życiem”). Wydłuża urlop rodzicielski do 65 tygodni (67 przy więcej niż jednym dziecku)."
                error={errors.zaZyciem}
              >
                <RadioGroup {...register('zaZyciem')} value={zaZyciem} options={yesNoOptions} columns={2} />
              </Field>

              {rodzaj === 'urodzenie' ? (
                <>
                  <Field group label="Czy dziecko urodziło się martwe albo zmarło?" error={errors.stanDziecka}>
                    <RadioGroup {...register('stanDziecka')} value={stanDziecka} options={childStateOptions} appearance="rows" />
                  </Field>
                  {stanDziecka === 'zmarlo' ? (
                    <Field label="Data zgonu dziecka" error={errors.dataZgonu}>
                      <DateInput {...register('dataZgonu')} className="sm:max-w-xs" />
                    </Field>
                  ) : null}
                  {liczbaDzieci > 1 && (stanDziecka === 'martwe' || stanDziecka === 'zmarlo') ? (
                    <Field label="Ile dzieci z tego porodu zmarło lub urodziło się martwych?" error={errors.liczbaZmarlych}>
                      <NumberInput {...register('liczbaZmarlych')} min={1} max={liczbaDzieci} step={1} inputMode="numeric" className="sm:max-w-32" />
                    </Field>
                  ) : null}

                  <Field
                    group
                    label="Czy dziecko po porodzie przebywało lub przebywa w szpitalu?"
                    help="Pobyt dziecka w szpitalu po porodzie (zwłaszcza wcześniaka) może dać prawo do uzupełniającego urlopu macierzyńskiego."
                    error={errors.szpitalDziecka}
                  >
                    <RadioGroup {...register('szpitalDziecka')} value={szpitalDziecka} options={yesNoOptions} columns={2} />
                  </Field>
                  {szpitalDziecka === 'tak' ? (
                    <>
                      <Field label="Od kiedy dziecko jest w szpitalu?" error={errors.szpitalOd}>
                        <DateInput {...register('szpitalOd')} className="sm:max-w-xs" />
                      </Field>
                      <Field label="Do kiedy?" description="Jeśli pobyt nadal trwa, wpisz przewidywaną datę wypisu." error={errors.szpitalDo}>
                        <DateInput {...register('szpitalDo')} className="sm:max-w-xs" />
                      </Field>
                      <Field label="W którym tygodniu ciąży urodziło się dziecko?" optional error={errors.tydzienCiazy}>
                        <NumberInput {...register('tydzienCiazy')} min={20} max={45} step={1} inputMode="numeric" className="sm:max-w-32" />
                      </Field>
                    </>
                  ) : null}

                  <Field
                    label="Czy dotyczy Cię któraś z sytuacji matki dziecka?"
                    help="Gdy matka jest w szpitalu, ma orzeczoną niezdolność do samodzielnej egzystencji, zmarła albo porzuciła dziecko, część urlopu macierzyńskiego może przejąć ojciec dziecka."
                    error={errors.sytuacjaMatki}
                  >
                    <Select {...register('sytuacjaMatki')} className="sm:max-w-md">
                      {motherSituationOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                    </Select>
                  </Field>
                  {sytuacjaMatki !== 'brak' ? (
                    <>
                      <Field label="Od kiedy?" error={errors.sytuacjaMatkiOd}>
                        <DateInput {...register('sytuacjaMatkiOd')} className="sm:max-w-xs" />
                      </Field>
                      {sytuacjaMatki === 'pobyt_w_szpitalu' || sytuacjaMatki === 'niezdolnosc_do_samodzielnej_egzystencji' ? (
                        <Field label="Do kiedy?" optional error={errors.sytuacjaMatkiDo}>
                          <DateInput {...register('sytuacjaMatkiDo')} className="sm:max-w-xs" />
                        </Field>
                      ) : null}
                    </>
                  ) : null}
                </>
              ) : null}
            </div>
          ) : null}

          {current.id === 'dlaKogo' ? (
            <Field group label={<span className="sr-only">Dla kogo sprawdzić świadczenia</span>} error={errors.dlaKogo}>
              <RadioGroup {...dlaKogoField} value={dlaKogo} options={scopeOptions} appearance="rows" />
            </Field>
          ) : null}

          {current.id === 'praca' ? (
            <div className="space-y-5">
              <Field group label="Kim jesteś dla dziecka?" error={errors.rola}>
                <RadioGroup {...rolaField} value={rola} options={roleOptions} columns={2} />
              </Field>
              <WorkFields prefix="praca" values={praca} errors={errors.praca} register={register} control={control} rodzaj={rodzaj} rola={rola} bothParents={dlaKogo === bothParents} />
              <Field
                label="Twoja data urodzenia"
                help="Przy zwolnieniu lekarskim w ciąży na umowie o pracę pracodawca najpierw płaci wynagrodzenie chorobowe: przez 33 dni w roku, a od 50. roku życia przez 14 dni. Dopiero potem jest zasiłek chorobowy."
                error={errors.dataUrodzenia}
              >
                <DateInput {...register('dataUrodzenia')} className="sm:max-w-xs" />
              </Field>
            </div>
          ) : null}

          {current.id === 'drugi' ? (
            <WorkFields prefix="drugi" values={drugi} errors={errors.drugi} register={register} control={control} rodzaj={rodzaj} rola={rola} bothParents />
          ) : null}

          <div className="grid gap-1">
            <Button type="submit" fullWidth disabled={isSubmitting || isValidating}>
              {step === lastStep ? 'Przejdź do uzupełniania danych' : 'Dalej'}
            </Button>
            {step > 0 ? (
              <Button variant="link" className="justify-self-center" onClick={() => setStep(step - 1)} disabled={isSubmitting || isValidating}>Wróć</Button>
            ) : (
              <Button asChild variant="link" className="justify-self-center"><Link to={paths.start}>Zmień sposób rozpoczęcia</Link></Button>
            )}
          </div>
        </form>
      </Card>
      <p className="text-sm text-ink-soft">Twoje odpowiedzi nie są wysyłane do urzędu.</p>
    </div>
  )
}
