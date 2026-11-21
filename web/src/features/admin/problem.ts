import { ApiError } from '@/app/api/client'

export function errorStatus(error: unknown): number | null {
  return error instanceof ApiError ? error.status : null
}

export function fieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError)) return {}
  const result: Record<string, string> = {}
  for (const [key, messages] of Object.entries(error.problem.errors ?? {})) {
    if (messages[0]) result[key] = messages[0]
  }
  return result
}

export function generalMessage(error: unknown): string | null {
  if (!error) return null
  if (error instanceof ApiError) return error.problem.detail ?? error.problem.title ?? `Błąd ${error.status}`
  return 'Nie można połączyć się z serwerem. Spróbuj ponownie.'
}
