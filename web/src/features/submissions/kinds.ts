import type { BenefitId, BenefitResult, FactSet, LeaveTemplate, SubmissionReceipt, SubmissionType } from '@/app/api/types'
import { paths } from '@/app/paths'

export interface SubmissionKind {
  id: string
  type: SubmissionType
  template?: LeaveTemplate
  title: string
  description: string
  recipient: string
  note?: { text: string; linkLabel: string; href: string }
}

export const leaveKinds: SubmissionKind[] = [
  {
    id: 'macierzynski_i_rodzicielski',
    type: 'wniosek_urlop',
    template: 'macierzynski_i_rodzicielski',
    title: 'Urlop macierzyński i rodzicielski (pełny wymiar)',
    description: 'Jeden wniosek do pracodawcy o część urlopu macierzyńskiego przed porodem i o urlop rodzicielski.',
    recipient: 'Pracodawca',
  },
  {
    id: 'rodzicielski',
    type: 'wniosek_urlop',
    template: 'rodzicielski',
    title: 'Urlop rodzicielski lub jego część',
    description: 'Prosisz pracodawcę o urlop rodzicielski. Możesz też od razu poprosić o łączenie go z pracą.',
    recipient: 'Pracodawca',
  },
  {
    id: 'ojcowski',
    type: 'wniosek_urlop',
    template: 'ojcowski',
    title: 'Urlop ojcowski lub jego część',
    description: 'Wniosek ojca dziecka do pracodawcy o urlop ojcowski.',
    recipient: 'Pracodawca',
  },
  {
    id: 'uzupelniajacy',
    type: 'wniosek_urlop',
    template: 'uzupelniajacy',
    title: 'Uzupełniający urlop macierzyński',
    description: 'Wniosek do pracodawcy o dodatkowy urlop po urodzeniu dziecka, np. wcześniaka lub po pobycie dziecka w szpitalu.',
    recipient: 'Pracodawca',
  },
]

export const otherKinds: SubmissionKind[] = [
  {
    id: 'dane_zam',
    type: 'dane_zam',
    title: 'Dane do wniosku o zasiłek macierzyński (ZAM)',
    description: 'Zestawienie danych, które trzeba podać we wniosku o zasiłek macierzyński.',
    recipient: 'ZUS',
    note: {
      text: 'To zestawienie danych do przepisania, a nie druk ZUS. Sam wniosek złożysz na stronie',
      linkLabel: 'zus.pl',
      href: 'https://www.zus.pl',
    },
  },
  {
    id: 'zapytanie_ezus',
    type: 'zapytanie_ezus',
    title: 'Zapytanie do ZUS',
    description: 'Pytanie o dane na Twoim koncie ubezpieczonego, np. o okresy ubezpieczenia, podstawy składek albo zwolnienia.',
    recipient: 'eZUS (konto w PUE)',
  },
  {
    id: 'odpis_aktu',
    type: 'odpis_aktu',
    title: 'Wniosek o odpis aktu urodzenia',
    description: 'Odpis aktu urodzenia dziecka dołącza się do wniosków o urlopy.',
    recipient: 'Urząd stanu cywilnego',
    note: {
      text: 'Informacje o rejestrze stanu cywilnego:',
      linkLabel: 'gov.pl, rejestr stanu cywilnego',
      href: 'https://www.gov.pl/web/cyfryzacja/rejestr-stanu-cywilnego',
    },
  },
]

export const allKinds = [...leaveKinds, ...otherKinds]

export const findKind = (id: string | null) => allKinds.find((kind) => kind.id === id)

export const submissionFormPath = (id: string) =>
  paths.submissions + '?' + new URLSearchParams({ widok: 'formularz', szablon: id }).toString()

const leaveByBenefit: Partial<Record<BenefitId, LeaveTemplate>> = {
  rodzicielski: 'rodzicielski',
  ojcowski: 'ojcowski',
  macierzynski_uzupelniajacy: 'uzupelniajacy',
}

const maternityBenefits: BenefitId[] = ['macierzynski', 'macierzynski_uzupelniajacy', 'rodzicielski', 'ojcowski', 'przyjecie_dziecka', 'przejecie_przez_ojca']

export function applicationFor(benefit: BenefitResult, facts: FactSet) {
  if (benefit.person !== 'wnioskodawca' || benefit.status !== 'przysługuje' || !maternityBenefits.includes(benefit.id)) return null

  const date = benefit.period?.from ?? facts.sprawa['zdarzenie.data']?.value
  const employee = date && facts.wnioskodawca['ubezpieczenie.tytuly']?.value.some(
    (title) => title.rodzajTytulu === 'pracownik' && title.od <= date && (!title.do || title.do >= date),
  )
  const leave = employee ? leaveByBenefit[benefit.id] : undefined
  return {
    href: submissionFormPath(leave ?? 'dane_zam'),
    label: leave ? 'Przygotuj wniosek o urlop' : 'Przygotuj dane do wniosku ZAM',
  }
}

export const typeLabels: Record<SubmissionType, string> = {
  wniosek_urlop: 'Wniosek o urlop',
  dane_zam: 'Dane do wniosku ZAM',
  zapytanie_ezus: 'Zapytanie do ZUS',
  odpis_aktu: 'Wniosek o odpis aktu urodzenia',
}

export const recipientLabels: Record<SubmissionReceipt['recipient'], string> = {
  pracodawca: 'Pracodawca',
  zus: 'ZUS',
  ezus: 'eZUS (konto w PUE)',
  urzad_stanu_cywilnego: 'Urząd stanu cywilnego',
}

const statusLabels: Record<string, string> = {
  zlozone: 'Złożone (symulacja)',
}

export const statusLabel = (status?: string) => (status ? (statusLabels[status] ?? status) : 'Złożone (symulacja)')

export const dateTimeLabel = (iso: string) =>
  new Intl.DateTimeFormat('pl-PL', { dateStyle: 'long', timeStyle: 'short' }).format(new Date(iso))

/** Dzieli wnioski o urlop na pasujące do sytuacji teraz i resztę (pełna lista zostaje dostępna). Brak danych = wszystkie „teraz”. */
export function relevantLeaveKinds(facts: FactSet): { now: SubmissionKind[]; later: SubmissionKind[] } {
  const { sprawa } = facts
  const kind = sprawa['zdarzenie.rodzaj']?.value
  const role = facts.wnioskodawca['osoba.rola']?.value
  const preemie = (sprawa['dziecko.pobytyWSzpitalu']?.value ?? []).length > 0 || (sprawa['dziecko.tydzienCiazyPrzyPorodzie']?.value ?? 40) < 37
  const isLater = (k: SubmissionKind): boolean => {
    if (!kind) return false
    if (k.id === 'macierzynski_i_rodzicielski') return kind === 'urodzenie' || kind === 'przyjecie'
    if (k.id === 'ojcowski') return role === 'matka'
    if (k.id === 'uzupelniajacy') return kind === 'planuje' || kind === 'ciaza' || (kind === 'urodzenie' && !preemie) || kind === 'przyjecie'
    return false
  }
  return { now: leaveKinds.filter((k) => !isLater(k)), later: leaveKinds.filter(isLater) }
}
