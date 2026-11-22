/**
 * Czyste funkcje pola daty (DateInput): tekst wpisany po polsku ↔ RRRR-MM-DD.
 * Osobny moduł, żeby dało się je sprawdzić testami bez Reacta.
 */
const pad = (value: number) => String(value).padStart(2, '0')

/** Miesiąc liczony od zera (jak w Date). */
export const isoDate = (year: number, month: number, day: number) => `${year}-${pad(month + 1)}-${pad(day)}`

function validIso(year: number, month: number, day: number): string | null {
  if (year < 1900 || year > 2200 || month < 1 || month > 12 || day < 1) return null
  const date = new Date(Date.UTC(year, month - 1, day))
  return date.getUTCMonth() === month - 1 && date.getUTCDate() === day ? isoDate(year, month - 1, day) : null
}

/** Tekst wpisany przez użytkownika → RRRR-MM-DD albo null, gdy to nie jest pełna, istniejąca data. */
export function parseDate(text: string): string | null {
  const t = text.trim().replace(/\s*([.\-/])\s*/g, '$1')
  let m = /^(\d{1,2})[.\-/ ](\d{1,2})[.\-/ ](\d{4})$/.exec(t)
  if (m) return validIso(Number(m[3]), Number(m[2]), Number(m[1]))
  m = /^(\d{4})-(\d{1,2})-(\d{1,2})$/.exec(t)
  if (m) return validIso(Number(m[1]), Number(m[2]), Number(m[3]))
  const digits = t.replace(/\./g, '')
  if (/^[\d.]+$/.test(t) && /^\d{8}$/.test(digits)) {
    return (
      validIso(Number(digits.slice(4)), Number(digits.slice(2, 4)), Number(digits.slice(0, 2))) ??
      validIso(Number(digits.slice(0, 4)), Number(digits.slice(4, 6)), Number(digits.slice(6)))
    )
  }
  return null
}

const isIso = (value: string) => /^\d{4}-\d{2}-\d{2}$/.test(value) && parseDate(value) === value

/** RRRR-MM-DD → DD.MM.RRRR; inne wartości bez zmian. */
export const displayDate = (value: string) => (isIso(value) ? value.split('-').reverse().join('.') : value)

/** Same cyfry → DD.MM.RRRR w trakcie pisania (np. „1503” → „15.03”). */
export function maskDate(digits: string): string {
  const d = digits.slice(0, 8)
  if (d.length <= 2) return d
  if (d.length <= 4) return `${d.slice(0, 2)}.${d.slice(2)}`
  return `${d.slice(0, 2)}.${d.slice(2, 4)}.${d.slice(4)}`
}

/** Co przekazujemy na zewnątrz dla danego tekstu: datę ISO albo (po opuszczeniu pola) tekst do walidacji. */
export const outwardDate = (text: string) => parseDate(text) ?? text.trim()
