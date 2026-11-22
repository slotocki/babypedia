import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { Parameter, ParameterStatus } from '@/app/api/types'
import { Alert, Button, Card, Checkbox, DateInput, EmptyState, Field, RadioGroup, Section, Stack, TextInput, formatDate } from '@/ui'
import { QueryView } from './QueryView'
import { fieldErrors, generalMessage } from './problem'
import { useAdminSession } from './session'
import { groupOrder, otherGroup, parameterName } from './parameter-names'
import { allowsEmptyValue, describeUnit, formatParameterValue, inputSuffix, isTextUnit, parseInput } from './values'

interface KeyGroup {
  key: string
  rows: Parameter[]
}

interface RowGroup {
  wiersz: string
  keys: KeyGroup[]
}

function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function currentRow(rows: Parameter[], today: string): Parameter | undefined {
  let best: Parameter | undefined
  for (const row of rows) {
    if (row.validFrom.slice(0, 10) <= today && (!best || row.validFrom >= best.validFrom)) best = row
  }
  return best
}

function groupParameters(parameters: Parameter[], query: string): RowGroup[] {
  const needle = query.trim().toLocaleLowerCase('pl')
  const byKey = new Map<string, Parameter[]>()
  for (const parameter of parameters) {
    const list = byKey.get(parameter.key) ?? []
    list.push(parameter)
    byKey.set(parameter.key, list)
  }

  const groups = new Map<string, KeyGroup[]>()
  for (const [key, rows] of byKey) {
    const sorted = [...rows].sort((a, b) => a.validFrom.localeCompare(b.validFrom))
    const name = parameterName(key, sorted[0].wiersz)
    const haystack = [key, name?.title ?? '', ...sorted.flatMap((r) => [r.wiersz ?? '', r.legalBasis, r.note ?? ''])].join(' ').toLocaleLowerCase('pl')
    if (needle && !haystack.includes(needle)) continue
    const wiersz = name?.group ?? otherGroup
    const list = groups.get(wiersz) ?? []
    list.push({ key, rows: sorted })
    groups.set(wiersz, list)
  }
  const rank = (group: string) => {
    const index = groupOrder.indexOf(group)
    return index === -1 ? groupOrder.length : index
  }
  return [...groups].map(([wiersz, keys]) => ({ wiersz, keys })).sort((a, b) => rank(a.wiersz) - rank(b.wiersz))
}

export function ParametersTab() {
  const { password } = useAdminSession()
  const parameters = useQuery({
    queryKey: ['admin', 'parameters'],
    queryFn: () => api.admin.parameters(password),
  })
  const [search, setSearch] = useState('')
  const [saved, setSaved] = useState<string | null>(null)

  return (
    <Section
      title="Parametry prawne"
      description="Każda wartość ma datę „obowiązuje od”. Nowa wartość dotyczy okresów od wskazanego dnia i nie zmienia wcześniejszych."
    >
      <Alert variant="info" title="Co tu zmienić i gdzie zobaczyć efekt">
        <ol className="list-decimal space-y-1 pl-5">
          <li>Znajdź parametr (grupy poniżej albo wyszukiwarka) i wybierz „Dodaj wartość od dnia…”.</li>
          <li>Wpisz nową wartość i datę, od której obowiązuje (np. nowa stawka od 1 stycznia). Wcześniejsze okresy zostają bez zmian.</li>
          <li>Zobacz efekt: otwórz zakładkę „Historia zmian”, a potem w aplikacji przelicz wynik (strona „Twój plan”). Kwoty i terminy liczą się już z nowej wartości.</li>
        </ol>
      </Alert>
      <QueryView query={parameters} loadingLabel="Ładowanie parametrów…">
        {(data) => <ParameterList parameters={data} search={search} onSearch={setSearch} saved={saved} onSaved={setSaved} />}
      </QueryView>
    </Section>
  )
}

interface ParameterListProps {
  parameters: Parameter[]
  search: string
  onSearch: (value: string) => void
  saved: string | null
  onSaved: (message: string | null) => void
}

function ParameterList({ parameters, search, onSearch, saved, onSaved }: ParameterListProps) {
  const groups = useMemo(() => groupParameters(parameters, search), [parameters, search])
  const today = todayIso()
  const [editing, setEditing] = useState<string | null>(null)

  return (
    <Stack gap="lg">
      <Field label="Szukaj parametru" description="Wpisz fragment klucza, wiersza tabeli lub podstawy prawnej.">
        <TextInput type="search" value={search} onChange={(e) => onSearch(e.target.value)} autoComplete="off" />
      </Field>

      <div role="status" className="empty:hidden">
        {saved ? <Alert variant="info" title={saved} /> : null}
      </div>

      {parameters.length === 0 ? (
        <EmptyState title="Brak parametrów" description="Baza parametrów jest pusta." />
      ) : groups.length === 0 ? (
        <EmptyState title="Nic nie znaleziono" description="Zmień wyszukiwaną frazę." />
      ) : (
        groups.map((group) => (
          <Section key={group.wiersz} level={3} title={group.wiersz}>
            <Stack gap="md">
              {group.keys.map((item) => (
                <ParameterCard
                  key={item.key}
                  item={item}
                  today={today}
                  editing={editing === item.key}
                  onToggle={() => {
                    onSaved(null)
                    setEditing(editing === item.key ? null : item.key)
                  }}
                  onSaved={(message) => {
                    setEditing(null)
                    onSaved(message)
                  }}
                />
              ))}
            </Stack>
          </Section>
        ))
      )}
    </Stack>
  )
}

interface ParameterCardProps {
  item: KeyGroup
  today: string
  editing: boolean
  onToggle: () => void
  onSaved: (message: string) => void
}

function ParameterCard({ item, today, editing, onToggle, onSaved }: ParameterCardProps) {
  const current = currentRow(item.rows, today)
  const latest = item.rows[item.rows.length - 1]
  const reference = current ?? latest
  const unit = reference.unit
  const pending = reference.status === 'do_weryfikacji'
  const name = parameterName(item.key, reference.wiersz)

  return (
    <Card as="article" aria-label={`Parametr ${item.key}`}>
      <Stack gap="md">
        <div className="space-y-1">
          <h4 className="break-words text-base font-semibold text-ink">
            {name ? `${name.title}${name.variant ? ` — ${name.variant}` : ''}` : item.key}
          </h4>
          {name ? <p className="break-words font-mono text-xs text-ink-soft">{item.key}</p> : null}
          <p className="text-ink">
            <span className="text-ink-soft">Aktualna wartość: </span>
            <strong>{current ? formatParameterValue(current.value, unit) : 'brak obowiązującej wartości'}</strong>
          </p>
          <p className="text-sm text-ink-soft">
            Jednostka: {describeUnit(unit)} · Podstawa prawna: {reference.legalBasis}
          </p>
          <p className="text-sm font-semibold text-ink">
            Status: {pending ? 'Do weryfikacji — wartość wymaga potwierdzenia' : 'Zweryfikowany'}
          </p>
          {reference.note ? <p className="text-sm text-ink-soft">Uwaga: {reference.note}</p> : null}
        </div>

        <div>
          <h5 className="mb-2 text-sm font-semibold text-ink">Wartości w czasie</h5>
          <ul className="divide-y divide-line rounded-control border border-line">
            {item.rows.map((row, index) => {
              const isCurrent = row === current
              const future = row.validFrom.slice(0, 10) > today
              return (
                <li key={`${row.validFrom}-${index}`} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 px-3 py-2">
                  <span className="min-w-0">
                    <span className="text-ink-soft">Obowiązuje od </span>
                    <span className="font-medium text-ink">{formatDate(row.validFrom)}</span>
                  </span>
                  <span className="font-semibold text-ink">{formatParameterValue(row.value, unit)}</span>
                  <span className="basis-full text-sm text-ink-soft">
                    {isCurrent ? 'Obowiązuje obecnie' : future ? 'Okres przyszły' : 'Okres wcześniejszy'}
                    {row.status === 'do_weryfikacji' ? ' · Do weryfikacji' : ''}
                  </span>
                </li>
              )
            })}
          </ul>
        </div>

        {editing ? (
          <ChangeForm item={item} unit={unit} currentValue={reference.value} previousStatus={latest.status} onCancel={onToggle} onSaved={onSaved} />
        ) : (
          <div>
            <Button variant="secondary" onClick={onToggle} aria-label={`Dodaj wartość od dnia… dla ${item.key}`}>
              Dodaj wartość od dnia…
            </Button>
          </div>
        )}
      </Stack>
    </Card>
  )
}

interface ChangeFormProps {
  item: KeyGroup
  unit: string | undefined
  currentValue: Parameter['value']
  previousStatus: ParameterStatus
  onCancel: () => void
  onSaved: (message: string) => void
}

function ChangeForm({ item, unit, currentValue, previousStatus, onCancel, onSaved }: ChangeFormProps) {
  const { password } = useAdminSession()
  const queryClient = useQueryClient()
  const [value, setValue] = useState('')
  const [empty, setEmpty] = useState(false)
  const [validFrom, setValidFrom] = useState('')
  const [legalBasis, setLegalBasis] = useState('')
  const [reason, setReason] = useState('')
  const [status, setStatus] = useState<ParameterStatus>(previousStatus)
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const save = useMutation({
    mutationFn: (change: Parameters<typeof api.admin.updateParameter>[1]) => api.admin.updateParameter(password, change),
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: ['admin'] })
      onSaved(`Zapisano ${item.key}: ${formatParameterValue(saved.value, saved.unit)} od ${formatDate(saved.validFrom)}.`)
    },
  })

  const serverErrors = fieldErrors(save.error)
  const errors = { ...serverErrors, ...clientErrors }
  const general = save.isError && Object.keys(serverErrors).length === 0 ? generalMessage(save.error) : null
  const suffix = inputSuffix(unit)
  const canBeEmpty = allowsEmptyValue(unit, currentValue)

  function submit(event: FormEvent) {
    event.preventDefault()
    const found: Record<string, string> = {}
    const parsed = parseInput(value, unit, empty)
    if (!parsed.ok) found.value = parsed.message
    if (!validFrom) found.validFrom = 'Podaj datę, od której wartość obowiązuje.'
    setClientErrors(found)
    if (!parsed.ok || !validFrom) return
    save.reset()
    save.mutate({
      key: item.key,
      value: parsed.value,
      validFrom,
      legalBasis: legalBasis.trim() || undefined,
      reason: reason.trim() || undefined,
      status,
    })
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-4 rounded-control border border-line-strong bg-neutral-soft p-3 sm:p-4">
      <h5 className="text-base font-semibold text-ink">Nowa wartość dla {item.key}</h5>
      <p className="text-sm text-ink-soft">Zmiana obowiązuje od wskazanej daty i nie zmienia wcześniejszych okresów.</p>

      {empty ? null : (
        <Field
          label="Nowa wartość"
          error={errors.value}
          description={
            unit === 'ulamek'
              ? 'Podaj procent, np. 81,5 oznacza 81,5%.'
              : unit === 'zl'
                ? 'Podaj kwotę w złotych, np. 4806,50.'
                : isTextUnit(unit)
                  ? 'Podaj tekst ustawienia.'
                  : `Jednostka: ${describeUnit(unit)}. Podaj liczbę całkowitą.`
          }
        >
          <TextInput
            inputMode={isTextUnit(unit) ? 'text' : 'decimal'}
            autoComplete="off"
            value={value}
            suffix={suffix}
            onChange={(e) => setValue(e.target.value)}
          />
        </Field>
      )}

      {canBeEmpty ? (
        <Checkbox
          label="Brak wartości (parametr pusty od tej daty)"
          checked={empty}
          onChange={(e) => {
            setEmpty(e.target.checked)
            setClientErrors((prev) => ({ ...prev, value: '' }))
          }}
        />
      ) : null}

      <Field label="Obowiązuje od" error={errors.validFrom}>
        <DateInput value={validFrom} onChange={(e) => setValidFrom(e.target.value)} />
      </Field>

      <Field label="Podstawa prawna" optional error={errors.legalBasis} description="Bez wpisu zostanie zachowana dotychczasowa podstawa.">
        <TextInput value={legalBasis} onChange={(e) => setLegalBasis(e.target.value)} maxLength={500} />
      </Field>

      <Field label="Powód zmiany" optional error={errors.reason}>
        <TextInput value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} />
      </Field>

      <Field
        label="Status nowej wartości"
        group
        error={errors.status}
        description={`Domyślnie jak poprzednia wartość (${previousStatus === 'do_weryfikacji' ? 'do weryfikacji' : 'zweryfikowana'}).`}
      >
        <RadioGroup
          name={`status-${item.key}`}
          value={status}
          onChange={(e) => setStatus(e.target.value as ParameterStatus)}
          options={[
            { value: 'do_weryfikacji', label: 'Do weryfikacji', description: 'Wartość wymaga potwierdzenia w przepisach lub materiałach ZUS.' },
            { value: 'ok', label: 'Zweryfikowana', description: 'Wartość potwierdzona.' },
          ]}
        />
      </Field>

      {errors.key ? (
        <Alert variant="error" title="Błąd klucza">
          {errors.key}
        </Alert>
      ) : null}
      {general ? (
        <Alert variant="error" title="Nie zapisano zmiany">
          {general}
        </Alert>
      ) : null}

      <div className="flex flex-wrap gap-3">
        <Button type="submit" loading={save.isPending} loadingLabel="Zapisuję…">
          Zapisz wartość
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={save.isPending}>
          Anuluj
        </Button>
      </div>
    </form>
  )
}
