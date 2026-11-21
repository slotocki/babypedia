import type { ReactNode } from 'react'
import { cn } from './cn'
import { InfoIcon } from './icons'

export interface EmptyStateProps {
  title: ReactNode
  description?: ReactNode
  action?: ReactNode
  className?: string
}

export function EmptyState({ title, description, action, className }: EmptyStateProps) {
  return (
    <div className={cn('flex flex-col items-center gap-3 rounded-card border-2 border-dashed border-line px-4 py-8 text-center', className)}>
      <InfoIcon className="size-8 text-ink-muted" />
      <p className="text-lg font-semibold text-ink">{title}</p>
      {description ? <p className="max-w-prose text-ink-soft">{description}</p> : null}
      {action}
    </div>
  )
}
