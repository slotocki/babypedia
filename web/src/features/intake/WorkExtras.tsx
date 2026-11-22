import type { ReactNode } from 'react'
import { useFieldArray, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form'
import { Button, Checkbox, DateInput, Field, NumberInput, RadioGroup, TextInput, type ChoiceOption } from '@/ui'
import type { FormValues } from './IntakePage'
import {
  emptyExtraTitle,
  emptyLeavePart,
  emptyPartTime,
  emptySickLeave,
  MAX_EXTRA_TITLES,
  MAX_LEAVE_PARTS,
  MAX_PART_TIME,
  MAX_SICK_LEAVES,
  showsLeavePlan,
  showsResignation,
  showsSickLeaves,
  type ExtrasAnswers,
} from './extrasFacts'

const titleOptions: ChoiceOption[] = [
  { value: 'pracownik', label: 'Umowa o pracę' },
  { value: 'zleceniobiorca', label: 'Umowa zlecenie' },
  { value: 'dzialalnosc', label: 'Własna działalność gospodarcza' },
  { value: 'inny', label: 'Inny tytuł', description: 'Np. umowa agencyjna albo praca nakładcza.' },
]

const sicknessOptions: ChoiceOption[] = [
  { value: 'obowiazkowe', label: 'Tak, obowiązkowe' },
  { value: 'dobrowolne', label: 'Tak, dobrowolne' },
  { value: 'brak', label: 'Nie', description: 'Np. działalność opłacana tylko ze składką zdrowotną obok etatu.' },
]

const payerOptions: ChoiceOption[] = [
  { value: 'ten_sam', label: 'U tego samego pracodawcy', description: 'Np. kolejna umowa po poprzedniej albo zmiana etatu.' },
  { value: 'inny', label: 'U innego pracodawcy' },
]

const ETAT_HINT = 'Wpisz liczbę albo ułamek, np. 1, 0,63 albo 5/8.'

type Prefix = 'praca' | 'drugi'

export interface WorkExtrasProps {
  prefix: Prefix
  self: boolean
  forma: string
  rodzaj: string
  mother: boolean
  /** Czy liczymy też dla drugiego rodzica (wtedy ojciec może przejąć resztę urlopu macierzyńskiego). */
  bothParents: boolean
  values: ExtrasAnswers
  errors: FieldErrors<ExtrasAnswers> | undefined
  register: UseFormRegister<FormValues>
  control: Control<FormValues>
}

function Group({ title, help, children }: { title: string; help?: ReactNode; children: ReactNode }) {
  return (
    <section className="min-w-0 space-y-3 border-t border-line pt-4">
      <h2 className="text-base font-semibold text-ink">{title}</h2>
      {help ? <p className="text-sm text-ink-soft">{help}</p> : null}
      {children}
    </section>
  )
}

function Item({ title, onRemove, children }: { title: string; onRemove: () => void; children: ReactNode }) {
  return (
    <div className="min-w-0 space-y-4 rounded-card border border-line p-3 sm:p-4">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-base font-semibold text-ink">{title}</h3>
        <Button variant="link" onClick={onRemove}>Usuń</Button>
      </div>
      {children}
    </div>
  )
}

const twoColumns = 'grid min-w-0 gap-4 sm:grid-cols-2'

/** Dodatkowe, domyślnie zwinięte sekcje kroku „Praca i ubezpieczenie”: kolejne tytuły, e-ZLA, plan urlopów. */
export function WorkExtras({ prefix, self, forma, rodzaj, mother, bothParents, values, errors, register, control }: WorkExtrasProps) {
  const base = `${prefix}.extras` as const
  const titles = useFieldArray({ control, name: `${base}.tytuly` })
  const leaves = useFieldArray({ control, name: `${base}.zwolnienia` })
  const parts = useFieldArray({ control, name: `${base}.czesci` })
  const partTime = useFieldArray({ control, name: `${base}.pracaCzesc` })
  const scope = { rodzaj, mother }
  const radio = (name: Parameters<UseFormRegister<FormValues>>[0], value: string) => ({ ...register(name), value })
  const who = (mine: string, theirs: string) => (self ? mine : theirs)

  return (
    <div className="space-y-5">
      <Field
        label="Do kiedy trwał ten tytuł ubezpieczenia?"
        optional
        description="Wypełnij tylko, jeśli umowa lub działalność już się skończyła."
        help="Data końca tytułu ubezpieczenia (rozwiązanie umowy, zamknięcie lub zawieszenie działalności). Od niej zależy, czy w dniu porodu było ubezpieczenie chorobowe."
        error={errors?.tytulDo}
      >
        <DateInput {...register(`${base}.tytulDo`)} className="sm:max-w-xs" />
      </Field>

      <Group
        title={who('Inne tytuły ubezpieczenia', 'Inne tytuły ubezpieczenia drugiego rodzica')}
        help="Np. druga umowa, kolejna umowa po poprzedniej albo działalność obok etatu."
      >
        {titles.fields.map((item, index) => {
          const t = values.tytuly[index]
          const err = errors?.tytuly?.[index]
          const at = <K extends keyof ExtrasAnswers['tytuly'][number]>(key: K) => `${base}.tytuly.${index}.${key}` as const
          return (
            <Item key={item.id} title={`Tytuł ubezpieczenia ${index + 2}`} onRemove={() => titles.remove(index)}>
              <Field group label="Rodzaj" error={err?.forma}>
                <RadioGroup {...radio(at('forma'), t?.forma ?? '')} options={titleOptions} appearance="rows" />
              </Field>
              <div className={twoColumns}>
                <Field label="Od kiedy?" error={err?.od}>
                  <DateInput {...register(at('od'))} />
                </Field>
                <Field label="Do kiedy?" optional error={err?.do}>
                  <DateInput {...register(at('do'))} />
                </Field>
              </div>
              {t?.forma === 'pracownik' ? (
                <>
                  <Field label="Wymiar etatu" description={ETAT_HINT} error={err?.etat}>
                    <TextInput {...register(at('etat'))} inputMode="decimal" autoComplete="off" className="sm:max-w-32" />
                  </Field>
                  {forma === 'pracownik' ? (
                    <Field group label="Gdzie jest ta umowa o pracę?" error={err?.platnik}>
                      <RadioGroup {...radio(at('platnik'), t.platnik)} options={payerOptions} appearance="rows" />
                    </Field>
                  ) : null}
                </>
              ) : t?.forma ? (
                <Field
                  group
                  label="Czy ten tytuł obejmuje ubezpieczenie chorobowe?"
                  help="Umowa o pracę zawsze daje obowiązkowe ubezpieczenie chorobowe. Przy zleceniu i działalności jest ono dobrowolne, a gdy masz też etat, działalność bywa opłacana bez składki chorobowej."
                  error={err?.chorobowe}
                >
                  <RadioGroup {...radio(at('chorobowe'), t.chorobowe)} options={sicknessOptions} appearance="rows" />
                </Field>
              ) : null}
            </Item>
          )
        })}
        {titles.fields.length < MAX_EXTRA_TITLES ? (
          <Button variant="secondary" onClick={() => titles.append(emptyExtraTitle())}>
            {titles.fields.length === 0 ? 'Dodaj drugi tytuł ubezpieczenia' : 'Dodaj kolejny tytuł ubezpieczenia'}
          </Button>
        ) : null}
      </Group>

      {showsSickLeaves(scope) ? (
        <Group
          title={who('Zwolnienia lekarskie w ciąży', 'Zwolnienia lekarskie matki w ciąży')}
          help="Dane z e-ZLA (PUE/eZUS). Zwolnienie w ciąży ma kod literowy B."
        >
          {leaves.fields.map((item, index) => {
            const err = errors?.zwolnienia?.[index]
            const at = <K extends keyof ExtrasAnswers['zwolnienia'][number]>(key: K) => `${base}.zwolnienia.${index}.${key}` as const
            return (
              <Item key={item.id} title={`Zwolnienie ${index + 1}`} onRemove={() => leaves.remove(index)}>
                <div className={twoColumns}>
                  <Field label="Od" error={err?.od}>
                    <DateInput {...register(at('od'))} />
                  </Field>
                  <Field label="Do" error={err?.do}>
                    <DateInput {...register(at('do'))} />
                  </Field>
                </div>
                <div>
                  <Checkbox {...register(at('kodB'))} label="Kod B (ciąża)" />
                  <Checkbox {...register(at('szpital'))} label="Pobyt w szpitalu" />
                </div>
              </Item>
            )
          })}
          {leaves.fields.length < MAX_SICK_LEAVES ? (
            <Button variant="secondary" onClick={() => leaves.append(emptySickLeave())}>Dodaj zwolnienie lekarskie</Button>
          ) : null}
        </Group>
      ) : null}

      {showsResignation(scope) ? (
        <Group title={who('Rezygnacja z części urlopu macierzyńskiego', 'Rezygnacja matki z części urlopu macierzyńskiego')}>
          <Checkbox
            {...register(`${base}.rezygnacja`)}
            label={who('Rezygnuję z reszty urlopu macierzyńskiego, a resztę przejmie ojciec dziecka', 'Matka rezygnuje z reszty urlopu macierzyńskiego, a resztę przejmuje ojciec')}
            description="Najwcześniej po 14 tygodniach od porodu."
          />
          {values.rezygnacja ? (
            <Field
              label="Po ilu tygodniach od porodu?"
              help="Matka może zrezygnować z pozostałej części urlopu macierzyńskiego po wykorzystaniu co najmniej 14 tygodni po porodzie. Resztę może wtedy wykorzystać ubezpieczony ojciec dziecka."
              error={errors?.rezygnacjaPoTyg}
            >
              <NumberInput {...register(`${base}.rezygnacjaPoTyg`)} min={1} max={52} step={1} inputMode="numeric" className="sm:max-w-32" />
            </Field>
          ) : null}
          {values.rezygnacja && !bothParents ? (
            <p className="text-sm text-ink-soft">Żeby sprawdzić zasiłek ojca za przejętą część urlopu, wybierz na początku liczenie dla Ciebie i drugiego rodzica.</p>
          ) : null}
        </Group>
      ) : null}

      {showsLeavePlan(scope) ? (
        <Group
          title={who('Podział urlopu rodzicielskiego', 'Podział urlopu rodzicielskiego drugiego rodzica')}
          help="Urlop rodzicielski można wykorzystać naraz albo w najwyżej 5 częściach. Jeśli nie planujesz podziału, pomiń tę sekcję."
        >
          {parts.fields.map((item, index) => {
            const err = errors?.czesci?.[index]
            return (
              <Item key={item.id} title={`Część ${index + 1}`} onRemove={() => parts.remove(index)}>
                <div className={twoColumns}>
                  <Field label="Liczba tygodni" error={err?.tygodnie}>
                    <NumberInput {...register(`${base}.czesci.${index}.tygodnie`)} min={1} max={52} step={1} inputMode="numeric" />
                  </Field>
                  <Field label="Od kiedy?" optional error={err?.od}>
                    <DateInput {...register(`${base}.czesci.${index}.od`)} />
                  </Field>
                </div>
              </Item>
            )
          })}
          {parts.fields.length < MAX_LEAVE_PARTS ? (
            <Button variant="secondary" onClick={() => parts.append(emptyLeavePart())}>
              {parts.fields.length === 0 ? 'Podziel urlop na części' : 'Dodaj kolejną część'}
            </Button>
          ) : null}

          {partTime.fields.map((item, index) => {
            const err = errors?.pracaCzesc?.[index]
            return (
              <Item key={item.id} title={`Praca w czasie urlopu ${index + 1}`} onRemove={() => partTime.remove(index)}>
                <div className={twoColumns}>
                  <Field label="Wymiar etatu" description="Najwyżej 1/2 etatu, np. 0,5." error={err?.wymiar}>
                    <TextInput {...register(`${base}.pracaCzesc.${index}.wymiar`)} inputMode="decimal" autoComplete="off" />
                  </Field>
                  <Field label="Liczba tygodni" error={err?.tygodnie}>
                    <NumberInput {...register(`${base}.pracaCzesc.${index}.tygodnie`)} min={1} max={52} step={1} inputMode="numeric" />
                  </Field>
                  <Field label="Od kiedy?" optional error={err?.od}>
                    <DateInput {...register(`${base}.pracaCzesc.${index}.od`)} />
                  </Field>
                </div>
              </Item>
            )
          })}
          {partTime.fields.length < MAX_PART_TIME ? (
            <Button variant="secondary" onClick={() => partTime.append(emptyPartTime())}>
              Dodaj pracę na część etatu w czasie urlopu
            </Button>
          ) : null}
        </Group>
      ) : null}
    </div>
  )
}
