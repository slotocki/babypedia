import { useId, type ReactNode } from 'react'
import { cn } from './cn'

export interface SectionProps {
  title: ReactNode
  description?: ReactNode
  level?: 2 | 3
  actions?: ReactNode
  className?: string
  children?: ReactNode
}

export function Section({ title, description, level = 2, actions, className, children }: SectionProps) {
  const titleId = useId()
  const Heading = level === 2 ? 'h2' : 'h3'
  return (
    <section aria-labelledby={titleId} className={cn('space-y-4', className)}>
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
        <div className="min-w-0 space-y-1">
          <Heading id={titleId} className={cn('font-semibold text-ink', level === 2 ? 'text-xl' : 'text-lg')}>
            {title}
          </Heading>
          {description ? <p className="text-ink-soft">{description}</p> : null}
        </div>
        {actions}
      </div>
      {children}
    </section>
  )
}
