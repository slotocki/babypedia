import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState, type FormEvent } from 'react'
import { useForm, type FieldPath } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { z } from 'zod'
import type { EventKind, FactSet } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { QuickStart } from '@/features/cases/QuickStart'
import { addDaysIso, isValidIsoDate, todayIso } from '@/features/review/dates'
import { calculationScopeLabels, eventKindLabels } from '@/features/review/fact-labels'
import { getFact, userFactOf, withFact } from '@/features/review/facts'
import { Button, Card, DateInput, Field, PageHeader, RadioGroup, type ChoiceOption } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

const eventKinds = Object.keys(eventKindLabels) as EventKind[]
const scopes = ['ja', 'ja_i_drugi_rodzic']

const schema = z
  .object({ rodzaj: z.string(), data: z.string(), dataDziecka: z.string(), dlaKogo: z.string() })
  .superRefine((v, ctx) => {
    const issue = (path: string, message: string) => ctx.addIssue({ code: z.ZodIssueCode.custom, path: [path], message })
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
  })

type FormValues = z.infer<typeof schema>

const questions = [
  { title: 'Co się u Ciebie dzieje?', fields: ['rodzaj'] },
  { title: 'Podaj datę', fields: ['data', 'dataDziecka'] },
  { title: 'Dla kogo sprawdzić świadczenia?', fields: ['dlaKogo'] },
] satisfies { title: string; fields: FieldPath<FormValues>[] }[]

const eventOptions: ChoiceOption[] = eventKinds.map((value) => ({ value, label: eventKindLabels[value] }))

const scopeOptions: ChoiceOption[] = [
  { value: 'ja', label: calculationScopeLabels.ja },
  { value: 'ja_i_drugi_rodzic', label: calculationScopeLabels.ja_i_drugi_rodzic },
]

const dateCopy: Record<EventKind, { label: string; hint: string }> = {
  planuje: {
    label: 'Kiedy planujesz, że dziecko się urodzi? (jeśli wiesz)',
    hint: 'Możesz zostawić to pole puste. Z przybliżoną datą łatwiej policzyć okresy.',
  },
  ciaza: {
    label: 'Kiedy masz termin porodu?',
    hint: 'Znajdziesz ją w karcie ciąży albo zapytaj lekarza prowadzącego.',
  },
  urodzenie: { label: 'Kiedy urodziło się dziecko?', hint: 'Jeśli to poród mnogi, podaj datę urodzenia pierwszego dziecka.' },
  przyjecie: { label: 'Kiedy dziecko zostało przyjęte do Twojej rodziny?', hint: 'Datę znajdziesz w postanowieniu sądu albo w umowie z ośrodkiem.' },
}

function applyAnswers(facts: FactSet, v: FormValues): FactSet {
  let next = withFact(facts, 'sprawa', 'zdarzenie.rodzaj', userFactOf(v.rodzaj))
  next = withFact(next, 'sprawa', 'zdarzenie.data', v.data ? userFactOf(v.data) : undefined)
  next = withFact(next, 'sprawa', 'zdarzenie.dlaKogo', userFactOf(v.dlaKogo))
  if (v.rodzaj === 'przyjecie') {
    next = withFact(next, 'sprawa', 'dziecko.dataUrodzenia', userFactOf(v.dataDziecka))
  } else if (getFact(next, 'sprawa', 'dziecko.dataUrodzenia')?.source === 'user') {
    next = withFact(next, 'sprawa', 'dziecko.dataUrodzenia', undefined)
  }
  return next
}

export function IntakePage() {
  const { facts, setFacts } = useCaseState()
  const navigate = useNavigate()
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [step, setStep] = useState(0)

  const stored = (key: string) => String(getFact(facts, 'sprawa', key)?.value ?? '')
  const { register, handleSubmit, trigger, watch, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    shouldFocusError: false,
    defaultValues: {
      rodzaj: stored('zdarzenie.rodzaj'),
      data: stored('zdarzenie.data'),
      dataDziecka: stored('dziecko.dataUrodzenia'),
      dlaKogo: stored('zdarzenie.dlaKogo'),
    },
  })

  const rodzaj = watch('rodzaj') as EventKind | ''
  const dlaKogo = watch('dlaKogo')
  const copy = rodzaj ? dateCopy[rodzaj] : undefined
  const { errors, isSubmitting, isValidating } = formState
  const rodzajField = register('rodzaj')
  const dlaKogoField = register('dlaKogo')
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
      if (await trigger(questions[step].fields)) setStep(step + 1)
      else focusInvalid()
      return
    }

    await handleSubmit(
      (values) => {
        setFacts(applyAnswers(facts, values))
        navigate(paths.login)
      },
      (invalid) => {
        const firstInvalid = questions.findIndex(({ fields }) => fields.some((field) => invalid[field]))
        if (firstInvalid >= 0) setStep(firstInvalid)
        focusInvalid()
      },
    )()
  }

  return (
    <div ref={pageRef} className="mx-auto max-w-xl space-y-4">
      <Card className="space-y-5">
        <div className="space-y-2">
          <p className="text-sm font-medium text-ink-soft">Pytanie {step + 1} z {questions.length}</p>
          <div className="flex gap-1.5" aria-hidden="true">
            {questions.map(({ title }, index) => (
              <span key={title} className={`h-1.5 flex-1 rounded-full ${index <= step ? 'bg-brand' : 'bg-line'}`} />
            ))}
          </div>
        </div>
        <PageHeader
          title={step === 1 ? (copy?.label ?? questions[step].title) : questions[step].title}
          description={step === 0 ? 'Sprawdź, jakie świadczenia mogą Ci przysługiwać.' : undefined}
        />

        <form onSubmit={(event) => void onSubmit(event)} noValidate className="space-y-5">
          {step === 0 ? (
            <Field group label={<span className="sr-only">Twoja sytuacja</span>} error={errors.rodzaj}>
              <RadioGroup {...rodzajField} value={rodzaj} options={eventOptions} appearance="rows" />
            </Field>
          ) : null}

          {step === 1 && copy ? (
            <div className="space-y-5">
              <Field label={<span className="sr-only">{copy.label}</span>} description={copy.hint} error={errors.data}>
                <DateInput {...register('data')} className="sm:max-w-xs" />
              </Field>
              {rodzaj === 'przyjecie' ? (
                <Field
                  label="Kiedy urodziło się przyjmowane dziecko?"
                  description="Wiek dziecka wpływa na zasady."
                  error={errors.dataDziecka}
                >
                  <DateInput {...register('dataDziecka')} className="sm:max-w-xs" />
                </Field>
              ) : null}
            </div>
          ) : null}

          {step === lastStep ? (
            <Field group label={<span className="sr-only">Dla kogo sprawdzić świadczenia</span>} error={errors.dlaKogo}>
              <RadioGroup {...dlaKogoField} value={dlaKogo} options={scopeOptions} appearance="rows" />
            </Field>
          ) : null}

          <div className="grid gap-1">
            <Button type="submit" fullWidth disabled={isSubmitting || isValidating}>
              {step === lastStep ? 'Przejdź do uzupełniania danych' : 'Dalej'}
            </Button>
            {step > 0 ? (
              <Button variant="link" className="justify-self-center" onClick={() => setStep(step - 1)} disabled={isSubmitting || isValidating}>Wróć</Button>
            ) : null}
          </div>
        </form>
      </Card>
      <p className="text-sm text-ink-soft">Twoje odpowiedzi nie są wysyłane do urzędu.</p>
      <QuickStart />
    </div>
  )
}
