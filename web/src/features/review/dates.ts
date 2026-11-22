const ISO = /^\d{4}-\d{2}-\d{2}$/

const pad = (n: number) => String(n).padStart(2, '0')

export function isValidIsoDate(value: string): boolean {
  if (!ISO.test(value)) return false
  const [y, m, d] = value.split('-').map(Number)
  const date = new Date(Date.UTC(y, m - 1, d))
  return y >= 1900 && date.getUTCFullYear() === y && date.getUTCMonth() === m - 1 && date.getUTCDate() === d
}

export function todayIso(): string {
  const now = new Date()
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

export function addDaysIso(value: string, days: number): string {
  const [y, m, d] = value.split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, d + days)).toISOString().slice(0, 10)
}

/** Jak daleko wstecz przyjmujemy termin porodu przy „Jestem w ciąży” (sprawy z poprzednich lat). */
export const PAST_PREGNANCY_YEARS = 5
/** Urodzenie dziecka sprzed tylu lat to prawie na pewno pomyłka (urlop rodzicielski kończy się z rokiem 6. urodzin). */
const PAST_BIRTH_YEARS = 7

/**
 * Sprawdza datę zdarzenia tak samo w kreatorze i w pytaniach uzupełniających.
 * Zwraca komunikat dla użytkownika albo null, gdy data jest wiarygodna.
 */
export function eventDateIssue(rodzaj: string | undefined, value: string, today = todayIso()): string | null {
  switch (rodzaj) {
    case 'planuje':
      return value < today ? 'Ta data już minęła. Podaj datę z przyszłości albo zostaw pole puste.' : null
    case 'ciaza':
      // Termin z przeszłości jest dozwolony (sprawy z ubiegłych lat): silnik liczy okresy od tej daty.
      if (value < addDaysIso(today, -365 * PAST_PREGNANCY_YEARS))
        return `Ta data jest sprzed ponad ${PAST_PREGNANCY_YEARS} lat. Jeśli dziecko już się urodziło, wybierz „Dziecko się urodziło”.`
      return value > addDaysIso(today, 330) ? 'Ta data jest zbyt odległa jak na termin porodu. Sprawdź ją w karcie ciąży.' : null
    case 'urodzenie':
      if (value > today) return 'Data urodzenia nie może być z przyszłości. Jeśli dziecko jeszcze się nie urodziło, wybierz „Jestem w ciąży”.'
      return value < addDaysIso(today, -365 * PAST_BIRTH_YEARS) ? `Ta data jest sprzed ponad ${PAST_BIRTH_YEARS} lat. Sprawdź ją jeszcze raz.` : null
    case 'przyjecie':
      return value > addDaysIso(today, 365) ? 'Ta data jest zbyt odległa. Sprawdź ją jeszcze raz.' : null
    default:
      return null
  }
}
