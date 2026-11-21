import type { ReactNode } from 'react'
import { describedBy } from './cx'

export interface FieldAria {
  id: string
  'aria-invalid': boolean
  'aria-describedby': string | undefined
}

export function FieldHint({ id, children }: { id: string; children: ReactNode }) {
  return (
    <p id={`${id}-hint`} className="mt-1 text-sm text-slate-700">
      {children}
    </p>
  )
}

export function FieldError({ id, children }: { id: string; children: ReactNode }) {
  return (
    <p id={`${id}-error`} className="mt-1 text-sm font-medium text-red-800">
      <span className="font-semibold">Błąd: </span>
      {children}
    </p>
  )
}

interface FieldProps {
  id: string
  label: ReactNode
  hint?: ReactNode
  error?: string | null
  children: (aria: FieldAria) => ReactNode
}

export function Field({ id, label, hint, error, children }: FieldProps) {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-base font-semibold text-slate-900">
        {label}
      </label>
      {hint ? <FieldHint id={id}>{hint}</FieldHint> : null}
      <div className="mt-2">
        {children({ id, 'aria-invalid': Boolean(error), 'aria-describedby': describedBy(id, hint, error) })}
      </div>
      {error ? <FieldError id={id}>{error}</FieldError> : null}
    </div>
  )
}
