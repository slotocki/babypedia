import type { DataSource } from '@/app/case-context'
import type { ReceivedBenefitKind, SicknessInsurance, TitleKind } from '@/app/api/types'

export const titleLabels: Record<TitleKind, string> = {
  pracownik: 'Pracownik (umowa o pracę)',
  zleceniobiorca: 'Zleceniobiorca (umowa zlecenia)',
  dzialalnosc: 'Prowadzący działalność gospodarczą',
  osoba_wspolpracujaca: 'Osoba współpracująca',
  pobierajacy_zasilek: 'Pobierający zasiłek',
  inny: 'Inny tytuł ubezpieczenia',
}

export const sicknessLabels: Record<SicknessInsurance, string> = {
  obowiazkowe: 'Ubezpieczenie chorobowe: obowiązkowe',
  dobrowolne: 'Ubezpieczenie chorobowe: dobrowolne',
  brak: 'Ubezpieczenie chorobowe: brak',
}

export const benefitLabels: Record<ReceivedBenefitKind, string> = {
  wynagrodzenie_chorobowe: 'Wynagrodzenie chorobowe',
  chorobowy: 'Zasiłek chorobowy',
  rehabilitacyjne: 'Świadczenie rehabilitacyjne',
  macierzynski: 'Zasiłek macierzyński',
  macierzynski_uzupelniajacy: 'Zasiłek macierzyński za uzupełniający urlop',
  rodzicielski: 'Zasiłek za urlop rodzicielski',
  ojcowski: 'Zasiłek za urlop ojcowski',
  opiekunczy: 'Zasiłek opiekuńczy',
  inne: 'Inne świadczenie',
}

export const sickLeaveCodes: Record<string, string> = {
  B: 'B = niezdolność do pracy w okresie ciąży',
}

const month = new Intl.DateTimeFormat('pl-PL', { month: 'long', year: 'numeric', timeZone: 'UTC' })

export function formatMonth(value: string): string {
  const [y, m] = value.split('-').map(Number)
  return month.format(new Date(Date.UTC(y, m - 1, 1)))
}

/** Źródła danych w kroku zgody przy logowaniu (symulacja). */
export const dataSources: { id: DataSource; label: string; description: string }[] = [
  { id: 'zus', label: 'ZUS — konto ubezpieczonego (PUE/eZUS)', description: 'Ubezpieczenia, podstawy składek, saldo składek.' },
  { id: 'ezla', label: 'e-ZLA — zwolnienia lekarskie', description: 'Okresy zwolnień, w tym pobyt w szpitalu.' },
  { id: 'usc', label: 'Rejestr stanu cywilnego — dane dziecka', description: 'Data urodzenia i liczba dzieci.' },
]
