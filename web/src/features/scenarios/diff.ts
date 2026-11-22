import type { BenefitDiff, BenefitResult, BenefitStatus, CompareResult, Deadline, PersonRef, Risk } from '@/app/api/types'
import { formatMoney, formatWeeks } from '@/ui/format'
import { benefitNames, personLabel, statusText } from './labels'

export interface BenefitChange {
  key: string
  id: BenefitResult['id']
  person: PersonRef
  base?: BenefitResult
  variant?: BenefitResult
  statusBase: BenefitStatus
  statusVariant: BenefitStatus
  statusChanged: boolean
  weeksDelta: number | null
  amountDelta: number | null
  periodChanged: boolean
}

export interface DeadlineChange {
  id: string
  kind: 'nowy' | 'przesuniety'
  base?: Deadline
  variant: Deadline
}

export interface Analysis {
  changed: BenefitChange[]
  unchanged: BenefitChange[]
  totalAmountDelta: number | null
  amountsUnknown: boolean
  newRisks: Risk[]
  deadlines: DeadlineChange[]
  both: boolean
}

const keyOf = (id: string, person: PersonRef) => `${id}:${person}`

const periodOf = (b?: BenefitResult) => (b?.period ? `${b.period.from}|${b.period.to}` : '')

export function analyze(result: CompareResult): Analysis {
  const base = new Map(result.base.benefits.map((b) => [keyOf(b.id, b.person), b]))
  const variant = new Map(result.variant.benefits.map((b) => [keyOf(b.id, b.person), b]))
  const diffs = new Map<string, BenefitDiff>(result.diff.map((d) => [keyOf(d.id, d.person), d]))
  const keys = new Set([...base.keys(), ...variant.keys(), ...diffs.keys()])

  const all: BenefitChange[] = []
  for (const key of keys) {
    const b = base.get(key)
    const v = variant.get(key)
    const d = diffs.get(key)
    const ref = b ?? v
    const id = d?.id ?? ref?.id
    const person = d?.person ?? ref?.person
    if (!id || !person) continue
    const statusBase = d?.statusBase ?? b?.status ?? 'nie_dotyczy'
    const statusVariant = d?.statusVariant ?? v?.status ?? 'nie_dotyczy'
    all.push({
      key,
      id,
      person,
      base: b,
      variant: v,
      statusBase,
      statusVariant,
      statusChanged: statusBase !== statusVariant,
      weeksDelta: d ? d.weeksDelta : (v?.period?.weeks ?? 0) - (b?.period?.weeks ?? 0),
      amountDelta: d ? d.amountDelta : null,
      periodChanged: periodOf(b) !== periodOf(v),
    })
  }

  const isChanged = (c: BenefitChange) => c.statusChanged || Boolean(c.weeksDelta) || Boolean(c.amountDelta) || c.periodChanged
  const irrelevant = (c: BenefitChange) => c.statusBase === 'nie_dotyczy' && c.statusVariant === 'nie_dotyczy'

  const changed = all.filter(isChanged)
  const unchanged = all.filter((c) => !isChanged(c) && !irrelevant(c))

  const amounts = changed.map((c) => c.amountDelta).filter((n): n is number => n !== null)
  const totalAmountDelta = amounts.length > 0 ? amounts.reduce((a, b) => a + b, 0) : null

  const baseRiskIds = new Set(result.base.risks.map((r) => r.id))
  const newRisks = result.variant.risks.filter((r) => !baseRiskIds.has(r.id))

  const baseDeadlines = new Map<string, Deadline>()
  for (const b of result.base.benefits) for (const d of b.deadlines) baseDeadlines.set(d.id, d)
  const deadlines: DeadlineChange[] = []
  const seen = new Set<string>()
  for (const v of result.variant.benefits) {
    for (const d of v.deadlines) {
      if (seen.has(d.id)) continue
      seen.add(d.id)
      const before = baseDeadlines.get(d.id)
      if (!before) deadlines.push({ id: d.id, kind: 'nowy', variant: d })
      else if (before.date !== d.date) deadlines.push({ id: d.id, kind: 'przesuniety', base: before, variant: d })
    }
  }

  const both = all.some((c) => c.person === 'drugiRodzic') || result.base.benefits.some((b) => b.person === 'drugiRodzic')

  return {
    changed,
    unchanged,
    totalAmountDelta,
    amountsUnknown: changed.some((c) => c.amountDelta === null && (c.base?.amount == null) !== (c.variant?.amount == null)),
    newRisks,
    deadlines,
    both,
  }
}

const MINUS = '−'

export function signedWeeks(delta: number): string {
  if (delta === 0) return '0 tygodni'
  return `${delta > 0 ? '+' : MINUS}${formatWeeks(Math.abs(delta))} (${delta > 0 ? 'więcej' : 'mniej'})`
}

export function signedMoney(delta: number): string {
  if (delta === 0) return formatMoney(0)
  return `${delta > 0 ? '+' : MINUS}${formatMoney(Math.abs(delta))} (${delta > 0 ? 'więcej' : 'mniej'})`
}

export function benefitTitle(c: BenefitChange, both: boolean): string {
  return both ? `${benefitNames[c.id]} (${personLabel(c.person)})` : benefitNames[c.id]
}

export function describeChange(c: BenefitChange, both: boolean): string {
  const parts: string[] = []
  if (c.statusChanged) parts.push(`${statusText[c.statusBase]} → ${statusText[c.statusVariant]}`)
  if (c.weeksDelta) parts.push(signedWeeks(c.weeksDelta))
  if (c.amountDelta) parts.push(signedMoney(c.amountDelta))
  if (parts.length === 0) parts.push('zmienia się okres')
  return `${benefitTitle(c, both)}: ${parts.join(', ')}`
}

const AMOUNT_TOLERANCE = 0.01

/**
 * Jedno zdanie „co to znaczy” nad różnicą. Wyłącznie z danych porównania (statusy, kwoty, okresy); brak podstaw → null.
 */
export function summarize(result: CompareResult, analysis: Analysis, scenarioId?: string): string | null {
  const { changed, totalAmountDelta, both } = analysis
  if (changed.length === 0) return null
  const statusChange = changed.find((c) => c.statusChanged)

  if (totalAmountDelta !== null && !changed.some((c) => c.statusChanged)) {
    const totalBase = result.base.benefits.reduce((sum, b) => sum + (b.amount?.total ?? 0), 0)
    const abs = Math.abs(totalAmountDelta)
    if (totalAmountDelta === 0 && analysis.changed.some((c) => c.periodChanged || c.weeksDelta)) {
      return 'Łączna kwota zostaje taka sama, zmieniają się tylko okresy wypłat.'
    }
    if (totalBase > 0 && abs <= totalBase * AMOUNT_TOLERANCE) {
      const tail = scenarioId === 'wariant_wyplaty' ? '; uśredniony wypłaca równo przez cały czas.' : '; różni się głównie rozłożeniem w czasie.'
      return `Oba warianty dają prawie tyle samo (różnica ${formatMoney(abs)})${tail}`
    }
    return `Ten wariant daje łącznie o ${formatMoney(abs)} ${totalAmountDelta > 0 ? 'więcej' : 'mniej'} niż plan bazowy.`
  }

  if (statusChange) {
    const name = benefitTitle(statusChange, both)
    const became = statusText[statusChange.statusVariant]
    const others = changed.filter((c) => c.statusChanged).length - 1
    return `W tym wariancie zmienia się status: ${name} — ${became}${others > 0 ? ` (i jeszcze ${others} ${others === 1 ? 'świadczenie' : 'świadczenia'})` : ''}.`
  }
  return null
}
