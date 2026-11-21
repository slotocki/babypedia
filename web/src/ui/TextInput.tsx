import type { ComponentProps, ReactNode } from 'react'
import { cn } from './cn'
import { useFieldControl } from './field-context'
import { controlClass } from './input-styles'

export type TextInputProps = ComponentProps<'input'> & { suffix?: ReactNode }

export function TextInput({ suffix, className, type = 'text', ...props }: TextInputProps) {
  const field = useFieldControl()
  const input = (
    <input
      type={type}
      id={props.id ?? field.id}
      aria-describedby={props['aria-describedby'] ?? field['aria-describedby']}
      aria-invalid={props['aria-invalid'] ?? field['aria-invalid']}
      className={cn(controlClass, suffix ? 'pr-14' : null, className)}
      {...props}
    />
  )

  if (!suffix) return input

  return (
    <div className="relative">
      {input}
      <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-ink-soft">
        {suffix}
      </span>
    </div>
  )
}
