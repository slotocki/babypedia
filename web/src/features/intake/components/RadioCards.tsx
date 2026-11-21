import type { KeyboardEvent, ReactNode, Ref } from 'react'
import { cx, describedBy } from './cx'
import { FieldError, FieldHint } from './Field'

export interface RadioOption {
  value: string
  label: string
  description?: string
}

interface RadioCardsProps {
  id: string
  legend: ReactNode
  hint?: ReactNode
  error?: string | null
  options: RadioOption[]
  value: string
  onChange: (value: string) => void
  onBlur?: () => void
  firstRef?: Ref<HTMLInputElement>
  columns?: 1 | 2
}

export function RadioCards({ id, legend, hint, error, options, value, onChange, onBlur, firstRef, columns = 1 }: RadioCardsProps) {
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>, optionValue: string) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      onChange(optionValue)
    }
  }

  return (
    <fieldset aria-describedby={describedBy(id, hint, error)}>
      <legend className="mb-1 text-base font-semibold text-slate-900">{legend}</legend>
      {hint ? <FieldHint id={id}>{hint}</FieldHint> : null}
      <div className={cx('mt-2 grid gap-2', columns === 2 && 'sm:grid-cols-2')}>
        {options.map((option, index) => {
          const inputId = `${id}-${option.value}`
          return (
            <label
              key={option.value}
              htmlFor={inputId}
              className="flex min-h-11 cursor-pointer items-start gap-3 rounded-lg border border-slate-400 bg-white p-3 hover:bg-slate-50 has-[:checked]:border-2 has-[:checked]:border-teal-800 has-[:checked]:bg-teal-50 has-[:focus-visible]:ring-4 has-[:focus-visible]:ring-teal-600/50"
            >
              <input
                id={inputId}
                ref={index === 0 ? firstRef : undefined}
                type="radio"
                name={id}
                value={option.value}
                checked={value === option.value}
                aria-invalid={Boolean(error)}
                onChange={() => onChange(option.value)}
                onKeyDown={(event) => onKeyDown(event, option.value)}
                onBlur={onBlur}
                className="mt-0.5 size-5 shrink-0 accent-teal-800"
              />
              <span>
                <span className="block text-base font-medium text-slate-900">{option.label}</span>
                {option.description ? <span className="block text-sm text-slate-700">{option.description}</span> : null}
              </span>
            </label>
          )
        })}
      </div>
      {error ? <FieldError id={id}>{error}</FieldError> : null}
    </fieldset>
  )
}
