import { z } from 'zod'
import { isValidIsoDate } from './dates'
import type { FactKind, FactMeta } from './fact-labels'

export interface RangeRow {
  od: string
  do: string
}

export interface Draft {
  text: string
  od: string
  to: string
  rows: RangeRow[]
}

export const emptyDraft = (): Draft => ({ text: '', od: '', to: '', rows: [] })

export type ParseResult = { ok: true; value: unknown } | { ok: false; error: string }

export function draftFromValue(kind: FactKind, value: unknown): Draft {
  const draft = emptyDraft()
  if (value === undefined || value === null) return draft
  switch (kind) {
    case 'boolean':
      draft.text = value ? 'tak' : 'nie'
      break
    case 'dateRanges':
      draft.rows = (value as RangeRow[]).map((r) => ({ od: r.od, do: r.do }))
      break
    case 'sytuacjaMatki': {
      const s = value as { rodzaj: string; od: string; do?: string | null }
      draft.text = s.rodzaj
      draft.od = s.od
      draft.to = s.do ?? ''
      break
    }
    case 'readonly':
      break
    default:
      draft.text = String(value)
  }
  return draft
}

export function isDraftEmpty(kind: FactKind, draft: Draft): boolean {
  if (kind === 'dateRanges') return draft.rows.every((r) => !r.od && !r.do)
  if (kind === 'sytuacjaMatki') return !draft.text && !draft.od && !draft.to
  return draft.text.trim() === ''
}

const dateSchema = z
  .string()
  .refine(isValidIsoDate, 'Wybierz datę z kalendarza albo wpisz ją w formacie rrrr-mm-dd, np. 2027-03-15.')

const normalizeNumber = (text: string) => text.replace(/[\s]/g, '').replace(',', '.')

function numberSchema(kind: 'integer' | 'number' | 'money', meta: FactMeta) {
  const min = meta.min ?? 0
  const max = meta.max ?? 1_000_000_000
  const example = kind === 'money' ? 'np. 6500 albo 6500,50' : 'np. 2'
  const base = z
    .string()
    .transform(normalizeNumber)
    .refine((s) => /^\d+(\.\d+)?$/.test(s), `Wpisz liczbę${kind === 'money' ? ' (kwota w złotych)' : ''}, ${example}.`)
    .transform(Number)
  return base
    .refine((n) => kind !== 'integer' || Number.isInteger(n), 'Wpisz liczbę całkowitą, np. 2.')
    .refine((n) => n >= min && n <= max, `Podaj wartość od ${min} do ${max}.`)
}

export function parseDraft(kind: FactKind, draft: Draft, meta: FactMeta, optionValues: string[] = []): ParseResult {
  const fail = (error: string): ParseResult => ({ ok: false, error })
  const first = (r: z.SafeParseReturnType<unknown, unknown>): ParseResult =>
    r.success ? { ok: true, value: r.data } : fail(r.error.issues[0]?.message ?? 'Sprawdź tę odpowiedź.')

  switch (kind) {
    case 'date':
      return first(dateSchema.safeParse(draft.text))
    case 'integer':
    case 'number':
    case 'money':
      return first(numberSchema(kind, meta).safeParse(draft.text))
    case 'boolean':
      if (draft.text !== 'tak' && draft.text !== 'nie') return fail('Wybierz „Tak” albo „Nie”.')
      return { ok: true, value: draft.text === 'tak' }
    case 'enum':
      if (!optionValues.includes(draft.text)) return fail('Wybierz jedną z odpowiedzi.')
      return { ok: true, value: draft.text }
    case 'dateRanges': {
      const rows = draft.rows.filter((r) => r.od || r.do)
      for (const r of rows) {
        if (!isValidIsoDate(r.od) || !isValidIsoDate(r.do)) return fail('W każdym pobycie podaj obie daty: „od” i „do”.')
        if (r.do < r.od) return fail('Data „do” nie może być wcześniejsza niż data „od”.')
      }
      return { ok: true, value: rows }
    }
    case 'sytuacjaMatki': {
      if (!Object.keys(meta.options ?? {}).includes(draft.text)) return fail('Wybierz, która sytuacja dotyczy matki.')
      if (!isValidIsoDate(draft.od)) return fail('Podaj, od kiedy trwa ta sytuacja (data „od”).')
      if (draft.to && (!isValidIsoDate(draft.to) || draft.to < draft.od)) return fail('Data „do” musi być taka sama jak „od” albo późniejsza.')
      return { ok: true, value: { rodzaj: draft.text, od: draft.od, ...(draft.to ? { do: draft.to } : {}) } }
    }
    case 'readonly':
      return fail('Tej wartości nie można tu zmienić.')
  }
}
