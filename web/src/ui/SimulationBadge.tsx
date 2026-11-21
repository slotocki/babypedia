import { cn } from './cn'
import { FlaskIcon } from './icons'

export function SimulationBadge({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-pill border border-dashed border-warn bg-warn-soft px-2.5 py-0.5 text-xs font-bold uppercase tracking-wide text-warn',
        className,
      )}
    >
      <FlaskIcon className="size-3.5 shrink-0" />
      symulacja
    </span>
  )
}
