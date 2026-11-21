import type { ReactNode } from 'react'
import { cn } from './cn'

export interface PageHeaderProps {
  title: ReactNode
  description?: ReactNode
  eyebrow?: ReactNode
  action?: ReactNode
  className?: string
}

export function PageHeader({ title, description, eyebrow, action, className }: PageHeaderProps) {
  return (
    <header className={cn('space-y-3', className)}>
      {eyebrow ? <p className="text-sm font-semibold text-brand-strong">{eyebrow}</p> : null}
      <h1 className="text-2xl font-bold leading-tight tracking-tight text-ink sm:text-3xl">{title}</h1>
      {description ? <p className="max-w-prose text-lg text-ink-soft">{description}</p> : null}
      {action ? <div className="pt-1">{action}</div> : null}
    </header>
  )
}
