import { ApiError } from '@/app/api/client'

export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.problem.title ?? `Serwer zwrócił błąd ${error.status}.`
  }
  return 'Nie udało się połączyć z serwerem. Sprawdź połączenie z internetem.'
}
