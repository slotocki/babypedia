import type { ReactNode } from 'react'
import { Button } from './Button'
import { describeError } from './describe-error'

type Tone = 'info' | 'error' | 'warning'

const tones: Record<Tone, string> = {
  info: 'border-teal-700 bg-teal-50 text-slate-900',
  warning: 'border-amber-700 bg-amber-50 text-slate-900',
  error: 'border-red-700 bg-red-50 text-slate-900',
}

const prefixes: Record<Tone, string> = { info: 'Informacja', warning: 'Uwaga', error: 'Błąd' }

export function Alert({ tone = 'info', title, children }: { tone?: Tone; title?: string; children: ReactNode }) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`rounded-lg border-l-4 p-4 ${tones[tone]}`}>
      <p className="font-semibold">
        <span className="sr-only">{prefixes[tone]}: </span>
        {title ?? prefixes[tone]}
      </p>
      <div className="mt-1 text-base">{children}</div>
    </div>
  )
}

export function ErrorState({ error, onRetry, title = 'Coś poszło nie tak' }: { error: unknown; onRetry: () => void; title?: string }) {
  return (
    <Alert tone="error" title={title}>
      <p>{describeError(error)}</p>
      <p className="mt-1">Możesz spróbować jeszcze raz. Twoje odpowiedzi nie zostały utracone.</p>
      <Button variant="secondary" className="mt-3" onClick={onRetry}>
        Spróbuj ponownie
      </Button>
    </Alert>
  )
}

export function Spinner({ label }: { label: string }) {
  return (
    <div role="status" className="flex items-center gap-3 py-4 text-slate-800">
      <span aria-hidden="true" className="size-5 animate-spin rounded-full border-2 border-teal-800 border-t-transparent" />
      <span>{label}</span>
    </div>
  )
}
