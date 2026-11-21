import type { IsoDate } from '@/app/api/types'

const dateLong = new Intl.DateTimeFormat('pl-PL', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' })
const dateShort = new Intl.DateTimeFormat('pl-PL', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })
const money = new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN', useGrouping: 'always' })
const percent = new Intl.NumberFormat('pl-PL', { style: 'percent', maximumFractionDigits: 1 })
const number = new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 2, useGrouping: 'always' })

function toUtc(iso: IsoDate): Date {
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, d))
}

export function formatDate(iso: IsoDate): string {
  return dateLong.format(toUtc(iso))
}

export function formatDateShort(iso: IsoDate): string {
  return dateShort.format(toUtc(iso))
}

export function formatMoney(value: number): string {
  return money.format(value)
}

export function formatNumber(value: number): string {
  return number.format(value)
}

export function formatPercent(fraction: number): string {
  return percent.format(fraction)
}

export function pluralize(count: number, one: string, few: string, many: string): string {
  const abs = Math.abs(count)
  if (abs === 1) return one
  const lastTwo = abs % 100
  const last = abs % 10
  if (last >= 2 && last <= 4 && !(lastTwo >= 12 && lastTwo <= 14)) return few
  return many
}

export function formatDays(days: number): string {
  return `${formatNumber(days)} ${pluralize(days, 'dzień', 'dni', 'dni')}`
}

export function formatWeeks(weeks: number): string {
  return `${formatNumber(weeks)} ${pluralize(weeks, 'tydzień', 'tygodnie', 'tygodni')}`
}

export function parseMoney(text: string): number | null {
  const cleaned = text.replace(/\s/g, '').replace(/zł$/i, '').replace(',', '.')
  if (!/^\d+(\.\d{1,2})?$/.test(cleaned)) return null
  return Number(cleaned)
}
