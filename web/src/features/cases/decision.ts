import type { CaseAgreement, CaseDecision } from '@/app/api/types'

export const decisionLabel: Record<CaseDecision, string> = {
  odmowa: 'odmowa',
  przyznanie: 'przyznanie',
  do_odczytania: 'do odczytania z dokumentów',
}

/** Co w danej sprawie testowej porównano z dokumentami ZUS. */
export const agreementLabel: Record<CaseAgreement, string> = {
  kwota: 'Okres i kwota zgodne z wydrukiem ZUS',
  okres: 'Okres zgodny z ZUS co do dnia',
  decyzja: 'Odmowa zgodna z decyzją ZUS',
}

export function normalize(text: string) {
  return text.toLocaleLowerCase('pl').normalize('NFD').replace(/\p{M}/gu, '').replace(/ł/g, 'l')
}
