import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { z } from 'zod'
import type { EventKind, FactSet } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { addDaysIso, isValidIsoDate, todayIso } from '@/features/review/dates'
import { calculationScopeLabels, eventKindLabels } from '@/features/review/fact-labels'
import { getFact, userFactOf, withFact } from '@/features/review/facts'
import { Button, DateInput, Field, PageHeader, RadioGroup, Stepper, type ChoiceOption } from '@/ui'
import { flowSteps } from './steps'
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
            issue('data', 'Ta data już minęła. Jeśli dziecko się urodziło, wróć wyżej i wybierz „Dziecko się urodziło”.')
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

const eventOptions: ChoiceOption[] = eventKinds.map((value) => ({ value, label: eventKindLabels[value] }))

const scopeOptions: ChoiceOption[] = [
  { value: 'ja', label: calculationScopeLabels.ja },
  { value: 'ja_i_drugi_rodzic', label: calculationScopeLabels.ja_i_drugi_rodzic, description: 'Pokażę też, co przysługuje drugiemu rodzicowi.' },
]

const dateCopy: Record<EventKind, { label: string; hint: string }> = {
  planuje: {
    label: 'Kiedy planujesz, że dziecko się urodzi? (jeśli wiesz)',
    hint: 'Możesz zostawić to pole puste. Z przybliżoną datą łatwiej policzyć okresy.',
  },
  ciaza: {
    label: 'Jaka jest przewidywana data porodu?',
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

  const stored = (key: string) => String(getFact(facts, 'sprawa', key)?.value ?? '')
  const { register, handleSubmit, watch, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      rodzaj: stored('zdarzenie.rodzaj'),
      data: stored('zdarzenie.data'),
      dataDziecka: stored('dziecko.dataUrodzenia'),
      dlaKogo: stored('zdarzenie.dlaKogo'),
    },
  })

  const rodzaj = watch('rodzaj') as EventKind | ''
  const dlaKogo = watch('dlaKogo')
  const copy = rodzaj ? dateCopy[rodzaj] : null
  const { errors } = formState
  const rodzajField = register('rodzaj')
  const dlaKogoField = register('dlaKogo')

  const onSubmit = handleSubmit((values) => {
    setFacts(applyAnswers(facts, values))
    navigate(paths.login)
  })

  return (
    <div ref={pageRef} className="mx-auto max-w-2xl space-y-6">
      <Stepper steps={flowSteps} current={0} />
      <PageHeader
        title="Powiedz nam, co się dzieje"
        description="Trzy krótkie pytania. Na ich podstawie sprawdzimy, jakie świadczenia mogą Ci przysługiwać. Nic nie jest wysyłane do żadnego urzędu."
      />

      <form onSubmit={onSubmit} noValidate className="space-y-8">
        <Field group label="1. Co się dzieje?" error={errors.rodzaj}>
          <RadioGroup {...rodzajField} value={rodzaj} options={eventOptions} />
        </Field>

        {copy && rodzaj ? (
          <div className="space-y-5">
            <Field label={`2. ${copy.label}`} description={copy.hint} error={errors.data}>
              <DateInput {...register('data')} className="sm:max-w-xs" />
            </Field>
            {rodzaj === 'przyjecie' ? (
              <Field
                label="Kiedy urodziło się przyjmowane dziecko?"
                description="Wiek dziecka ma wpływ na zasady, dlatego o niego pytamy."
                error={errors.dataDziecka}
              >
                <DateInput {...register('dataDziecka')} className="sm:max-w-xs" />
              </Field>
            ) : null}
          </div>
        ) : (
          <p className="text-ink-soft">2. Gdy wybierzesz odpowiedź na pierwsze pytanie, zapytamy o właściwą datę.</p>
        )}

        <Field group label="3. Dla kogo liczymy?" error={errors.dlaKogo}>
          <RadioGroup {...dlaKogoField} value={dlaKogo} options={scopeOptions} />
        </Field>

        <Button type="submit" fullWidth className="sm:w-auto">
          Dalej
        </Button>
      </form>
    </div>
  )
}
