import type { ReactNode } from 'react'
import { Tabs as TabsPrimitive } from 'radix-ui'
import { cn } from './cn'

export interface TabItem {
  value: string
  label: ReactNode
  content: ReactNode
}

export interface TabsProps {
  items: TabItem[]
  label: string
  defaultValue?: string
  value?: string
  onValueChange?: (value: string) => void
  className?: string
}

export function Tabs({ items, label, defaultValue, value, onValueChange, className }: TabsProps) {
  return (
    <TabsPrimitive.Root
      defaultValue={value === undefined ? (defaultValue ?? items[0]?.value) : undefined}
      value={value}
      onValueChange={onValueChange}
      className={cn('space-y-4', className)}
    >
      <TabsPrimitive.List aria-label={label} className="-mx-4 flex gap-1 overflow-x-auto border-b border-line px-4 sm:mx-0 sm:px-0">
        {items.map((item) => (
          <TabsPrimitive.Trigger
            key={item.value}
            value={item.value}
            className="min-h-11 shrink-0 whitespace-nowrap border-b-4 border-transparent px-4 py-2 text-base font-medium text-ink-soft hover:text-ink data-[state=active]:border-brand data-[state=active]:font-semibold data-[state=active]:text-brand-strong"
          >
            {item.label}
          </TabsPrimitive.Trigger>
        ))}
      </TabsPrimitive.List>
      {items.map((item) => (
        <TabsPrimitive.Content key={item.value} value={item.value}>
          {item.content}
        </TabsPrimitive.Content>
      ))}
    </TabsPrimitive.Root>
  )
}
