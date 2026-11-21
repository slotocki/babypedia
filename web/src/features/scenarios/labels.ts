import type { BenefitId, BenefitStatus, PersonRef } from '@/app/api/types'

export const benefitNames: Record<BenefitId, string> = {
  chorobowy_w_ciazy: 'Zasiłek chorobowy w ciąży',
  rehabilitacyjne: 'Świadczenie rehabilitacyjne',
  zasilek_do_dnia_porodu: 'Zasiłek do dnia porodu',
  macierzynski: 'Zasiłek macierzyński',
  macierzynski_uzupelniajacy: 'Uzupełniający zasiłek macierzyński',
  rodzicielski: 'Urlop rodzicielski',
  ojcowski: 'Zasiłek ojcowski',
  przyjecie_dziecka: 'Zasiłek przy przyjęciu dziecka',
  przejecie_przez_ojca: 'Przejęcie zasiłku przez ojca',
  pogrzebowy: 'Zasiłek pogrzebowy',
}

export const statusText: Record<BenefitStatus, string> = {
  przysługuje: 'przysługuje',
  nie_przysługuje: 'nie przysługuje',
  brakuje_danych: 'brakuje danych',
  nie_dotyczy: 'nie dotyczy',
  nie_rozstrzygam: 'nie rozstrzygam (zapytaj ZUS)',
}

export const personLabel = (person: PersonRef): string => (person === 'drugiRodzic' ? 'drugi rodzic' : 'Ty')
