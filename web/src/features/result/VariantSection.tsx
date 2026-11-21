import { Link } from 'react-router'
import type { PaymentVariant } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Button, Field, RadioGroup, Section, type ChoiceOption } from '@/ui'

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
}

export function VariantSection({ variant, onChange, disabled }: VariantSectionProps) {
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
