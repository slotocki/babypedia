import { useState, type FormEvent } from 'react'
import type { FactSet } from '@/app/api/types'
import { Button } from '@/features/intake/components/Button'
import { SourceBadge } from '@/features/intake/components/Badges'
import { AnswerField } from './AnswerField'
import { draftFromValue, parseDraft, type Draft } from './answers'
import { factKeyOrder, factMeta, formatFactValue, isFactKey, labelFor, optionsOf } from './fact-labels'
import { factsIn, type AnyFact, type Bucket } from './facts'

const groups: { bucket: Bucket; title: string; empty: string }[] = [
  { bucket: 'sprawa', title: 'Zdarzenie i dziecko', empty: 'Brak danych o zdarzeniu i dziecku.' },
  { bucket: 'wnioskodawca', title: 'Ty', empty: 'Brak danych o Tobie. Uzupełnisz je w pytaniach poniżej.' },
  { bucket: 'drugiRodzic', title: 'Drugi rodzic', empty: 'Brak danych o drugim rodzicu. Uzupełnisz je w pytaniach poniżej.' },
]

interface FactListProps {
  facts: FactSet
  onChange: (bucket: Bucket, key: string, value: unknown) => void
}

export function FactList({ facts, onChange }: FactListProps) {
  const [editing, setEditing] = useState<string | null>(null)
  const showSecond = Boolean(facts.drugiRodzic) || facts.sprawa['zdarzenie.dlaKogo']?.value === 'ja_i_drugi_rodzic'

  return (
    <div className="space-y-8">
      {groups
        .filter((g) => g.bucket !== 'drugiRodzic' || showSecond)
        .map((group) => {
          const map = factsIn(facts, group.bucket)
          const keys = factKeyOrder.filter((key) => map[key] !== undefined)
          const headingId = `grupa-${group.bucket}`
          return (
            <section key={group.bucket} aria-labelledby={headingId}>
              <h3 id={headingId} className="text-lg font-semibold text-slate-900">
                {group.title}
              </h3>
              {keys.length === 0 ? (
                <p className="mt-2 text-slate-700">{group.empty}</p>
              ) : (
                <ul className="mt-2 divide-y divide-slate-200 border-y border-slate-200">
                  {keys.map((key) => {
                    const rowId = `${group.bucket}:${key}`
                    return (
                      <FactRow
                        key={rowId}
                        rowId={rowId}
                        factKey={key}
                        fact={map[key] as AnyFact}
                        facts={facts}
                        editing={editing === rowId}
                        onEdit={() => setEditing(rowId)}
                        onCancel={() => closeEditor(rowId, setEditing)}
                        onSave={(value) => {
                          onChange(group.bucket, key, value)
                          closeEditor(rowId, setEditing)
                        }}
                      />
                    )
                  })}
                </ul>
              )}
            </section>
          )
        })}
    </div>
  )
}

function closeEditor(rowId: string, setEditing: (id: string | null) => void) {
  setEditing(null)
  requestAnimationFrame(() => document.getElementById(`popraw-${rowId}`)?.focus())
}

interface FactRowProps {
  rowId: string
  factKey: string
  fact: AnyFact
  facts: FactSet
  editing: boolean
  onEdit: () => void
  onCancel: () => void
  onSave: (value: unknown) => void
}

function FactRow({ rowId, factKey, fact, facts, editing, onEdit, onCancel, onSave }: FactRowProps) {
  const meta = isFactKey(factKey) ? factMeta[factKey] : undefined
  const label = labelFor(factKey, facts)
  const editable = meta !== undefined && meta.kind !== 'readonly'
  const options = optionsOf(factKey)
  const [draft, setDraft] = useState<Draft>(() => draftFromValue(meta?.kind ?? 'readonly', fact.value))
  const [error, setError] = useState<string | null>(null)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!meta) return
    const form = event.currentTarget as HTMLFormElement
    const result = parseDraft(meta.kind, draft, meta, options.map((o) => o.value))
    if (!result.ok) {
      setError(result.error)
      setTimeout(() => form.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(), 0)
      return
    }
    setError(null)
    onSave(result.value)
  }

  return (
    <li className="py-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <p className="text-sm font-medium text-slate-700">{label}</p>
          <p className="mt-0.5 whitespace-pre-line break-words text-base font-semibold text-slate-900">
            {formatFactValue(factKey, fact.value)}
          </p>
          <p className="mt-1">
            <SourceBadge source={fact.source} />
          </p>
        </div>
        {editable && !editing ? (
          <Button
            id={`popraw-${rowId}`}
            variant="secondary"
            aria-label={`Popraw: ${label}`}
            onClick={() => {
              setDraft(draftFromValue(meta.kind, fact.value))
              setError(null)
              onEdit()
            }}
          >
            Popraw
          </Button>
        ) : null}
        {!editable ? <p className="text-sm text-slate-700 sm:max-w-48 sm:text-right">Tych danych nie poprawiasz tutaj.</p> : null}
      </div>

      {editing && meta ? (
        <form onSubmit={submit} noValidate className="mt-3 space-y-3 rounded-lg border border-slate-300 bg-slate-50 p-3">
          <AnswerField
            id={`wartosc-${rowId}`}
            label={`Nowa wartość: ${label}`}
            kind={meta.kind}
            options={options}
            draft={draft}
            onChange={setDraft}
            error={error}
            autoFocus
            unit={meta.unit}
            hint="Po zapisaniu źródłem tej informacji będziesz Ty."
          />
          <div className="flex flex-wrap gap-2">
            <Button type="submit">Zapisz</Button>
            <Button variant="secondary" onClick={onCancel}>
              Anuluj
            </Button>
          </div>
        </form>
      ) : null}
    </li>
  )
}
