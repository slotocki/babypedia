import { useId, type ReactNode } from 'react'
import { cn } from './cn'
import { FieldContext, type FieldControl } from './field-context'
import { HelpTip } from './HelpTip'
import { ErrorIcon } from './icons'

export type FieldErrorInput = string | { message?: string } | undefined

export interface FieldProps {
  label: ReactNode
  /** Krótka podpowiedź pod etykietą (jedna linia: co wpisać, gdzie to znaleźć). */
  description?: ReactNode
  /** Dłuższe wyjaśnienie („dlaczego pytamy”) schowane pod ikonką „?” przy etykiecie. */
  help?: ReactNode
  error?: FieldErrorInput
  fix?: ReactNode
  optional?: boolean
  group?: boolean
  className?: string
  children: ReactNode
}

export function Field({ label, description, help, error, fix, optional, group = false, className, children }: FieldProps) {
  const uid = useId()
  const descriptionId = `${uid}-description`
  const errorId = `${uid}-error`
  const message = typeof error === 'string' ? error : error?.message
  const hasError = Boolean(message)

  const describedBy = [description ? descriptionId : null, hasError ? errorId : null].filter(Boolean).join(' ') || undefined
  const control: FieldControl = {
    id: group ? undefined : uid,
    'aria-describedby': group ? undefined : describedBy,
    'aria-invalid': hasError ? true : undefined,
  }

  const labelContent = (
    <>
      {label}
      {optional ? <span className="font-normal text-ink-muted"> (opcjonalnie)</span> : null}
    </>
  )
  const helpTip = help ? (
    <HelpTip label={typeof label === 'string' ? `Wyjaśnienie: ${label}` : 'Wyjaśnienie'}>{help}</HelpTip>
  ) : null

  const body = (
    <>
      {description ? (
        <p id={descriptionId} className="text-sm text-ink-soft">
          {description}
        </p>
      ) : null}
      {children}
      {hasError ? (
        <p id={errorId} role="alert" className="flex items-start gap-2 text-sm font-medium text-bad">
          <ErrorIcon className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Błąd: </span>
            {message}
            {fix ? <span className="block font-normal">Co zrobić: {fix}</span> : null}
          </span>
        </p>
      ) : null}
    </>
  )

  return (
    <FieldContext value={control}>
      {group ? (
        <fieldset aria-describedby={describedBy} className={cn('min-w-0 space-y-2', className)}>
          <legend className="mb-2 text-base font-semibold text-ink">
            {helpTip ? (
              <span className="flex items-start gap-1">
                <span className="min-w-0">{labelContent}</span>
                {helpTip}
              </span>
            ) : (
              labelContent
            )}
          </legend>
          <div className="space-y-2">{body}</div>
        </fieldset>
      ) : (
        <div className={cn('min-w-0 space-y-2', className)}>
          {helpTip ? (
            <div className="flex items-start gap-1">
              <label htmlFor={uid} className="min-w-0 text-base font-semibold text-ink">
                {labelContent}
              </label>
              {helpTip}
            </div>
          ) : (
            <label htmlFor={uid} className="block text-base font-semibold text-ink">
              {labelContent}
            </label>
          )}
          {body}
        </div>
      )}
    </FieldContext>
  )
}
