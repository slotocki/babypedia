import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import type { PersonRef, ScenarioChange } from '@/app/api/types'
import { Button, Card, DateInput, Field, NumberInput, RadioGroup, Switch, TextInput } from '@/ui'
import {
  buildChange,
  buildSchema,
  EXCLUSIVE_WEEKS,
  type FormValues,
  type ScenarioInfo,
} from './scenarios'

export type ServerErrors = Partial<Record<keyof FormValues, string>>

interface ScenarioFormProps {
  scenario: ScenarioInfo
  persons: PersonRef[]
  totalWeeks: number
  initial: FormValues
  serverErrors: ServerErrors
  pending: boolean
  onChangeValues: () => void
  onSubmit: (change: ScenarioChange) => void
}

const personOptions = [
  { value: 'wnioskodawca', label: 'Ja' },
  { value: 'drugiRodzic', label: 'Drugi rodzic' },
]

export function ScenarioForm({ scenario, persons, totalWeeks, initial, serverErrors, pending, onChangeValues, onSubmit }: ScenarioFormProps) {
  const id = scenario.id
  const schema = useMemo(() => buildSchema(id, totalWeeks), [id, totalWeeks])
  const choosePerson = persons.length > 1
  const defaultPerson = initial.person || (id === 'drugi_rodzic_bierze' ? 'drugiRodzic' : (persons[0] ?? 'wnioskodawca'))

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { ...initial, person: defaultPerson },
    shouldFocusError: true,
  })

  const message = (name: keyof FormValues) => errors[name]?.message ?? serverErrors[name]
  const notify = { onChange: onChangeValues }

  const submit = handleSubmit((values) => {
    const person: PersonRef | undefined = choosePerson ? (values.person as PersonRef) : id === 'drugi_rodzic_bierze' ? 'drugiRodzic' : undefined
    onSubmit(buildChange(id, values, person))
  })

  const maxForOne = totalWeeks - EXCLUSIVE_WEEKS

  return (
    <form onSubmit={submit} noValidate>
    <Card className="space-y-6">
      <div className="space-y-1">
        <h3 className="text-lg font-semibold text-ink">{scenario.title}</h3>
        <p className="text-ink-soft">{scenario.description}</p>
      </div>

      {choosePerson ? (
        <Field group label="Kogo dotyczy ten wariant?">
          <Controller
            control={control}
            name="person"
            render={({ field }) => (
              <RadioGroup
                name={field.name}
                value={field.value}
                onChange={(e) => {
                  field.onChange(e.target.value)
                  onChangeValues()
                }}
                options={personOptions}
                columns={2}
              />
            )}
          />
        </Field>
      ) : id === 'drugi_rodzic_bierze' ? (
        <p className="text-ink-soft">Ten wariant dotyczy drugiego rodzica.</p>
      ) : null}

      {id === 'odchodze_z_pracy' || id === 'przechodze_na_dzialalnosc' || id === 'splacam_zadluzenie' ? (
        <Field
          label={
            id === 'odchodze_z_pracy'
              ? 'Od kiedy nie będziesz już ubezpieczona/y z tej umowy?'
              : id === 'przechodze_na_dzialalnosc'
                ? 'Od kiedy zaczynasz działalność?'
                : 'W którym dniu spłacisz zaległe składki?'
          }
          description="Wybierz datę z kalendarza albo wpisz ją w formacie rrrr-mm-dd."
          error={message('data')}
          fix="wpisz poprawną datę z lat 2000–2100."
        >
          <DateInput {...register('data', notify)} className="sm:max-w-xs" />
        </Field>
      ) : null}

      {id === 'przechodze_na_dzialalnosc' ? (
        <Controller
          control={control}
          name="dobrowolne"
          render={({ field }) => (
            <Switch
              label="Opłacam dobrowolne ubezpieczenie chorobowe"
              description="Bez niego z działalności nie przysługuje zasiłek macierzyński."
              checked={field.value}
              name={field.name}
              onChange={(e) => {
                field.onChange(e.target.checked)
                onChangeValues()
              }}
            />
          )}
        />
      ) : null}

      {id === 'umowa_nie_zostanie_przedluzona' ? (
        <p className="text-ink-soft">Ten wariant nie wymaga żadnych dodatkowych danych. Użyjemy daty końca umowy, którą już znamy.</p>
      ) : null}

      {id === 'drugi_rodzic_bierze' || id === 'praca_na_czesc_etatu' ? (
        <Field
          label={id === 'drugi_rodzic_bierze' ? 'Ile tygodni urlopu rodzicielskiego bierze wskazana osoba?' : 'Przez ile tygodni łączysz urlop rodzicielski z pracą?'}
          description={
            id === 'drugi_rodzic_bierze'
              ? `Każdy rodzic ma ${EXCLUSIVE_WEEKS} tygodni urlopu rodzicielskiego tylko dla siebie, więc jedna osoba może wziąć najwyżej ${maxForOne} z ${totalWeeks} tygodni. Urlop można podzielić najwyżej na 5 części.`
              : `Urlop rodzicielski trwa łącznie ${totalWeeks} tygodni. Podaj, przez ile z nich pracujesz.`
          }
          error={message('tygodnie')}
          fix={id === 'drugi_rodzic_bierze' ? `podaj liczbę tygodni tak, aby każdemu rodzicowi zostało co najmniej ${EXCLUSIVE_WEEKS}.` : 'podaj całkowitą liczbę tygodni.'}
        >
          <NumberInput {...register('tygodnie', notify)} inputMode="numeric" suffix="tyg." className="sm:max-w-xs" />
        </Field>
      ) : null}

      {id === 'praca_na_czesc_etatu' ? (
        <Field
          label="Wymiar etatu w czasie łączenia"
          description="Podaj ułamek etatu, najwyżej 0,5 (pół etatu). Na przykład 0,25 to jedna czwarta etatu."
          error={message('wymiar')}
          fix="wpisz liczbę większą od 0 i najwyżej 0,5."
        >
          <TextInput {...register('wymiar', notify)} inputMode="decimal" autoComplete="off" className="sm:max-w-xs" />
        </Field>
      ) : null}

      {id === 'wariant_wyplaty' ? (
        <Field group label="Który wariant wypłaty sprawdzić?" error={message('wariant')} fix="wybierz jeden z dwóch wariantów.">
          <Controller
            control={control}
            name="wariant"
            render={({ field }) => (
              <RadioGroup
                name={field.name}
                value={field.value}
                onChange={(e) => {
                  field.onChange(e.target.value)
                  onChangeValues()
                }}
                options={[
                  { value: 'standardowy', label: 'Standardowy', description: '100% za zasiłek macierzyński, potem 70% za urlop rodzicielski.' },
                  { value: 'usredniony', label: 'Uśredniony', description: '81,5% przez cały okres. Wymaga wniosku do 21 dni po porodzie lub przyjęciu dziecka.' },
                ]}
              />
            )}
          />
          <p className="text-sm text-ink-soft">Dziewięć tygodni drugiego rodzica jest zawsze płatne w wysokości 70%.</p>
        </Field>
      ) : null}

      <Button type="submit" loading={pending} loadingLabel="Porównuję…">
        Porównaj z planem bazowym
      </Button>
    </Card>
    </form>
  )
}
