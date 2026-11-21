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

export function formatDatePl(value: string): string {
  if (!isValidIsoDate(value)) return value
  const [y, m, d] = value.split('-')
  return `${d}.${m}.${y}`
}
