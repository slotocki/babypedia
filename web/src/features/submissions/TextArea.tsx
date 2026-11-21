import type { ComponentProps } from 'react'
import { cn, useFieldControl } from '@/ui'

export function TextArea({ className, ...props }: ComponentProps<'textarea'>) {
  const field = useFieldControl()
  return (
    <textarea
      id={props.id ?? field.id}
      aria-describedby={props['aria-describedby'] ?? field['aria-describedby']}
      aria-invalid={props['aria-invalid'] ?? field['aria-invalid']}
      rows={4}
      className={cn(
        'block min-h-24 w-full rounded-control border border-line-strong bg-surface px-3 py-2 text-base text-ink placeholder:text-ink-muted aria-invalid:border-2 aria-invalid:border-bad',
        className,
      )}
      {...props}
    />
  )
}
