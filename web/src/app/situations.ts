import type { SupportSituation } from './api/types'

/**
 * Wspólny słownik sytuacji katalogu wsparcia: ekran „Wsparcie” (filtr) i panel /admin (wybór przy organizacji).
 * Kontrakt: enum `SupportSituation` w contracts/openapi.yaml.
 */
export const situations: readonly { id: SupportSituation; label: string; difficult: boolean }[] = [
  { id: 'martwe_urodzenie', label: 'Martwe urodzenie', difficult: true },
  { id: 'zgon_dziecka', label: 'Śmierć dziecka', difficult: true },
  { id: 'wczesniak', label: 'Wcześniactwo i pobyt dziecka w szpitalu', difficult: true },
  { id: 'ciezka_choroba_dziecka', label: 'Ciężka choroba lub niepełnosprawność dziecka', difficult: true },
  { id: 'samotny_rodzic', label: 'Samotne rodzicielstwo', difficult: false },
  { id: 'trudna_sytuacja_materialna', label: 'Trudna sytuacja materialna', difficult: false },
  { id: 'przyjecie_dziecka', label: 'Przyjęcie dziecka na wychowanie', difficult: false },
]

export const isSituationId = (value: string | null): value is SupportSituation => situations.some((s) => s.id === value)

export const situationLabel = (id: string): string => situations.find((s) => s.id === id)?.label ?? id
