import type { FactSet, ProblemDetails } from '@/app/api/types'
import type { SubmissionTemplate, TemplateField } from './api'
import { isValidIsoDate } from '@/features/review/dates'

export type Values = Record<string, string>

const ELEVEN_DIGITS = /\d{11}/

export function visibleFields(template: SubmissionTemplate, values: Values): TemplateField[] {
  const shown = new Set<string>()
  for (const field of template.fields) {
    const { when } = field
    if (!when) {
      shown.add(field.key)
      continue
    }
    const current = (values[when.key] ?? '').trim()
    const matches = when.value === '*' ? current !== '' : current === when.value
    if (shown.has(when.key) && matches) shown.add(field.key)
  }
  return template.fields.filter((field) => shown.has(field.key))
}

export function prefill(template: SubmissionTemplate, facts: FactSet, profileName?: string | null): Values {
  const kase = facts.sprawa
  const kind = kase['zdarzenie.rodzaj']?.value
  const eventDate = kase['zdarzenie.data']?.value
  const birthDate = kase['dziecko.dataUrodzenia']?.value ?? (kind === 'urodzenie' ? eventDate : undefined)
  const zaZyciem = kase['dziecko.zaZyciem']?.value
  const path = kase['przyjecie.sciezka']?.value

  const candidates: Record<string, string | undefined> = {
    imieNazwisko: profileName ?? undefined,
    dataPorodu: birthDate ?? (kind === 'ciaza' ? eventDate : undefined),
    dzieckoDataUrodzenia: birthDate,
    przewidywanaDataPorodu: kind === 'ciaza' ? eventDate : undefined,
    zaZyciem: zaZyciem === undefined ? undefined : zaZyciem ? 'tak' : 'nie',
    dzieckoPrzyjete: kind === 'przyjecie' ? 'tak' : undefined,
    dataPrzyjeciaDziecka: kind === 'przyjecie' ? eventDate : undefined,
    podstawaWniosku:
      kind === 'urodzenie'
        ? 'urodzenie'
        : kind === 'przyjecie' && path
          ? path === 'przysposobienie'
            ? 'przyjecie_przysposobienie'
            : 'rodzina_zastepcza'
          : undefined,
  }

  const values: Values = {}
  for (const field of template.fields) {
    const value = candidates[field.key]
    if (!value) continue
    if (field.kind === 'choice' && !field.options?.some((option) => option.value === value)) continue
    values[field.key] = value
  }
  return values
}

export function validateLocally(fields: TemplateField[], values: Values): Record<string, string> {
  const errors: Record<string, string> = {}
  for (const field of fields) {
    const value = (values[field.key] ?? '').trim()
    if (!value) {
      if (field.required) errors[field.key] = 'To pole jest wymagane.'
      continue
    }
    if (ELEVEN_DIGITS.test(value.replace(/[\s-]/g, ''))) {
      errors[field.key] = 'Nie wpisuj numeru PESEL ani innych 11-cyfrowych numerów — Kompas Rodzica ich nie przyjmuje.'
    } else if (field.kind === 'date' && (!isValidIsoDate(value) || Number(value.slice(0, 4)) > 2200)) {
      errors[field.key] = 'Wybierz datę z kalendarza albo wpisz ją w formacie RRRR-MM-DD.'
    } else if (field.kind === 'integer' && !/^\d{1,6}$/.test(value)) {
      errors[field.key] = 'Wpisz liczbę całkowitą, np. 1.'
    }
  }
  return errors
}

export function collectFields(fields: TemplateField[], values: Values): Values {
  const result: Values = {}
  for (const field of fields) {
    const value = (values[field.key] ?? '').trim()
    if (value) result[field.key] = value
  }
  return result
}

export function splitProblem(problem: ProblemDetails): { byField: Record<string, string>; general: string[] } {
  const byField: Record<string, string> = {}
  const general: string[] = []
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    const message = messages[0]
    if (!message) continue
    if (key.startsWith('fields.')) byField[key.slice('fields.'.length)] = message
    else general.push(message)
  }
  return { byField, general }
}

export function legalBasis(field: TemplateField): string | null {
  return field.basis && !field.basis.startsWith('pole pomocnicze') ? field.basis : null
}
