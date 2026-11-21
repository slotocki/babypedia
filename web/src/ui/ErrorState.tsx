import type { ReactNode } from 'react'
import type { ProblemDetails } from '@/app/api/types'
import { Alert } from './Alert'
import { Button } from './Button'

export interface ErrorStateProps {
  error?: unknown
  title?: ReactNode
  description?: ReactNode
  onRetry?: () => void
  retrying?: boolean
  className?: string
}

function toProblem(error: unknown): ProblemDetails | null {
  if (typeof error !== 'object' || error === null) return null
  if ('problem' in error) return (error as { problem: ProblemDetails }).problem
  if ('title' in error || 'detail' in error) return error as ProblemDetails
  return null
}

export function ErrorState({ error, title, description, onRetry, retrying = false, className }: ErrorStateProps) {
  const problem = toProblem(error)
  const isNetwork = error instanceof TypeError
  const fieldErrors = problem?.errors ? Object.values(problem.errors).flat() : []

  const heading = title ?? (isNetwork ? 'Nie możemy połączyć się z serwerem' : (problem?.title ?? 'Nie udało się wykonać operacji'))
  const body = description ?? problem?.detail ?? 'Spróbuj ponownie za chwilę. Jeśli błąd się powtarza, wróć do poprzedniego kroku.'

  return (
    <Alert variant="error" title={heading} className={className}>
      <div className="space-y-3">
        <p>{body}</p>
        {fieldErrors.length > 0 ? (
          <ul className="list-disc space-y-1 pl-5">
            {fieldErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        ) : null}
        {problem?.status ? <p className="text-sm text-ink-soft">Kod błędu: {problem.status}</p> : null}
        {onRetry ? (
          <Button variant="secondary" onClick={onRetry} loading={retrying} loadingLabel="Ponawiam próbę…">
            Spróbuj ponownie
          </Button>
        ) : null}
      </div>
    </Alert>
  )
}
