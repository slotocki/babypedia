import { useState, type FormEvent } from 'react'
import type { FactSet } from '@/app/api/types'
import { Button, Section, SourceBadge, UserIcon } from '@/ui'
import { AnswerField } from './AnswerField'
import { draftFromValue, parseDraft, type Draft } from './answers'
import { factKeyOrder, factMeta, formatFactValue, isFactKey, labelFor, optionsOf } from './fact-labels'
import { isExampleAnswer } from './examples'
import { factsIn, type AnyFact, type Bucket } from './facts'

const groups: { bucket: Bucket; title: string; empty: string }[] = [
  { bucket: 'sprawa', title: 'Zdarzenie i dziecko', empty: 'Brak danych o zdarzeniu i dziecku.' },
  { bucket: 'wnioskodawca', title: 'Ty', empty: 'Brak danych o Tobie. Uzupełnisz je w pytaniach poniżej.' },
  { bucket: 'drugiRodzic', title: 'Drugi rodzic', empty: 'Brak danych o drugim rodzicu. Uzupełnisz je w pytaniach poniżej.' },
]

interface FactListProps {
  facts: FactSet
  onChange: (bucket: Bucket, key: string, value: unknown) => void
  subset?: 'essential' | 'other'
  /** Sesja symulowanego logowania — odpowiedzi z pliku osoby testowej oznaczamy jako przykład. */
  sessionId?: string | null
}

const essentialKeys = new Set([
  'zdarzenie.rodzaj', 'zdarzenie.data', 'zdarzenie.dlaKogo', 'dziecko.liczba',
  'osoba.rola', 'ubezpieczenie.tytuly', 'skladki.zadluzenie', 'podstawa.ustalonaPrzezPlatnika',
])

export function FactList({ facts, onChange, subset, sessionId = null }: FactListProps) {
  const [editing, setEditing] = useState<string | null>(null)
  const showSecond = Boolean(facts.drugiRodzic) || facts.sprawa['zdarzenie.dlaKogo']?.value === 'ja_i_drugi_rodzic'

  return (
    <div className="space-y-8">
      {groups
        .filter((g) => g.bucket !== 'drugiRodzic' || showSecond)
        .map((group) => {
          const map = factsIn(facts, group.bucket)
          const keys = factKeyOrder.filter((key) => map[key] !== undefined && (
            subset === undefined || essentialKeys.has(key) === (subset === 'essential')
          ))
          if (subset === 'other' && keys.length === 0) return null
          return (
            <Section key={group.bucket} level={3} title={group.title}>
              {keys.length === 0 ? (
                <p className="text-ink-soft">{group.empty}</p>
              ) : (
                <ul className="divide-y divide-line">
                  {keys.map((key) => {
                    const rowId = `${group.bucket}:${key}`
                    return (
                      <FactRow
                        key={rowId}
                        rowId={rowId}
                        factKey={key}
                        fact={map[key] as AnyFact}
                        facts={facts}
                        example={isExampleAnswer(sessionId, group.bucket, key, map[key])}
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
  example: boolean
  editing: boolean
  onEdit: () => void
  onCancel: () => void
  onSave: (value: unknown) => void
}

function FactRow({ rowId, factKey, fact, facts, example, editing, onEdit, onCancel, onSave }: FactRowProps) {
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

  // Kreator zapisuje odpowiedź „więcej niż 20 osób” jako 21, a „20 lub mniej” jako 20 — pokazujemy odpowiedź, nie liczbę.
  const headcountAnswer =
    factKey === 'platnik.liczbaUbezpieczonych' && fact.source === 'user' && (fact.value === 20 || fact.value === 21)
      ? fact.value === 21 ? 'Więcej niż 20 osób' : '20 osób lub mniej'
      : null
  const lines = (headcountAnswer ?? formatFactValue(factKey, fact.value)).split('\n')

  return (
    <li className="space-y-3 py-3">
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-1">
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
          {example ? (
            <span className="mt-1 inline-flex items-center gap-1.5 rounded-pill border border-line-strong bg-surface px-2.5 py-0.5 text-sm font-medium text-ink-soft">
              <UserIcon className="size-4 shrink-0" />
              <span className="sr-only">Źródło: </span>
              Twoje odpowiedzi (przykład)
            </span>
          ) : (
            <SourceBadge source={fact.source} className="pt-1" />
          )}
        </div>
        {editable && !editing ? (
          <Button
            id={`popraw-${rowId}`}
            variant="link"
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
    </li>
  )
}
