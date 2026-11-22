import { Card, Checkbox, DeadlineItem, Disclosure, EmptyState, Section, WarningIcon, formatDate } from '@/ui'
import { AVERAGED_DEADLINE, benefitList, mergeDeadlines, type DeadlineEntry, type MergedDeadline } from './insights'

export type { DeadlineEntry } from './insights'

export interface DeadlinesSectionProps {
  entries: DeadlineEntry[]
  completed: string[]
  next?: { deadlineId: string; benefitId: string; person?: string | null } | null
  showPerson: boolean
  today: string
  onToggle: (deadlineId: string, done: boolean) => void
  /** Użytkownik wybrał wariant 81,5% — wniosek o niego nie jest wtedy „do wyboru”. */
  averagedChosen?: boolean
}

function Entry({
  item,
  done,
  isNext,
  showPerson,
  today,
  onToggle,
  averagedChosen,
}: {
  item: MergedDeadline
  done: boolean
  isNext: boolean
  showPerson: boolean
  today: string
  onToggle: DeadlinesSectionProps['onToggle']
  averagedChosen?: boolean
}) {
  const { deadline } = item.entry
  const past = deadline.date < today
  const optional = deadline.id === AVERAGED_DEADLINE && !averagedChosen
  return (
    <li className="space-y-2 p-4">
      <DeadlineItem deadline={deadline} className="border-0 bg-transparent p-0" />
      <div className="space-y-1 pl-9 text-sm text-ink-soft">
        {optional && !past ? (
          <p className="font-semibold text-ink">To Twój wybór, nie obowiązek: wniosek składasz tylko wtedy, gdy chcesz wariant 81,5%.</p>
        ) : null}
        <p className="break-words">
          Dotyczy: <span className="font-semibold text-ink">{benefitList(item.benefits, showPerson)}</span>
        </p>
        {deadline.basis ? <p className="break-words">Przepis: {deadline.basis}</p> : null}
        {isNext && !done ? <p className="font-semibold text-brand-strong">To jest Twój najbliższy krok.</p> : null}
        {past && !done && optional ? (
          <p className="font-semibold text-ink">
            Wariantu 81,5% nie da się już wybrać — zasiłek będzie wypłacany w wariancie standardowym. W tej sprawie nic więcej nie musisz robić.
          </p>
        ) : past && !done ? (
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
            Zrobione<span className="sr-only">: termin {formatDate(deadline.date)}</span>
          </>
        }
      />
    </li>
  )
}

export function DeadlinesSection({ entries, completed, next, showPerson, today, onToggle, averagedChosen = false }: DeadlinesSectionProps) {
  const merged = mergeDeadlines(entries)
  // Minięty termin wyboru wariantu 81,5% nie jest alarmem — przenosimy go na koniec listy.
  const late = (m: MergedDeadline) => (!averagedChosen && m.entry.deadline.id === AVERAGED_DEADLINE && m.entry.deadline.date < today ? 1 : 0)
  const sorted = [...merged].sort((a, b) => late(a) - late(b) || a.entry.deadline.date.localeCompare(b.entry.deadline.date))
  const open = sorted.filter((m) => !completed.includes(m.entry.deadline.id))
  const done = sorted.filter((m) => completed.includes(m.entry.deadline.id))
  const isNext = (m: MergedDeadline) =>
    !!next &&
    m.entry.deadline.id === next.deadlineId &&
    m.benefits.some((b) => b.id === next.benefitId && (!next.person || b.person === next.person))

  const renderList = (list: MergedDeadline[], isDone: boolean) => (
    <ul className="divide-y divide-line">
      {list.map((item) => (
        <Entry key={item.key} item={item} done={isDone} isNext={isNext(item)} showPerson={showPerson} today={today} onToggle={onToggle} averagedChosen={averagedChosen} />
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
