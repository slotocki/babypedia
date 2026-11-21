import type { ReactNode } from 'react'
import { Accordion as AccordionPrimitive } from 'radix-ui'
import { cn } from './cn'
import { ChevronDownIcon } from './icons'

export interface AccordionItem {
  value: string
  title: ReactNode
  content: ReactNode
}

export interface AccordionProps {
  items: AccordionItem[]
  defaultValue?: string
  className?: string
}

export function Accordion({ items, defaultValue, className }: AccordionProps) {
  return (
    <AccordionPrimitive.Root type="single" collapsible defaultValue={defaultValue} className={cn('divide-y divide-line rounded-card border border-line bg-surface', className)}>
      {items.map((item) => (
        <AccordionPrimitive.Item key={item.value} value={item.value}>
          <AccordionPrimitive.Header className="m-0">
            <AccordionPrimitive.Trigger className="group flex min-h-11 w-full items-center justify-between gap-3 px-4 py-3 text-left text-base font-semibold text-ink">
              <span className="min-w-0">{item.title}</span>
              <ChevronDownIcon className="size-6 shrink-0 text-ink-soft transition-transform group-data-[state=open]:rotate-180" />
            </AccordionPrimitive.Trigger>
          </AccordionPrimitive.Header>
          <AccordionPrimitive.Content className="px-4 pb-4 text-ink">{item.content}</AccordionPrimitive.Content>
        </AccordionPrimitive.Item>
      ))}
    </AccordionPrimitive.Root>
  )
}
