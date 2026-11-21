import type { Period as PeriodData } from '@/app/api/types'
import { cn } from './cn'
import { formatDate, formatDays, formatWeeks } from './format'

export function Period({ period, className }: { period: PeriodData; className?: string }) {
  return (
    <span className={cn('inline-flex flex-col', className)}>
      <span className="text-base font-semibold text-ink">
        <time dateTime={period.from}>{formatDate(period.from)}</time>
        {' – '}
        <time dateTime={period.to}>{formatDate(period.to)}</time>
      </span>
      <span className="text-sm text-ink-soft">
        {formatWeeks(period.weeks)} ({formatDays(period.days)})
      </span>
    </span>
  )
}
