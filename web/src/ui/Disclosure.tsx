import type { ReactNode } from 'react'
import { Collapsible } from 'radix-ui'
import { cn } from './cn'
import { ChevronDownIcon } from './icons'

export interface DisclosureProps {
  title: ReactNode
  summary?: ReactNode
  defaultOpen?: boolean
  open?: boolean
  onOpenChange?: (open: boolean) => void
  className?: string
  children: ReactNode
}

export function Disclosure({ title, summary, defaultOpen, open, onOpenChange, className, children }: DisclosureProps) {
  return (
    <Collapsible.Root defaultOpen={defaultOpen} open={open} onOpenChange={onOpenChange} className={cn('rounded-card border border-line bg-surface', className)}>
      <Collapsible.Trigger className="group flex min-h-11 w-full items-center justify-between gap-3 rounded-card px-4 py-3 text-left">
        <span className="min-w-0">
          <span className="block text-base font-semibold text-ink">{title}</span>
          {summary ? <span className="block text-sm text-ink-soft">{summary}</span> : null}
        </span>
        <ChevronDownIcon className="size-6 shrink-0 text-ink-soft transition-transform group-data-[state=open]:rotate-180" />
      </Collapsible.Trigger>
      <Collapsible.Content className="border-t border-line px-4 py-3">{children}</Collapsible.Content>
    </Collapsible.Root>
  )
}
