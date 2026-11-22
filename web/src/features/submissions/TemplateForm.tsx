import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, api } from '@/app/api/client'
import type { BenefitResult, LeaveTemplate, SubmissionReceipt } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { downloadBlob } from './api'
import { Alert, Button, DateInput, ErrorState, Field, NumberInput, RadioGroup, Select, SkeletonText, TextInput } from '@/ui'
import type { SubmissionTemplate, TemplateField } from './api'
import { collectFields, legalBasis, prefill, splitProblem, validateLocally, visibleFields, type Values } from './form'
import { NeedsLogin } from './NeedsLogin'
import { TextArea } from './TextArea'

interface TemplateFormProps {
  template: SubmissionTemplate
  /** null = bez logowania: tylko PDF (wniosek do pracodawcy, zestawienie ZAM), bez zapisu. */
  sessionId: string | null
  onDone: (receipt: SubmissionReceipt) => void
}

const RADIO_LIMIT = 4

export function TemplateForm(props: TemplateFormProps) {
  const { facts } = useCaseState()
  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj'])
  // Daty z planu (wynik silnika) wstępnie wypełniają formularz; bez wyniku formularz działa jak dotąd.
  const assessment = useQuery({
    queryKey: ['assessment', facts],
    queryFn: () => api.assess({ facts }),
    enabled: hasEvent,
    retry: false,
  })
  if (hasEvent && assessment.isPending) return <SkeletonText lines={6} label="Wczytuję Twój plan…" />
  return <TemplateFormInner {...props} benefits={assessment.data?.benefits} />
}

function TemplateFormInner({ template, sessionId, onDone, benefits }: TemplateFormProps & { benefits?: BenefitResult[] }) {
  const { facts, profileName } = useCaseState()
  const queryClient = useQueryClient()
  const formRef = useRef<HTMLFormElement>(null)
  const [prefilled] = useState(() => prefill(template, facts, profileName, benefits))
  const [values, setValues] = useState<Values>(prefilled)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [general, setGeneral] = useState<string[]>([])
  const [focusToken, setFocusToken] = useState(0)
  const [touched, setTouched] = useState<Set<string>>(new Set())

  const [anonymousPdf, setAnonymousPdf] = useState<Blob | null>(null)

  const mutation = useMutation({
    mutationFn: async (fields: Values): Promise<Blob | SubmissionReceipt> => {
      const body = {
        type: template.type,
        template: template.type === 'wniosek_urlop' ? (template.template as LeaveTemplate) : undefined,
        fields,
      }
      return sessionId ? api.submissions.create(sessionId, body) : api.submissions.document(body)
    },
    onSuccess: (result) => {
      if (result instanceof Blob) {
        setAnonymousPdf(result)
        downloadBlob(result, `${template.id}.pdf`)
        return
      }
      void queryClient.invalidateQueries({ queryKey: ['submissions', 'mine'] })
      onDone(result)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 400) {
        const { byField, general: rest } = splitProblem(error.problem)
        setErrors(byField)
        setGeneral(rest)
        setFocusToken((n) => n + 1)
      }
    },
  })

  const fields = visibleFields(template, values)

  useEffect(() => {
    if (focusToken === 0) return
    const first = fields.find((field) => errors[field.key])
    if (!first) return
    formRef.current?.querySelector<HTMLElement>(`[data-field-key="${first.key}"] :is(input, select, textarea)`)?.focus()
    // fokus tylko po nowej próbie wysłania
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [focusToken])

  const change = (key: string, value: string) => {
    setValues((current) => ({ ...current, [key]: value }))
    setTouched((current) => new Set(current).add(key))
    setErrors((current) => {
      if (!current[key]) return current
      const rest = { ...current }
      delete rest[key]
      return rest
    })
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setGeneral([])
    const local = validateLocally(fields, values)
    if (Object.keys(local).length > 0) {
      setErrors(local)
      setFocusToken((n) => n + 1)
      return
    }
    setErrors({})
    mutation.mutate(collectFields(fields, values))
  }

  const errorCount = Object.keys(errors).length
  const unauthorized = mutation.error instanceof ApiError && mutation.error.status === 401
  const serverProblem = mutation.error && !(mutation.error instanceof ApiError && mutation.error.status === 400) && !unauthorized

  if (anonymousPdf) {
    return (
      <div className="space-y-4">
        <Alert variant="info" title="PDF jest gotowy">
          Plik został pobrany. Nic nie zostało zapisane na serwerze ani wysłane do adresata.
        </Alert>
        <Button onClick={() => downloadBlob(anonymousPdf, `${template.id}.pdf`)}>Pobierz PDF ponownie</Button>
      </div>
    )
  }

  if (unauthorized) return <NeedsLogin expired templateId={template.id} />

  const preparesPdf = template.type === 'wniosek_urlop' || template.type === 'dane_zam'

  return (
    <form ref={formRef} onSubmit={submit} noValidate className="space-y-6">
      <p className="text-ink-soft">
        Pola bez dopisku „opcjonalnie” są wymagane. Nie wpisuj numeru PESEL — nie jest nam potrzebny. Przy polach z ikoną „?” znajdziesz podstawę prawną.
      </p>

      {errorCount > 0 || general.length > 0 ? (
        <Alert variant="error" title={errorCount > 0 ? `Popraw pola zaznaczone na czerwono (${errorCount})` : 'Nie udało się wysłać wniosku'}>
          {general.length > 0 ? (
            <ul className="list-disc space-y-1 pl-5">
              {general.map((message) => (
                <li key={message}>{message}</li>
              ))}
            </ul>
          ) : null}
        </Alert>
      ) : null}

      {fields.map((field) => (
        <div key={field.key} data-field-key={field.key}>
          <FieldControl field={field} value={values[field.key] ?? ''} error={errors[field.key]} prefilled={prefilled[field.key] !== undefined && !touched.has(field.key)} onChange={(value) => change(field.key, value)} />
        </div>
      ))}

      {serverProblem ? <ErrorState error={mutation.error} /> : null}

      <Button type="submit" fullWidth loading={mutation.isPending} loadingLabel={preparesPdf ? 'Przygotowuję PDF…' : 'Wysyłam (symulacja)…'} className="sm:w-auto">
        {preparesPdf ? 'Przygotuj PDF' : 'Wyślij (symulacja)'}
      </Button>
      <p className="text-sm text-ink-soft">Symulacja: nic nie zostanie wysłane do pracodawcy, ZUS ani urzędu.{sessionId ? '' : ' Bez logowania dostajesz sam plik PDF — nie zapisujemy go na serwerze.'}</p>
    </form>
  )
}

interface FieldControlProps {
  field: TemplateField
  value: string
  error?: string
  prefilled: boolean
  onChange: (value: string) => void
}

function FieldControl({ field, value, error, prefilled, onChange }: FieldControlProps) {
  const basis = legalBasis(field)
  const description = prefilled ? <span className="block font-medium text-brand-strong">Wstępnie wypełnione z Twoich danych i planu — sprawdź.</span> : undefined
  const help = basis ? `Podstawa prawna: ${basis}` : undefined

  const options = field.options ?? []
  const asRadio = field.kind === 'choice' && options.length <= RADIO_LIMIT

  return (
    <Field label={field.label} description={description} help={help} error={error} optional={!field.required} group={asRadio}>
      {field.kind === 'text' ? (
        <TextInput value={value} maxLength={field.maxLength} onChange={(e) => onChange(e.target.value)} autoComplete="off" />
      ) : null}
      {field.kind === 'longtext' ? <TextArea value={value} maxLength={field.maxLength} onChange={(e) => onChange(e.target.value)} /> : null}
      {field.kind === 'date' ? <DateInput value={value} onChange={(e) => onChange(e.target.value)} /> : null}
      {field.kind === 'integer' ? <NumberInput value={value} inputMode="numeric" min={0} step={1} onChange={(e) => onChange(e.target.value)} /> : null}
      {asRadio ? (
        <RadioGroup name={field.key} value={value} options={options} columns={options.length === 2 ? 2 : 1} onChange={(e) => onChange(e.target.value)} />
      ) : null}
      {field.kind === 'choice' && !asRadio ? (
        <Select value={value} onChange={(e) => onChange(e.target.value)}>
          <option value="">Wybierz…</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      ) : null}
    </Field>
  )
}
