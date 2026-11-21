import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import type { IsoDate, MissingFact, PaymentVariant } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Alert, Button, DateInput, Field, RadioGroup, Section, type ChoiceOption } from '@/ui'

const options: ChoiceOption[] = [
  {
    value: 'standardowy',
    label: 'Standardowy',
    description: 'Zasiłek macierzyński 100% podstawy, za urlop rodzicielski 70%.',
  },
  {
    value: 'usredniony',
    label: 'Uśredniony — 81,5%',
    description: 'Jedna stawka 81,5% za cały okres. Wniosek trzeba złożyć do 21 dni po porodzie lub przyjęciu dziecka.',
  },
]

export interface VariantSectionProps {
  variant: PaymentVariant
  onChange: (variant: PaymentVariant) => void
  disabled?: boolean
  /** Pytanie silnika o datę wniosku o 81,5% — gdy termin 21 dni minął, a daty brak (wtedy liczone są stawki standardowe). */
  applicationDateQuestion?: MissingFact
  onApplicationDate?: (date: IsoDate) => void
}

function ApplicationDateForm({ question, onSave, busy }: { question: MissingFact; onSave: (date: IsoDate) => void; busy?: boolean }) {
  const [value, setValue] = useState('')
  const [error, setError] = useState<string>()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      setError('Podaj datę złożenia wniosku.')
      return
    }
    setError(undefined)
    onSave(value)
  }

  return (
    <Alert variant="warning" title="Termin na wniosek o 81,5% już minął">
      <p>
        Wybrany jest wariant uśredniony, ale termin 21 dni od porodu lub przyjęcia dziecka już minął. Bez daty wniosku złożonego w terminie liczymy
        stawki standardowe: 100% za urlop macierzyński i 70% za rodzicielski.
      </p>
      <form onSubmit={submit} noValidate className="mt-3 space-y-3">
        <Field label={question.question} description="Jeśli wniosek złożono w terminie, podaj jego datę — przeliczymy wynik." error={error} fix="wybierz dzień z kalendarza">
          <DateInput value={value} onChange={(e) => setValue(e.target.value)} />
        </Field>
        <Button type="submit" variant="secondary" loading={busy} loadingLabel="Przeliczam…">
          Zapisz datę wniosku
        </Button>
      </form>
    </Alert>
  )
}

export function VariantSection({ variant, onChange, disabled, applicationDateQuestion, onApplicationDate }: VariantSectionProps) {
  return (
    <Section title="Sprawdź inny wariant" description="Zobacz, jak zmieni się wynik, gdy zmienisz sposób wypłaty albo plan.">
      <div className="space-y-4">
        <Field label="Wariant wypłaty zasiłku" group>
          <RadioGroup
            name="wariant-wyplaty"
            options={options}
            value={variant}
            disabled={disabled}
            onChange={(event) => onChange(event.target.value as PaymentVariant)}
          />
        </Field>
        {variant === 'usredniony' && applicationDateQuestion && onApplicationDate ? (
          <ApplicationDateForm question={applicationDateQuestion} onSave={onApplicationDate} busy={disabled} />
        ) : null}
        <p role="status" className="text-sm text-ink-soft">
          Po zmianie wyniki powyżej przeliczą się od razu.
        </p>
        <Button asChild className="w-full sm:w-auto">
          <Link to={paths.scenarios}>Porównaj warianty</Link>
        </Button>
      </div>
    </Section>
  )
}
