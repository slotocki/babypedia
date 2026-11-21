import type { FactSet } from '@/app/api/types'

export const situations = [
  { id: 'martwe_urodzenie', label: 'Martwe urodzenie', difficult: true },
  { id: 'zgon_dziecka', label: 'Śmierć dziecka', difficult: true },
  { id: 'wczesniak', label: 'Wcześniactwo i pobyt dziecka w szpitalu', difficult: true },
  { id: 'ciezka_choroba_dziecka', label: 'Ciężka choroba lub niepełnosprawność dziecka', difficult: true },
  { id: 'samotny_rodzic', label: 'Samotne rodzicielstwo', difficult: false },
  { id: 'trudna_sytuacja_materialna', label: 'Trudna sytuacja materialna', difficult: false },
  { id: 'przyjecie_dziecka', label: 'Przyjęcie dziecka na wychowanie', difficult: false },
] as const

export type SituationId = (typeof situations)[number]['id']

export const ALL = 'wszystkie'
export type Filter = SituationId | typeof ALL

export const isSituationId = (value: string | null): value is SituationId => situations.some((s) => s.id === value)

export const isDifficultSituation = (id: Filter): boolean => situations.some((s) => s.id === id && s.difficult)

export const situationLabel = (id: string): string => situations.find((s) => s.id === id)?.label ?? id

export function detectSituation(facts: FactSet): SituationId | null {
  const { sprawa } = facts
  if (sprawa['dziecko.martweUrodzenie']?.value === true) return 'martwe_urodzenie'
  if (sprawa['dziecko.dataZgonu'] !== undefined || (sprawa['dziecko.liczbaZmarlych']?.value ?? 0) > 0) return 'zgon_dziecka'
  const stays = sprawa['dziecko.pobytyWSzpitalu']?.value ?? []
  if (stays.length > 0 && (sprawa['dziecko.tydzienCiazyPrzyPorodzie']?.value ?? 40) < 37) return 'wczesniak'
  if (sprawa['dziecko.zaZyciem']?.value === true) return 'ciezka_choroba_dziecka'
  if (sprawa['zdarzenie.rodzaj']?.value === 'przyjecie') return 'przyjecie_dziecka'
  return null
}
