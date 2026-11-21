import type { BenefitResult, Deadline } from '@/app/api/types'
import { Checkbox, DeadlineItem, EmptyState, Section, WarningIcon } from '@/ui'
import { benefitName, deadlineTypeLabels, personLabels } from './labels'

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
    <li className={done ? 'space-y-2 opacity-80' : 'space-y-2'}>
      <DeadlineItem deadline={deadline} />
      <div className="space-y-1 px-1 text-sm text-ink-soft">
        <p className="break-words">
          Dotyczy: <span className="font-semibold text-ink">{benefitName(benefit.id)}</span>
          {showPerson ? ` (${personLabels[benefit.person]})` : null}
        </p>
        <p>Rodzaj terminu: {deadlineTypeLabels[deadline.type]}</p>
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
    <ul className="space-y-6">
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
      <Section title="Co zrobić i do kiedy" description="Wszystkie terminy od najwcześniejszego. Gdy coś załatwisz, zaznacz „Zrobione”, a przeliczymy, co jest następne.">
        {entries.length === 0 ? (
          <EmptyState title="Nie ma terminów do pilnowania" description="Gdy pojawią się terminy związane z Twoimi świadczeniami, zobaczysz je tutaj." />
        ) : (
          <div className="space-y-6">
            {open.length > 0 ? renderList(open, false) : <p className="text-ink-soft">Wszystkie terminy są oznaczone jako zrobione.</p>}
            {done.length > 0 ? (
              <div className="space-y-3">
                <h3 className="text-lg font-semibold text-ink">Zrobione ({done.length})</h3>
                {renderList(done, true)}
              </div>
            ) : null}
          </div>
        )}
      </Section>
    </div>
  )
}
