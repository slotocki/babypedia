import type { FactSet } from '@/app/api/types'
import type { AnyFact, Bucket } from './facts'

/**
 * Odpowiedzi zapisane w pliku osoby testowej (źródło „user” w danych z symulowanego logowania).
 * Pamiętamy je osobno, żeby na „Sprawdź dane” oznaczyć je jako przykład, dopóki użytkownik ich nie zmieni.
 */
const STORAGE_KEY = 'babyradnik.przyklady'

interface Stored {
  sessionId: string
  values: Record<string, string>
}

const buckets: Bucket[] = ['sprawa', 'wnioskodawca', 'drugiRodzic']

export function rememberExampleAnswers(sessionId: string, facts: FactSet) {
  const values: Record<string, string> = {}
  for (const bucket of buckets) {
    for (const [key, fact] of Object.entries((facts[bucket] ?? {}) as Record<string, AnyFact | undefined>)) {
      if (fact?.source === 'user') values[`${bucket}:${key}`] = JSON.stringify(fact.value)
    }
  }
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ sessionId, values } satisfies Stored))
  } catch {
    // brak dostępu do sessionStorage — oznaczenia „przykład” po prostu nie będzie
  }
}

function load(): Stored | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Stored) : null
  } catch {
    return null
  }
}

/** Czy fakt to nadal niezmieniona przykładowa odpowiedź osoby testowej z bieżącej sesji. */
export function isExampleAnswer(sessionId: string | null, bucket: Bucket, key: string, fact: AnyFact | undefined): boolean {
  if (!sessionId || fact?.source !== 'user') return false
  const stored = load()
  return stored?.sessionId === sessionId && stored.values[`${bucket}:${key}`] === JSON.stringify(fact.value)
}
