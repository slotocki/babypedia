/**
 * Odpowiedzi z kreatora „Wprowadź dane ręcznie” w trakcie wypełniania. Zapisujemy je w sessionStorage,
 * żeby odświeżenie strony albo „Wstecz” w przeglądarce ich nie gubiły. Po zapisaniu odpowiedzi (koniec kreatora)
 * i przy nowym starcie wpisu ręcznego albo logowaniu szkic jest usuwany — wtedy źródłem są zapisane fakty.
 */
const STORAGE_KEY = 'babyradnik.wpis'

/** `owner` = sesja logowania albo sprawy (null przy wpisie bez logowania) — szkic innej sesji pomijamy. */
export function saveWizardDraft(owner: string | null, values: unknown) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ owner, values }))
  } catch {
    // brak dostępu do sessionStorage — szkic po prostu nie przetrwa odświeżenia
  }
}

export function clearWizardDraft() {
  try {
    sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // brak dostępu do sessionStorage
  }
}

/** Szkic nałożony na wartości odtworzone z faktów (brakujące pola szkicu biorą wartość z faktów). */
export function loadWizardDraft<T extends object>(owner: string | null, fromFacts: T): T {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    const stored = raw ? (JSON.parse(raw) as { owner?: unknown; values?: unknown }) : null
    const draft = stored?.owner === owner ? stored.values : null
    if (!draft || typeof draft !== 'object' || Array.isArray(draft)) return fromFacts
    const merged: Record<string, unknown> = { ...(fromFacts as Record<string, unknown>) }
    for (const [key, value] of Object.entries(draft)) {
      const base = merged[key]
      merged[key] =
        base && typeof base === 'object' && !Array.isArray(base) && value && typeof value === 'object' && !Array.isArray(value)
          ? { ...base, ...value }
          : value ?? base
    }
    return merged as T
  } catch {
    return fromFacts
  }
}
