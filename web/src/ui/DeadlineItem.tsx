import type { Deadline, DeadlineType } from '@/app/api/types'
import { cn } from './cn'
import { formatDate } from './format'
import { CalendarIcon } from './icons'

const typeLabels: Record<DeadlineType, string> = {
  najpóźniej_do: 'Najpóźniej do',
  najpóźniej_przed: 'Najpóźniej przed',
  okno: 'Termin w oknie czasowym',
  przedawnienie: 'Przedawnienie — po tej dacie tracisz roszczenie',
}

export interface DeadlineItemProps {
  deadline: Pick<Deadline, 'type' | 'date' | 'text' | 'consequence'>
  emphasized?: boolean
  className?: string
}

export function DeadlineItem({ deadline, emphasized = false, className }: DeadlineItemProps) {
  return (
    <div
      className={cn(
        'flex items-start gap-3 rounded-card border p-4',
        emphasized ? 'border-2 border-brand bg-brand-soft' : 'border-line bg-surface',
        className,
      )}
    >
      <CalendarIcon className="mt-0.5 size-6 shrink-0 text-brand-strong" />
      <div className="min-w-0 space-y-1">
        <p className="text-sm font-semibold text-ink-soft">
          {emphasized ? 'Najbliższy krok · ' : null}
          {typeLabels[deadline.type]}
        </p>
        <p className="text-lg font-bold text-ink">
          <time dateTime={deadline.date}>{formatDate(deadline.date)}</time>
        </p>
        <p className="text-base text-ink">{deadline.text}</p>
        {deadline.consequence ? (
          <p className="text-sm text-ink-soft">
            <span className="font-semibold">Skutek przekroczenia terminu:</span> {deadline.consequence}
          </p>
        ) : null}
      </div>
    </div>
  )
}
