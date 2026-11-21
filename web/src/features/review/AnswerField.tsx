import { Button } from '@/features/intake/components/Button'
import { cx, describedBy, inputClass } from '@/features/intake/components/cx'
import { Field, FieldError } from '@/features/intake/components/Field'
import { RadioCards, type RadioOption } from '@/features/intake/components/RadioCards'
import type { Draft } from './answers'
import type { FactKind } from './fact-labels'

interface AnswerFieldProps {
  id: string
  label: string
  kind: FactKind
  options: RadioOption[]
  draft: Draft
  onChange: (draft: Draft) => void
  error?: string | null
  hint?: string
  autoFocus?: boolean
  unit?: string
}

export function AnswerField({ id, label, kind, options, draft, onChange, error, hint, autoFocus, unit }: AnswerFieldProps) {
  const set = (patch: Partial<Draft>) => onChange({ ...draft, ...patch })

  switch (kind) {
    case 'date':
      return (
        <Field id={id} label={label} hint={hint} error={error}>
          {(aria) => (
            <input
              {...aria}
              type="date"
              autoFocus={autoFocus}
              value={draft.text}
              onChange={(e) => set({ text: e.target.value })}
              className={inputClass}
            />
          )}
        </Field>
      )

    case 'integer':
    case 'number':
    case 'money': {
      const unitHint = kind === 'money' ? 'Kwota w złotych, np. 6500,50.' : unit ? `Podaj w jednostce: ${unit}.` : undefined
      const fullHint = [hint, unitHint].filter(Boolean).join(' ') || undefined
      return (
        <Field id={id} label={label} hint={fullHint} error={error}>
          {(aria) => (
            <input
              {...aria}
              type="text"
              inputMode={kind === 'integer' ? 'numeric' : 'decimal'}
              autoComplete="off"
              autoFocus={autoFocus}
              value={draft.text}
              onChange={(e) => set({ text: e.target.value })}
              className={cx(inputClass, 'sm:max-w-xs')}
            />
          )}
        </Field>
      )
    }

    case 'boolean':
      return (
        <RadioCards
          id={id}
          legend={label}
          hint={hint}
          error={error}
          columns={2}
          value={draft.text}
          onChange={(text) => set({ text })}
          options={[
            { value: 'tak', label: 'Tak' },
            { value: 'nie', label: 'Nie' },
          ]}
        />
      )

    case 'enum':
      if (options.length <= 4) {
        return (
          <RadioCards id={id} legend={label} hint={hint} error={error} value={draft.text} onChange={(text) => set({ text })} options={options} />
        )
      }
      return (
        <Field id={id} label={label} hint={hint} error={error}>
          {(aria) => (
            <select {...aria} autoFocus={autoFocus} value={draft.text} onChange={(e) => set({ text: e.target.value })} className={inputClass}>
              <option value="">Wybierz odpowiedź</option>
              {options.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          )}
        </Field>
      )

    case 'sytuacjaMatki': {
      const dateId = `${id}-od`
      const toId = `${id}-do`
      return (
        <div className="space-y-4">
          <RadioCards id={id} legend={label} hint={hint} error={error} value={draft.text} onChange={(text) => set({ text })} options={options} />
          <Field id={dateId} label="Od kiedy trwa ta sytuacja">
            {(aria) => <input {...aria} type="date" value={draft.od} onChange={(e) => set({ od: e.target.value })} className={inputClass} />}
          </Field>
          <Field id={toId} label="Do kiedy (jeśli już się skończyła)">
            {(aria) => <input {...aria} type="date" value={draft.to} onChange={(e) => set({ to: e.target.value })} className={inputClass} />}
          </Field>
        </div>
      )
    }

    case 'dateRanges':
      return (
        <fieldset aria-describedby={describedBy(id, hint, error)}>
          <legend className="mb-1 text-base font-semibold text-slate-900">{label}</legend>
          {hint ? (
            <p id={`${id}-hint`} className="text-sm text-slate-700">
              {hint}
            </p>
          ) : null}
          <ul className="mt-2 space-y-3">
            {draft.rows.map((row, index) => (
              <li key={index} className="rounded-lg border border-slate-300 p-3">
                <p className="text-sm font-semibold text-slate-800">Pobyt {index + 1}</p>
                <div className="mt-2 grid gap-3 sm:grid-cols-2">
                  <Field id={`${id}-${index}-od`} label="Od">
                    {(aria) => (
                      <input
                        {...aria}
                        aria-invalid={Boolean(error)}
                        type="date"
                        value={row.od}
                        onChange={(e) => set({ rows: draft.rows.map((r, i) => (i === index ? { ...r, od: e.target.value } : r)) })}
                        className={inputClass}
                      />
                    )}
                  </Field>
                  <Field id={`${id}-${index}-do`} label="Do">
                    {(aria) => (
                      <input
                        {...aria}
                        aria-invalid={Boolean(error)}
                        type="date"
                        value={row.do}
                        onChange={(e) => set({ rows: draft.rows.map((r, i) => (i === index ? { ...r, do: e.target.value } : r)) })}
                        className={inputClass}
                      />
                    )}
                  </Field>
                </div>
                <Button variant="link" className="mt-2" onClick={() => set({ rows: draft.rows.filter((_, i) => i !== index) })}>
                  Usuń pobyt {index + 1}
                </Button>
              </li>
            ))}
          </ul>
          <Button variant="secondary" className="mt-3" onClick={() => set({ rows: [...draft.rows, { od: '', do: '' }] })}>
            Dodaj pobyt
          </Button>
          {error ? <FieldError id={id}>{error}</FieldError> : null}
        </fieldset>
      )

    case 'readonly':
      return null
  }
}
