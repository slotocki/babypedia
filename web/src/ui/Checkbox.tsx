import { useId, type ComponentProps, type ReactNode } from 'react'
import { cn } from './cn'
import { useFieldControl } from './field-context'
import { CheckIcon } from './icons'

export type CheckboxProps = Omit<ComponentProps<'input'>, 'type'> & {
  label: ReactNode
  description?: ReactNode
}

export function Checkbox({ label, description, className, ...props }: CheckboxProps) {
  const field = useFieldControl()
  const descriptionId = useId()
  const describedBy = [props['aria-describedby'] ?? field['aria-describedby'], description ? descriptionId : null].filter(Boolean).join(' ')

  return (
    <label
      className={cn(
        'flex min-h-11 cursor-pointer items-start gap-3 py-2',
        'has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand has-[:focus-visible]:rounded-control',
        props.disabled && 'cursor-not-allowed opacity-60',
        className,
      )}
    >
      <input
        type="checkbox"
        {...props}
        id={props.id ?? field.id}
        aria-describedby={describedBy || undefined}
        aria-invalid={props['aria-invalid'] ?? field['aria-invalid']}
        className="peer sr-only"
      />
      <span
        aria-hidden="true"
        className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-md border-2 border-line-strong bg-surface text-on-brand peer-checked:border-brand peer-checked:bg-brand peer-aria-invalid:border-bad [&>svg]:opacity-0 peer-checked:[&>svg]:opacity-100"
      >
        <CheckIcon className="size-4" strokeWidth={3} />
      </span>
      <span className="min-w-0">
        <span className="block text-base text-ink">{label}</span>
        {description ? (
          <span id={descriptionId} className="block text-sm text-ink-soft">
            {description}
          </span>
        ) : null}
      </span>
    </label>
  )
}
