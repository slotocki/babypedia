import type {
  ApplicationPlace,
  BasisSource,
  BenefitId,
  BenefitResult,
  BenefitStatus,
  DeadlineType,
  Payer,
  PaymentVariant,
  PersonRef,
} from '@/app/api/types'

export const benefitNames: Record<BenefitId, string> = {
  chorobowy_w_ciazy: 'Zasiłek chorobowy w okresie ciąży',
  rehabilitacyjne: 'Świadczenie rehabilitacyjne',
  zasilek_do_dnia_porodu: 'Zasiłek w wysokości zasiłku macierzyńskiego (do dnia porodu)',
  macierzynski: 'Zasiłek macierzyński za okres urlopu macierzyńskiego',
  macierzynski_uzupelniajacy: 'Zasiłek za okres uzupełniającego urlopu macierzyńskiego',
  rodzicielski: 'Zasiłek za okres urlopu rodzicielskiego',
  ojcowski: 'Zasiłek za okres urlopu ojcowskiego',
  przyjecie_dziecka: 'Zasiłek za okres urlopu na warunkach urlopu macierzyńskiego (przyjęcie dziecka)',
  przejecie_przez_ojca: 'Przejęcie zasiłku przez ojca w sytuacjach szczególnych matki',
  pogrzebowy: 'Zasiłek pogrzebowy',
}

export const benefitName = (id: string): string => (id in benefitNames ? benefitNames[id as BenefitId] : id)

export const personLabels: Record<PersonRef, string> = { wnioskodawca: 'Ty', drugiRodzic: 'Drugi rodzic' }

export const basisSourceLabels: Record<BasisSource, string> = {
  szacunek_zus: 'szacunek z danych ZUS',
  ustalona_przez_płatnika: 'podstawa ustalona przez płatnika',
  minimalna: 'minimalna podstawa',
}

export const deadlineTypeLabels: Record<DeadlineType, string> = {
  najpóźniej_do: 'najpóźniej do',
  najpóźniej_przed: 'najpóźniej przed',
  okno: 'w okresie',
  przedawnienie: 'przedawnienie',
}

export const payerLabels: Record<Payer, string> = { płatnik_składek: 'Płatnik składek (np. pracodawca)', zus: 'ZUS' }

export const placeLabels: Record<ApplicationPlace, string> = {
  pracodawca: 'U pracodawcy',
  płatnik_składek: 'U płatnika składek',
  zus: 'W ZUS',
}

export const variantLabels: Record<PaymentVariant, string> = {
  standardowy: 'Standardowy',
  usredniony: 'Uśredniony (81,5%)',
}

export const statusRank: Record<BenefitStatus, number> = {
  przysługuje: 0,
  brakuje_danych: 1,
  nie_rozstrzygam: 1,
  nie_przysługuje: 2,
  nie_dotyczy: 3,
}

export const statusSummaryLabels: Record<Exclude<BenefitStatus, 'nie_dotyczy'>, string> = {
  przysługuje: 'przysługuje',
  brakuje_danych: 'brakuje danych',
  nie_rozstrzygam: 'nie rozstrzygamy',
  nie_przysługuje: 'nie przysługuje',
}

export function sortBenefits(benefits: BenefitResult[]): BenefitResult[] {
  return benefits
    .map((benefit, index) => ({ benefit, index }))
    .sort(
      (a, b) =>
        statusRank[a.benefit.status] - statusRank[b.benefit.status] ||
        (a.benefit.period?.from ?? '9999').localeCompare(b.benefit.period?.from ?? '9999') ||
        a.index - b.index,
    )
    .map((entry) => entry.benefit)
}

export const benefitKey = (b: Pick<BenefitResult, 'id' | 'person'>) => `${b.id}:${b.person}`

export function todayIso(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

export function isHttpUrl(url: string): boolean {
  return /^https?:\/\//i.test(url)
}
