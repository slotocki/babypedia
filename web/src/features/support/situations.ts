import type { FactSet, SupportSituation } from '@/app/api/types'
import { situations } from '@/app/situations'

export { isSituationId, situationLabel, situations } from '@/app/situations'

export type SituationId = SupportSituation

export const ALL = 'wszystkie'
export type Filter = SituationId | typeof ALL

export const isDifficultSituation = (id: Filter): boolean => situations.some((s) => s.id === id && s.difficult)

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
