import type { ParameterValue } from '@/app/api/types'
import { formatDays, formatMoney, formatNumber, formatWeeks } from '@/ui'

const percent = new Intl.NumberFormat('pl-PL', {
  style: 'percent',
  maximumFractionDigits: 2,
})
const dateTime = new Intl.DateTimeFormat('pl-PL', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

const unitLabels: Record<string, string> = {
  ulamek: 'procent',
  zl: 'złote',
  tygodnie: 'tygodnie',
  dni: 'dni',
  miesiace: 'miesiące',
  lata: 'lata',
  liczba: 'liczba',
  ustawienie: 'ustawienie tekstowe',
}

export function describeUnit(unit: string | undefined): string {
  if (!unit) return 'tekst'
  return unitLabels[unit] ?? unit
}

export function formatParameterValue(value: ParameterValue | undefined, unit: string | undefined): string {
  if (value === null || value === undefined) return 'brak wartości'
  if (typeof value === 'string') return value
  switch (unit) {
    case 'ulamek':
      return percent.format(value)
    case 'zl':
      return formatMoney(value)
    case 'tygodnie':
      return formatWeeks(value)
    case 'dni':
      return formatDays(value)
    case 'miesiace':
      return `${formatNumber(value)} mies.`
    case 'lata':
      return `${formatNumber(value)} lat`
    default:
      return formatNumber(value)
  }
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? iso : dateTime.format(date)
}

export function inputSuffix(unit: string | undefined): string | undefined {
  if (unit === 'ulamek') return '%'
  if (unit === 'zl') return 'zł'
  return undefined
}

export function isTextUnit(unit: string | undefined): boolean {
  return unit === 'ustawienie' || unit === undefined
}

export function allowsEmptyValue(unit: string | undefined, current: ParameterValue): boolean {
  return current === null || isTextUnit(unit)
}

type Parsed = { ok: true; value: ParameterValue } | { ok: false; message: string }

export function parseInput(raw: string, unit: string | undefined, empty: boolean): Parsed {
  if (empty) return { ok: true, value: null }
  const text = raw.trim()
  if (!text) return { ok: false, message: 'Podaj wartość.' }
  if (isTextUnit(unit)) {
    if (text.length > 200) return { ok: false, message: 'Tekst może mieć najwyżej 200 znaków.' }
    return { ok: true, value: text }
  }
  const normalized = text.replace(/\s/g, '').replace(',', '.')
  if (unit === 'ulamek') {
    if (!/^\d+(\.\d+)?$/.test(normalized)) return { ok: false, message: 'Podaj procent, np. 81,5.' }
    const percentValue = Number(normalized)
    if (percentValue > 100)
      return {
        ok: false,
        message: 'Procent musi mieścić się w przedziale 0–100.',
      }
    return { ok: true, value: Number((percentValue / 100).toFixed(6)) }
  }
  if (unit === 'zl') {
    if (!/^\d+(\.\d{1,2})?$/.test(normalized)) return { ok: false, message: 'Podaj kwotę w złotych, np. 4806,00.' }
    return { ok: true, value: Number(normalized) }
  }
  if (!/^\d+$/.test(normalized)) return { ok: false, message: 'Podaj liczbę całkowitą nieujemną.' }
  return { ok: true, value: Number(normalized) }
}
