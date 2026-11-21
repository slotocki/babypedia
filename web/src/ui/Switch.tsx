import { useId, type ComponentProps, type ReactNode } from 'react'
import { cn } from './cn'
import { CheckIcon } from './icons'

export type SwitchProps = Omit<ComponentProps<'input'>, 'type' | 'role'> & {
  label: ReactNode
  description?: ReactNode
}

export function Switch({ label, description, className, ...props }: SwitchProps) {
  const descriptionId = useId()

  return (
    <label
      className={cn(
        'flex min-h-11 cursor-pointer items-center justify-between gap-4 py-2',
        'has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand has-[:focus-visible]:rounded-control',
        props.disabled && 'cursor-not-allowed opacity-60',
        className,
      )}
    >
      <span className="min-w-0">
        <span className="block text-base text-ink">{label}</span>
        {description ? (
          <span id={descriptionId} className="block text-sm text-ink-soft">
            {description}
          </span>
        ) : null}
      </span>
      <input
        type="checkbox"
        role="switch"
        {...props}
        aria-describedby={description ? descriptionId : props['aria-describedby']}
        className="peer sr-only"
      />
      <span
        aria-hidden="true"
        className="relative h-8 w-14 shrink-0 rounded-pill border-2 border-line-strong bg-surface transition-colors peer-checked:border-brand peer-checked:bg-brand peer-checked:[&>span]:translate-x-6 peer-checked:[&>span]:bg-surface peer-checked:[&>span]:text-brand peer-checked:[&>span>svg]:opacity-100"
      >
        <span className="absolute left-0.5 top-0.5 grid size-6 place-items-center rounded-full bg-line-strong text-on-brand transition-transform [&>svg]:opacity-0">
          <CheckIcon className="size-4" strokeWidth={3} />
        </span>
      </span>
    </label>
  )
}
