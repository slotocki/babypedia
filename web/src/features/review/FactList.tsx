import { useState, type FormEvent } from 'react'
import type { FactSet } from '@/app/api/types'
import { Button, Card, Section, SourceBadge } from '@/ui'
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
          return (
            <Section key={group.bucket} level={3} title={group.title}>
              {keys.length === 0 ? (
                <p className="text-ink-soft">{group.empty}</p>
              ) : (
                <ul className="space-y-3">
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
            </Section>
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

  const lines = formatFactValue(factKey, fact.value).split('\n')

  return (
    <li>
      <Card className="space-y-3 p-4 sm:p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0 space-y-1">
            <p className="text-sm font-medium text-ink-soft">{label}</p>
            {lines.length > 1 ? (
              <ul className="space-y-1 text-base font-semibold text-ink">
                {lines.map((line, index) => (
                  <li key={index}>{line}</li>
                ))}
              </ul>
            ) : (
              <p className="break-words text-base font-semibold text-ink">{lines[0]}</p>
            )}
            <SourceBadge source={fact.source} className="pt-1" />
          </div>
          {editable && !editing ? (
            <Button
              id={`popraw-${rowId}`}
              variant="secondary"
              aria-label={`Popraw: ${label}`}
              className="shrink-0"
              onClick={() => {
                setDraft(draftFromValue(meta.kind, fact.value))
                setError(null)
                onEdit()
              }}
            >
              Popraw
            </Button>
          ) : null}
          {!editable ? <p className="text-sm text-ink-soft sm:max-w-48 sm:text-right">Tej informacji nie poprawiasz tutaj.</p> : null}
        </div>

        {editing && meta ? (
          <form onSubmit={submit} noValidate className="space-y-3 rounded-card border border-line bg-neutral-soft p-3">
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
      </Card>
    </li>
  )
}
