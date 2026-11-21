import type { FocusEventHandler, ChangeEventHandler, ReactNode, Ref } from 'react'
import { cn } from './cn'
import { useFieldControl } from './field-context'
import { CheckIcon } from './icons'

export interface ChoiceOption {
  value: string
  label: ReactNode
  description?: ReactNode
  disabled?: boolean
}

export interface RadioGroupProps {
  name: string
  options: ChoiceOption[]
  value?: string | null
  defaultValue?: string
  onChange?: ChangeEventHandler<HTMLInputElement>
  onBlur?: FocusEventHandler<HTMLInputElement>
  ref?: Ref<HTMLInputElement>
  disabled?: boolean
  columns?: 1 | 2
  className?: string
}

export function RadioGroup({ name, options, value, defaultValue, onChange, onBlur, ref, disabled, columns = 1, className }: RadioGroupProps) {
  const field = useFieldControl()
  const controlled = value !== undefined

  return (
    <div className={cn('grid gap-3', columns === 2 && 'sm:grid-cols-2', className)}>
      {options.map((option) => (
        <label
          key={option.value}
          className={cn(
            'flex min-h-14 cursor-pointer items-start gap-3 rounded-card border-2 border-line bg-surface p-4 transition-colors hover:border-brand',
            'has-[:checked]:border-brand has-[:checked]:bg-brand-soft has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand',
            (disabled || option.disabled) && 'cursor-not-allowed opacity-60 hover:border-line',
            field['aria-invalid'] && 'border-bad',
          )}
        >
          <input
            ref={ref}
            type="radio"
            name={name}
            value={option.value}
            checked={controlled ? value === option.value : undefined}
            defaultChecked={controlled ? undefined : defaultValue === option.value}
            onChange={onChange}
            onBlur={onBlur}
            disabled={disabled || option.disabled}
            aria-invalid={field['aria-invalid']}
            className="peer sr-only"
          />
          <span
            aria-hidden="true"
            className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-full border-2 border-line-strong bg-surface text-on-brand peer-checked:border-brand peer-checked:bg-brand [&>svg]:opacity-0 peer-checked:[&>svg]:opacity-100"
          >
            <CheckIcon className="size-4" />
          </span>
          <span className="min-w-0">
            <span className="block text-base font-semibold text-ink">{option.label}</span>
            {option.description ? <span className="mt-0.5 block text-sm text-ink-soft">{option.description}</span> : null}
          </span>
        </label>
      ))}
    </div>
  )
}
