import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import type { BenefitStatus, FactSource } from '@/app/api/types'
import {
  Accordion,
  Alert,
  Amount,
  Button,
  Card,
  Checkbox,
  DateInput,
  DeadlineItem,
  Dialog,
  Disclosure,
  EmptyState,
  ErrorState,
  Field,
  MoneyInput,
  NumberInput,
  PageHeader,
  Period,
  RadioGroup,
  Section,
  Select,
  SimulationBadge,
  SimulationBanner,
  Skeleton,
  SkeletonText,
  SourceBadge,
  Spinner,
  Stack,
  StatusBadge,
  Stepper,
  Switch,
  Tabs,
  TextInput,
  ToneScope,
  Tooltip,
} from '..'
import { parseMoney } from '../format'

const statuses: BenefitStatus[] = ['przysługuje', 'nie_przysługuje', 'brakuje_danych', 'nie_dotyczy', 'nie_rozstrzygam']
const sources: FactSource[] = ['mobywatel', 'zus', 'ezla', 'usc', 'user', 'assumption']

const schema = z.object({
  event: z.string({ required_error: 'Wybierz jedną odpowiedź.' }).min(1, 'Wybierz jedną odpowiedź.'),
  date: z.string().min(1, 'Podaj datę.'),
  salary: z.string().refine((v) => parseMoney(v) !== null, 'Wpisz kwotę w złotych, na przykład 6 500,50.'),
  children: z.coerce.number({ invalid_type_error: 'Wpisz liczbę.' }).int('Wpisz liczbę całkowitą.').min(1, 'Wpisz co najmniej 1.'),
  employer: z.string().min(1, 'Wybierz rodzaj umowy.'),
  consent: z.literal(true, { errorMap: () => ({ message: 'Zaznacz, aby przejść dalej.' }) }),
  payer: z.string().min(2, 'Wpisz nazwę płatnika składek.'),
})
type DemoValues = z.input<typeof schema>

const eventOptions = [
  { value: 'ciaza', label: 'Jestem w ciąży', description: 'Policzymy zasiłki przed porodem i po nim.' },
  { value: 'urodzenie', label: 'Dziecko się urodziło', description: 'Sprawdzimy urlopy i zasiłki.' },
  { value: 'przyjecie', label: 'Przyjmuję dziecko na wychowanie' },
]

function DemoForm() {
  const [sent, setSent] = useState(false)
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<DemoValues, unknown, z.output<typeof schema>>({ resolver: zodResolver(schema) })
  const radio = register('event')
  const event = watch('event')

  return (
    <form
      noValidate
      className="space-y-5"
      onSubmit={handleSubmit(async () => {
        await new Promise((resolve) => setTimeout(resolve, 600))
        setSent(true)
      })}
    >
      <Field group label="Co się dzieje?" error={errors.event} fix="Wybierz jedną z trzech kart.">
        <RadioGroup
          name={radio.name}
          ref={radio.ref}
          onChange={radio.onChange}
          onBlur={radio.onBlur}
          value={event ?? null}
          options={eventOptions}
        />
      </Field>
      <Field label="Przewidywana data porodu" description="Dzień, który podał lekarz." error={errors.date} fix="Wybierz datę z kalendarza.">
        <DateInput {...register('date')} />
      </Field>
      <Field label="Przeciętne wynagrodzenie brutto" error={errors.salary} fix="Użyj cyfr i przecinka.">
        <MoneyInput {...register('salary')} />
      </Field>
      <Field label="Liczba dzieci" optional error={errors.children}>
        <NumberInput suffix="szt." min={1} {...register('children')} />
      </Field>
      <Field label="Nazwa płatnika składek" description="Zwykle nazwa Twojego pracodawcy." error={errors.payer} fix="Wpisz co najmniej dwa znaki.">
        <TextInput autoComplete="organization" {...register('payer')} />
      </Field>
      <Field label="Rodzaj umowy" error={errors.employer} fix="Wskaż umowę, na podstawie której pracujesz.">
        <Select {...register('employer')} defaultValue="">
          <option value="" disabled>
            Wybierz…
          </option>
          <option value="pracownik">Umowa o pracę</option>
          <option value="zlecenie">Umowa zlecenie</option>
          <option value="dzialalnosc">Własna działalność</option>
        </Select>
      </Field>
      <Field label="Zgoda" error={errors.consent} fix="Zaznacz pole poniżej.">
        <Checkbox label="Rozumiem, że to symulacja na fikcyjnych danych" {...register('consent')} />
      </Field>
      <Switch label="Pokaż ścieżkę decyzji" description="Wyjaśnienie krok po kroku." />
      <Button type="submit" loading={isSubmitting}>
        Dalej
      </Button>
      {sent ? <Alert variant="info" title="Formularz poprawny" /> : null}
    </form>
  )
}

export function UiGallery() {
  const [difficult, setDifficult] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [retrying, setRetrying] = useState(false)

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 space-y-10 px-4 py-6">
      <PageHeader
        eyebrow="Galeria komponentów"
        title="Komponenty Kompasu Rodzica"
        description="Wspólne elementy interfejsu. Przełącznik poniżej pokazuje stonowany wariant na trudną sytuację."
        action={<Switch label="Ton: trudna sytuacja" checked={difficult} onChange={(e) => setDifficult(e.target.checked)} />}
      />

      <Section title="Postęp">
        <Stepper steps={['Pytania', 'Logowanie', 'Dane', 'Wynik']} current={2} />
      </Section>

      <Section title="Przyciski">
        <Stack direction="row" gap="sm">
          <Button>Główna akcja</Button>
          <Button variant="secondary">Drugorzędna</Button>
          <Button variant="link">Link</Button>
          <Button disabled>Wyłączony</Button>
          <Button loading>Zapisuję</Button>
        </Stack>
      </Section>

      <Section title="Statusy świadczeń i źródła faktów">
        <Stack direction="row" gap="sm">
          {statuses.map((s) => (
            <StatusBadge key={s} status={s} />
          ))}
        </Stack>
        <Stack direction="row" gap="md">
          {sources.map((s) => (
            <SourceBadge key={s} source={s} />
          ))}
        </Stack>
        <SimulationBadge />
        <SimulationBanner system="Logowanie przez mObywatel" />
      </Section>

      <Section title="Komunikaty">
        <Stack>
          <Alert variant="info" title="Informacja">Aplikacja informuje, nie orzeka.</Alert>
          <Alert variant="warning" title="Uwaga">Brakuje daty końca umowy.</Alert>
          <Alert variant="risk" title="Zakończenie umowy przed porodem">
            Możesz utracić prawo do zasiłku macierzyńskiego. Sprawdź wariant „co jeśli”.
          </Alert>
          <Alert variant="error" title="Błąd">Nie udało się zapisać.</Alert>
        </Stack>
      </Section>

      <Section title="Formularz (React Hook Form + Zod)" description="Kliknij „Dalej” bez wypełniania, aby zobaczyć błędy przy polach.">
        <Card>
          <DemoForm />
        </Card>
      </Section>

      <Section title="Wynik">
        <Card className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h3 className="text-lg font-semibold">Zasiłek macierzyński</h3>
            <StatusBadge status="przysługuje" />
          </div>
          <Period period={{ from: '2027-03-15', to: '2027-08-01', days: 140, weeks: 20 }} />
          <Amount value={26255.6} size="lg" />
          <Disclosure title="Dlaczego tak? Ścieżka decyzji" summary="3 kroki">
            <ol className="list-decimal space-y-2 pl-5">
              <li>W dniu porodu podlegasz ubezpieczeniu chorobowemu.</li>
              <li>Masz co najmniej 30 dni ubezpieczenia.</li>
            </ol>
          </Disclosure>
        </Card>
        <DeadlineItem
          emphasized
          deadline={{
            type: 'najpóźniej_do',
            date: '2027-04-05',
            text: 'Złóż wniosek o 81,5% do 5 kwietnia 2027',
            consequence: '70% za urlop rodzicielski',
          }}
        />
        <DeadlineItem deadline={{ type: 'przedawnienie', date: '2032-08-01', text: 'Roszczenie o wypłatę zasiłku.' }} />
      </Section>

      <Section title="Panele">
        <Tabs
          label="Przykładowe zakładki"
          items={[
            { value: 'a', label: 'Ja', content: <p>Wynik dla wnioskodawcy.</p> },
            { value: 'b', label: 'Drugi rodzic', content: <p>Wynik dla drugiego rodzica.</p> },
          ]}
        />
        <Accordion
          items={[
            { value: '1', title: 'Jakie dokumenty przygotować?', content: 'Wniosek o urlop i odpis aktu urodzenia.' },
            { value: '2', title: 'Gdzie złożyć wniosek?', content: 'U pracodawcy.' },
          ]}
        />
        <Stack direction="row" gap="sm">
          <Dialog
            open={dialogOpen}
            onOpenChange={setDialogOpen}
            trigger={<Button variant="secondary">Otwórz okno</Button>}
            title="Wysłać wniosek?"
            description="To symulacja. Nic nie zostanie wysłane do ZUS."
            footer={
              <>
                <Button variant="secondary" onClick={() => setDialogOpen(false)}>Anuluj</Button>
                <Button onClick={() => setDialogOpen(false)}>Wyślij (symulacja)</Button>
              </>
            }
          />
          <Tooltip content="Podstawa to średnia z 12 miesięcy.">
            <Button variant="link">Co to jest podstawa?</Button>
          </Tooltip>
        </Stack>
      </Section>

      <Section title="Stany ładowania i błędów">
        <Stack>
          <Spinner />
          <SkeletonText />
          <Skeleton className="h-24" />
          <EmptyState title="Brak zgłoszeń" description="Gdy wyślesz pierwszy wniosek, pojawi się tutaj." action={<Button variant="secondary">Wróć do wyniku</Button>} />
          <ErrorState
            error={{ title: 'Usługa chwilowo niedostępna', detail: 'Serwer nie odpowiada.', status: 503 }}
            retrying={retrying}
            onRetry={() => {
              setRetrying(true)
              setTimeout(() => setRetrying(false), 1200)
            }}
          />
        </Stack>
      </Section>

    </ToneScope>
  )
}
