import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState, type FormEvent } from 'react'
import { useForm, type FieldErrors, type FieldPath, type UseFormRegister } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { z } from 'zod'
import type { EventKind, FactSet, ParentRole } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { addDaysIso, isValidIsoDate, todayIso } from '@/features/review/dates'
import { calculationScopeLabels, eventKindLabels } from '@/features/review/fact-labels'
import { getFact, userFactOf, withFact } from '@/features/review/facts'
import { Button, Card, DateInput, Field, MoneyInput, NumberInput, PageHeader, RadioGroup, Select, type ChoiceOption } from '@/ui'
import { applyManualFacts, parseAmount, workAnswersFrom, workInputFrom, type WorkAnswers } from './manualFacts'
import { useFocusHeading } from './useFocusHeading'

const eventKinds = Object.keys(eventKindLabels) as EventKind[]
const scopes = ['ja', 'ja_i_drugi_rodzic']
const bothParents = 'ja_i_drugi_rodzic'
const roles = ['matka', 'ojciec']
const workFormValues = ['pracownik', 'zleceniobiorca', 'dzialalnosc', 'brak']
const etatValues = ['1', '0.75', '0.5', '0.25']

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
}) satisfies z.ZodType<WorkAnswers>

type Issue = (path: string, message: string) => void

function checkWork(w: WorkAnswers, prefix: 'praca' | 'drugi', issue: Issue, today: string) {
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
  if (w.forma === 'pracownik' && !etatValues.includes(w.etat)) at('etat', 'Wybierz wymiar etatu.')

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
          if (v.data < addDaysIso(today, -30))
            issue('data', 'Ta data już minęła. Jeśli dziecko się urodziło, wybierz „Wróć”, a potem „Dziecko się urodziło”.')
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

    if (!roles.includes(v.rola)) issue('rola', 'Wybierz, czy jesteś matką, czy ojcem dziecka.')
    if (checkDate(v.dataUrodzenia, 'dataUrodzenia', true, 'swoją datę urodzenia')) {
      if (v.dataUrodzenia > addDaysIso(today, -365 * 13)) issue('dataUrodzenia', 'Sprawdź tę datę — wygląda na zbyt późną.')
      else if (v.dataUrodzenia < '1940-01-01') issue('dataUrodzenia', 'Sprawdź tę datę — wygląda na zbyt wczesną.')
    }

    checkWork(v.praca, 'praca', issue, today)
    if (v.dlaKogo === bothParents) checkWork(v.drugi, 'drugi', issue, today)
  })

type FormValues = z.infer<typeof schema>
type StepId = 'rodzaj' | 'data' | 'dlaKogo' | 'praca' | 'drugi'

const workFields = (prefix: 'praca' | 'drugi') =>
  (Object.keys(workSchema.shape) as (keyof WorkAnswers)[]).map((field) => `${prefix}.${field}` as FieldPath<FormValues>)

const allQuestions: { id: StepId; title: string; fields: FieldPath<FormValues>[] }[] = [
  { id: 'rodzaj', title: 'Co się u Ciebie dzieje?', fields: ['rodzaj'] },
  { id: 'data', title: 'Podaj datę', fields: ['data', 'dataDziecka', 'liczbaDzieci'] },
  { id: 'dlaKogo', title: 'Dla kogo sprawdzić świadczenia?', fields: ['dlaKogo'] },
  { id: 'praca', title: 'Praca i ubezpieczenie', fields: ['rola', ...workFields('praca'), 'dataUrodzenia'] },
  { id: 'drugi', title: 'Praca drugiego rodzica', fields: workFields('drugi') },
]

const questionsFor = (dlaKogo: string) => allQuestions.filter((q) => q.id !== 'drugi' || dlaKogo === bothParents)

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

const headcountOptions: ChoiceOption[] = [...yesNoOptions, { value: 'nie_wiem', label: 'Nie wiem', description: 'Zapytamy o to później.' }]

const etatOptions: { value: string; label: string }[] = [
  { value: '1', label: 'Pełny etat' },
  { value: '0.75', label: '3/4 etatu' },
  { value: '0.5', label: '1/2 etatu' },
  { value: '0.25', label: '1/4 etatu' },
]

const dateCopy: Record<EventKind, { label: string; hint: string; children: string }> = {
  planuje: {
    label: 'Kiedy planujesz, że dziecko się urodzi? (jeśli wiesz)',
    hint: 'Możesz zostawić to pole puste. Z przybliżoną datą łatwiej policzyć okresy.',
    children: 'Zwykle 1. Więcej, jeśli spodziewasz się ciąży mnogiej.',
  },
  ciaza: {
    label: 'Kiedy masz termin porodu?',
    hint: 'Znajdziesz ją w karcie ciąży albo zapytaj lekarza prowadzącego.',
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
  return applyManualFacts(next, {
    today: todayIso(),
    eventDate: v.data || undefined,
    rola: v.rola as ParentRole,
    dataUrodzenia: v.dataUrodzenia,
    ja: workInputFrom(v.praca),
    drugi: v.dlaKogo === bothParents ? workInputFrom(v.drugi) : undefined,
  })
}

const errorAt = (errors: FieldErrors<FormValues>, field: string): unknown =>
  field.split('.').reduce<unknown>((node, key) => (node as Record<string, unknown> | undefined)?.[key], errors)

interface WorkFieldsProps {
  prefix: 'praca' | 'drugi'
  values: WorkAnswers
  errors: FieldErrors<WorkAnswers> | undefined
  register: UseFormRegister<FormValues>
}

/** Pytania o pracę — pełna wersja dla wnioskodawcy, krótsza (bez wymiaru etatu) dla drugiego rodzica. */
function WorkFields({ prefix, values, errors, register }: WorkFieldsProps) {
  const self = prefix === 'praca'
  const name = (field: keyof WorkAnswers) => `${prefix}.${field}` as const
  const { forma } = values
  const contract = forma === 'pracownik' || forma === 'zleceniobiorca'
  const working = workFormValues.includes(forma) && forma !== 'brak'
  const radio = (field: keyof WorkAnswers) => ({ ...register(name(field)), value: values[field] })

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

          {self && forma === 'pracownik' ? (
            <Field label="Wymiar etatu" optional error={errors?.etat}>
              <Select {...register(name('etat'))} className="sm:max-w-xs">
                {etatOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </Select>
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
  const [step, setStep] = useState(() => Math.max(0, questionsFor(stored('zdarzenie.dlaKogo')).findIndex((q) => q.id === params.get('krok'))))
  const { register, handleSubmit, trigger, watch, formState } = useForm<FormValues>({
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
      praca: workAnswersFrom(facts, 'wnioskodawca'),
      drugi: workAnswersFrom(facts, 'drugiRodzic'),
    },
  })

  const rodzaj = watch('rodzaj') as EventKind | ''
  const dlaKogo = watch('dlaKogo')
  const rola = watch('rola')
  const praca = watch('praca')
  const drugi = watch('drugi')
  const questions = questionsFor(dlaKogo)
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
              <WorkFields prefix="praca" values={praca} errors={errors.praca} register={register} />
              <Field
                label="Twoja data urodzenia"
                help="Przy zwolnieniu lekarskim w ciąży na umowie o pracę pracodawca najpierw płaci wynagrodzenie chorobowe: przez 33 dni w roku, a od 50. roku życia przez 14 dni. Dopiero potem jest zasiłek chorobowy."
                error={errors.dataUrodzenia}
              >
                <DateInput {...register('dataUrodzenia')} className="sm:max-w-xs" />
              </Field>
            </div>
          ) : null}

          {current.id === 'drugi' ? <WorkFields prefix="drugi" values={drugi} errors={errors.drugi} register={register} /> : null}

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
