import type { CaseDecision } from '@/app/api/types'

export const decisionLabel: Record<CaseDecision, string> = {
  odmowa: 'odmowa',
  przyznanie: 'przyznanie',
  do_odczytania: 'do odczytania z dokumentów',
}

export function normalize(text: string) {
  return text.toLocaleLowerCase('pl').normalize('NFD').replace(/\p{M}/gu, '').replace(/ł/g, 'l')
}
