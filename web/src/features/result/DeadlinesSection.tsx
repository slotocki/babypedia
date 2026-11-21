import type { BenefitResult, Deadline } from '@/app/api/types'
import { Card, Checkbox, DeadlineItem, Disclosure, EmptyState, Section, WarningIcon } from '@/ui'
import { benefitName, personLabels } from './labels'

export interface DeadlineEntry {
  deadline: Deadline
  benefit: BenefitResult
}

export interface DeadlinesSectionProps {
  entries: DeadlineEntry[]
  completed: string[]
  nextDeadlineId?: string | null
  showPerson: boolean
  today: string
  onToggle: (deadlineId: string, done: boolean) => void
}

function Entry({
  entry,
  done,
  isNext,
  showPerson,
  today,
  onToggle,
}: {
  entry: DeadlineEntry
  done: boolean
  isNext: boolean
  showPerson: boolean
  today: string
  onToggle: DeadlinesSectionProps['onToggle']
}) {
  const { deadline, benefit } = entry
  const past = deadline.date < today
  return (
    <li className="space-y-2 p-4">
      <DeadlineItem deadline={deadline} className="border-0 bg-transparent p-0" />
      <div className="space-y-1 pl-9 text-sm text-ink-soft">
        <p className="break-words">
          Dotyczy: <span className="font-semibold text-ink">{benefitName(benefit.id)}</span>
          {showPerson ? ` (${personLabels[benefit.person]})` : null}
        </p>
        {deadline.basis ? <p className="break-words">Podstawa prawna: {deadline.basis}</p> : null}
        {isNext && !done ? <p className="font-semibold text-brand-strong">To jest Twój najbliższy krok.</p> : null}
        {past && !done ? (
          <p className="flex items-center gap-1.5 font-semibold text-bad">
            <WarningIcon className="size-4 shrink-0" />
            Termin minął. Sprawdź, co zrobić dalej, i w razie wątpliwości zapytaj ZUS.
          </p>
        ) : null}
      </div>
      <Checkbox
        checked={done}
        onChange={(event) => onToggle(deadline.id, event.target.checked)}
        label={
          <>
            Zrobione<span className="sr-only">: {deadline.text}</span>
          </>
        }
      />
    </li>
  )
}

export function DeadlinesSection({ entries, completed, nextDeadlineId, showPerson, today, onToggle }: DeadlinesSectionProps) {
  const sorted = [...entries].sort((a, b) => a.deadline.date.localeCompare(b.deadline.date))
  const open = sorted.filter((e) => !completed.includes(e.deadline.id))
  const done = sorted.filter((e) => completed.includes(e.deadline.id))

  const renderList = (list: DeadlineEntry[], isDone: boolean) => (
    <ul className="divide-y divide-line">
      {list.map((entry) => (
        <Entry
          key={`${entry.benefit.id}:${entry.benefit.person}:${entry.deadline.id}`}
          entry={entry}
          done={isDone}
          isNext={entry.deadline.id === nextDeadlineId}
          showPerson={showPerson}
          today={today}
          onToggle={onToggle}
        />
      ))}
    </ul>
  )

  return (
    <div id="terminy" className="scroll-mt-4">
      <Section title="Co zrobić i do kiedy" description="Zaznacz „Zrobione”, żeby przejść do kolejnego kroku.">
        {entries.length === 0 ? (
          <EmptyState title="Nie ma terminów do pilnowania" description="Gdy pojawią się terminy związane z Twoimi świadczeniami, zobaczysz je tutaj." />
        ) : (
          <div className="space-y-6">
            {open.length > 0 ? <Card className="p-0 sm:p-0">{renderList(open, false)}</Card> : <p className="text-ink-soft">Wszystkie terminy są oznaczone jako zrobione.</p>}
            {done.length > 0 ? (
              <Disclosure title={`Zrobione (${done.length})`}>{renderList(done, true)}</Disclosure>
            ) : null}
          </div>
        )}
      </Section>
    </div>
  )
}
