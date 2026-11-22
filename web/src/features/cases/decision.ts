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

/** Etykiety z akt są skrótowe („zas. mac. … ciąży_brak ubezpieczenia”) — rozwijamy je do czytelnego zdania. */
export function tidyTitle(title: string): string {
  const text = title
    .replace(/_/g, ' — ')
    .replace(/(?<=^|\s)zas\. mac\./gi, 'zasiłek macierzyński')
    .replace(/(?<=^|\s)zas\. chorobowy/gi, 'zasiłek chorobowy')
    .replace(/(?<=^|\s)zas\./gi, 'zasiłek')
    .replace(/(?<=^|\s)u\. rodz\./gi, 'za urlop rodzicielski')
    .replace(/urlopu mac\./gi, 'urlopu macierzyńskiego')
    .replace(/\s+/g, ' ')
    .trim()
  return text.charAt(0).toLocaleUpperCase('pl') + text.slice(1)
}
