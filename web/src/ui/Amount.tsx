import { cn } from './cn'
import { formatMoney } from './format'

export interface AmountProps {
  value: number
  suffix?: string
  showNote?: boolean
  size?: 'md' | 'lg'
  className?: string
}

export function Amount({ value, suffix, showNote = true, size = 'md', className }: AmountProps) {
  return (
    <span className={cn('inline-flex flex-col', className)}>
      <span className={cn('font-bold tabular-nums text-ink', size === 'lg' ? 'text-3xl' : 'text-xl')}>
        {formatMoney(value)}
        {suffix ? <span className="text-base font-medium text-ink-soft"> {suffix}</span> : null}
      </span>
      {showNote ? <span className="text-sm text-ink-soft">brutto, przed zaliczką na podatek</span> : null}
    </span>
  )
}
