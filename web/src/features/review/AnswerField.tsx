import { Button, DateInput, Field, MoneyInput, RadioGroup, Select, TextInput, type ChoiceOption } from '@/ui'
import type { Draft } from './answers'
import type { FactKind } from './fact-labels'

interface AnswerFieldProps {
  id: string
  label: string
  kind: FactKind
  options: ChoiceOption[]
  draft: Draft
  onChange: (draft: Draft) => void
  error?: string | null
  hint?: string
  autoFocus?: boolean
  unit?: string
}

export function AnswerField({ id, label, kind, options, draft, onChange, error, hint, autoFocus, unit }: AnswerFieldProps) {
  const set = (patch: Partial<Draft>) => onChange({ ...draft, ...patch })
  const message = error ?? undefined

  switch (kind) {
    case 'date':
      return (
        <Field label={label} description={hint} error={message}>
          <DateInput autoFocus={autoFocus} value={draft.text} onChange={(e) => set({ text: e.target.value })} className="sm:max-w-xs" />
        </Field>
      )

    case 'money': {
      const description = [hint, 'Kwota w złotych, np. 6500,50.'].filter(Boolean).join(' ')
      return (
        <Field label={label} description={description} error={message}>
          <MoneyInput autoFocus={autoFocus} value={draft.text} onChange={(e) => set({ text: e.target.value })} className="sm:max-w-xs" />
        </Field>
      )
    }

    case 'integer':
    case 'number': {
      const description = [hint, unit ? `Podaj w jednostce: ${unit}.` : undefined].filter(Boolean).join(' ') || undefined
      return (
        <Field label={label} description={description} error={message}>
          <TextInput
            type="text"
            inputMode={kind === 'integer' ? 'numeric' : 'decimal'}
            autoComplete="off"
            autoFocus={autoFocus}
            suffix={unit}
            value={draft.text}
            onChange={(e) => set({ text: e.target.value })}
            className="sm:max-w-xs"
          />
        </Field>
      )
    }

    case 'boolean':
      return (
        <Field group label={label} description={hint} error={message}>
          <RadioGroup
            name={id}
            columns={2}
            value={draft.text}
            onChange={(e) => set({ text: e.target.value })}
            options={[
              { value: 'tak', label: 'Tak' },
              { value: 'nie', label: 'Nie' },
            ]}
          />
        </Field>
      )

    case 'enum':
      if (options.length <= 4) {
        return (
          <Field group label={label} description={hint} error={message}>
            <RadioGroup name={id} value={draft.text} onChange={(e) => set({ text: e.target.value })} options={options} />
          </Field>
        )
      }
      return (
        <Field label={label} description={hint} error={message}>
          <Select autoFocus={autoFocus} value={draft.text} onChange={(e) => set({ text: e.target.value })}>
            <option value="">Wybierz odpowiedź</option>
            {options.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </Select>
        </Field>
      )

    case 'sytuacjaMatki':
      return (
        <div className="space-y-4">
          <Field group label={label} description={hint} error={message}>
            <RadioGroup name={id} value={draft.text} onChange={(e) => set({ text: e.target.value })} options={options} />
          </Field>
          <Field label="Od kiedy trwa ta sytuacja">
            <DateInput value={draft.od} onChange={(e) => set({ od: e.target.value })} className="sm:max-w-xs" />
          </Field>
          <Field label="Do kiedy (jeśli już się skończyła)" optional>
            <DateInput value={draft.to} onChange={(e) => set({ to: e.target.value })} className="sm:max-w-xs" />
          </Field>
        </div>
      )

    case 'dateRanges': {
      const update = (index: number, patch: Partial<{ od: string; do: string }>) =>
        set({ rows: draft.rows.map((r, i) => (i === index ? { ...r, ...patch } : r)) })
      return (
        <Field group label={label} description={hint} error={message}>
          {draft.rows.length > 0 ? (
            <ul className="space-y-3">
              {draft.rows.map((row, index) => (
                <li key={index} className="space-y-3 rounded-card border border-line bg-surface p-3">
                  <p className="text-sm font-semibold text-ink-soft">Pobyt {index + 1}</p>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <Field label="Od">
                      <DateInput aria-invalid={error ? true : undefined} value={row.od} onChange={(e) => update(index, { od: e.target.value })} />
                    </Field>
                    <Field label="Do">
                      <DateInput aria-invalid={error ? true : undefined} value={row.do} onChange={(e) => update(index, { do: e.target.value })} />
                    </Field>
                  </div>
                  <Button variant="link" onClick={() => set({ rows: draft.rows.filter((_, i) => i !== index) })}>
                    Usuń pobyt {index + 1}
                  </Button>
                </li>
              ))}
            </ul>
          ) : null}
          <Button variant="secondary" onClick={() => set({ rows: [...draft.rows, { od: '', do: '' }] })}>
            Dodaj pobyt
          </Button>
        </Field>
      )
    }

    case 'readonly':
      return null
  }
}
