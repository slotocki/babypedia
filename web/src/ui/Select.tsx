import type { ComponentProps } from 'react'
import { cn } from './cn'
import { useFieldControl } from './field-context'
import { controlClass } from './input-styles'
import { ChevronDownIcon } from './icons'

export function Select({ className, children, ...props }: ComponentProps<'select'>) {
  const field = useFieldControl()
  return (
    <div className="relative">
      <select
        id={props.id ?? field.id}
        aria-describedby={props['aria-describedby'] ?? field['aria-describedby']}
        aria-invalid={props['aria-invalid'] ?? field['aria-invalid']}
        className={cn(controlClass, 'appearance-none pr-10', className)}
        {...props}
      >
        {children}
      </select>
      <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 size-5 -translate-y-1/2 text-ink-soft" />
    </div>
  )
}
