import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ApiError, api } from '@/app/api/client'
import type { LeaveTemplate, SubmissionReceipt } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { Alert, Button, DateInput, ErrorState, Field, NumberInput, RadioGroup, Select, TextInput } from '@/ui'
import type { SubmissionTemplate, TemplateField } from './api'
import { collectFields, legalBasis, prefill, splitProblem, validateLocally, visibleFields, type Values } from './form'
import { NeedsLogin } from './NeedsLogin'
import { TextArea } from './TextArea'

interface TemplateFormProps {
  template: SubmissionTemplate
  sessionId: string
  onDone: (receipt: SubmissionReceipt) => void
}

const RADIO_LIMIT = 4

export function TemplateForm({ template, sessionId, onDone }: TemplateFormProps) {
  const { facts } = useCaseState()
  const queryClient = useQueryClient()
  const formRef = useRef<HTMLFormElement>(null)
  const [prefilled] = useState(() => prefill(template, facts))
  const [values, setValues] = useState<Values>(prefilled)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [general, setGeneral] = useState<string[]>([])
  const [focusToken, setFocusToken] = useState(0)
  const [touched, setTouched] = useState<Set<string>>(new Set())

  const mutation = useMutation({
    mutationFn: (fields: Values) =>
      api.submissions.create(sessionId, {
        type: template.type,
        template: template.type === 'wniosek_urlop' ? (template.template as LeaveTemplate) : undefined,
        fields,
      }),
    onSuccess: (receipt) => {
      void queryClient.invalidateQueries({ queryKey: ['submissions', 'mine'] })
      onDone(receipt)
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

  if (unauthorized) return <NeedsLogin expired templateId={template.id} />

  const preparesPdf = template.type === 'wniosek_urlop' || template.type === 'dane_zam'

  return (
    <form ref={formRef} onSubmit={submit} noValidate className="space-y-6">
      <p className="text-ink-soft">
        Pola bez dopisku „opcjonalnie” są wymagane. Nie wpisuj numeru PESEL — nie jest nam potrzebny.
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
      <p className="text-sm text-ink-soft">Symulacja: nic nie zostanie wysłane do pracodawcy, ZUS ani urzędu.</p>
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
  const description =
    basis || prefilled ? (
      <>
        {basis ? <span className="block">Podstawa: {basis}</span> : null}
        {prefilled ? <span className="block font-medium text-brand-strong">Wstępnie wypełnione z Twoich danych — sprawdź.</span> : null}
      </>
    ) : undefined

  const options = field.options ?? []
  const asRadio = field.kind === 'choice' && options.length <= RADIO_LIMIT

  return (
    <Field label={field.label} description={description} error={error} optional={!field.required} group={asRadio}>
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
