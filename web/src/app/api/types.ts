// Typy kontraktu API. Źródło prawdy: contracts/*.schema.json i contracts/openapi.yaml.
// Edytuje wyłącznie koordynator, razem ze zmianą kontraktu.

export type IsoDate = string

export type FactSource = 'mobywatel' | 'zus' | 'ezla' | 'usc' | 'user' | 'assumption'

export interface Fact<T> {
  value: T
  source: FactSource
  page?: number
}

export type EventKind = 'planuje' | 'ciaza' | 'urodzenie' | 'przyjecie'
export type CalculationScope = 'ja' | 'ja_i_drugi_rodzic'
export type AdoptionPath = 'przysposobienie' | 'rodzina_zastepcza_niezawodowa' | 'rodzina_zastepcza_zawodowa'
export type MotherSituationKind =
  | 'niezdolnosc_do_samodzielnej_egzystencji'
  | 'pobyt_w_szpitalu'
  | 'zgon'
  | 'porzucenie_dziecka'
export type ParentRole = 'matka' | 'ojciec'
export type TitleKind = 'pracownik' | 'zleceniobiorca' | 'dzialalnosc' | 'osoba_wspolpracujaca' | 'pobierajacy_zasilek' | 'inny'
export type SicknessInsurance = 'obowiazkowe' | 'dobrowolne' | 'brak'
export type ReceivedBenefitKind =
  | 'wynagrodzenie_chorobowe'
  | 'chorobowy'
  | 'rehabilitacyjne'
  | 'macierzynski'
  | 'macierzynski_uzupelniajacy'
  | 'rodzicielski'
  | 'ojcowski'
  | 'opiekunczy'
  | 'inne'
export type InsuranceEndReason =
  | 'wypowiedzenie_przez_pracownika'
  | 'wypowiedzenie_przez_pracodawce'
  | 'porozumienie_stron'
  | 'koniec_umowy_na_czas_okreslony'
  | 'likwidacja_pracodawcy'
  | 'upadlosc_pracodawcy'
  | 'smierc_pracodawcy'
  | 'naruszenie_prawa_orzeczone_przez_sad'
  | 'zamkniecie_dzialalnosci'
  | 'inna'
export type PaymentVariant = 'standardowy' | 'usredniony'

export interface DateRange { od: IsoDate; do: IsoDate; page?: number }

export interface InsuranceTitle {
  rodzajTytulu: TitleKind
  od: IsoDate
  do?: IsoDate | null
  chorobowe: SicknessInsurance
  kodTytulu?: string
  opis?: string
  platnikId?: string
  platnikNazwa?: string
  wymiarEtatu?: number
  page?: number
}

export interface ContributionBase { miesiac: string; kwota: number; platnikId?: string; page?: number }
export interface InsuranceBreak { od: IsoDate; do: IsoDate; opis?: string; page?: number }
export interface ReceivedBenefit { rodzaj: ReceivedBenefitKind; od: IsoDate; do: IsoDate; opis?: string; page?: number }
export interface SickLeave { od: IsoDate; do: IsoDate; kod?: string | null; szpital: boolean; dataWystawienia?: IsoDate; page?: number }
export interface ContributionDebt { naDzien: IsoDate; kwota: number }
export interface MotherSituation { rodzaj: MotherSituationKind; od: IsoDate; do?: IsoDate | null }
export interface LeavePart { tygodnie: number; od?: IsoDate }
export interface PartTimeWork { wymiar: number; tygodnie: number; od?: IsoDate }

export interface CaseFacts {
  'zdarzenie.rodzaj'?: Fact<EventKind>
  'zdarzenie.data'?: Fact<IsoDate>
  'zdarzenie.dlaKogo'?: Fact<CalculationScope>
  'dziecko.liczba'?: Fact<number>
  'dziecko.dataUrodzenia'?: Fact<IsoDate>
  'dziecko.tydzienCiazyPrzyPorodzie'?: Fact<number>
  'dziecko.masaUrodzeniowaG'?: Fact<number>
  'dziecko.pobytyWSzpitalu'?: Fact<DateRange[]>
  'dziecko.zaZyciem'?: Fact<boolean>
  'dziecko.martweUrodzenie'?: Fact<boolean>
  'dziecko.liczbaZmarlych'?: Fact<number>
  'dziecko.dataZgonu'?: Fact<IsoDate>
  'dziecko.dataPochowku'?: Fact<IsoDate>
  'przyjecie.sciezka'?: Fact<AdoptionPath>
  'przyjecie.wniosekDoSadu'?: Fact<boolean>
  'przyjecie.dataUprawomocnieniaPostanowienia'?: Fact<IsoDate>
  'przyjecie.odroczenieObowiazkuSzkolnego'?: Fact<boolean>
  'matka.sytuacjaSzczegolna'?: Fact<MotherSituation>
}

export interface PersonFacts {
  'osoba.rola'?: Fact<ParentRole>
  'osoba.dataUrodzenia'?: Fact<IsoDate>
  'osoba.dataUkonczeniaSzkoly'?: Fact<IsoDate>
  'ubezpieczenie.tytuly'?: Fact<InsuranceTitle[]>
  'ubezpieczenie.podstawySkladek'?: Fact<ContributionBase[]>
  'ubezpieczenie.przerwy'?: Fact<InsuranceBreak[]>
  'ubezpieczenie.pobraneSwiadczenia'?: Fact<ReceivedBenefit[]>
  'ubezpieczenie.latWczesniejszegoObowiazkowego'?: Fact<number>
  'ubezpieczenie.podlegaChorobowemu'?: Fact<boolean>
  'ubezpieczenie.ustaniePrzyczyna'?: Fact<InsuranceEndReason>
  'umowa.naCzasOkreslony'?: Fact<boolean>
  'umowa.dataKonca'?: Fact<IsoDate>
  'umowa.planowaneOdejscieData'?: Fact<IsoDate>
  'platnik.liczbaUbezpieczonych'?: Fact<number>
  'skladki.zadluzenie'?: Fact<ContributionDebt>
  'skladki.dataSplaty'?: Fact<IsoDate>
  'ezla.zwolnienia'?: Fact<SickLeave[]>
  'podstawa.ustalonaPrzezPlatnika'?: Fact<number>
  'plan.wariantWyplaty'?: Fact<PaymentVariant>
  'plan.dataWnioskuUsrednionego'?: Fact<IsoDate>
  'plan.macierzynskiPrzedPorodemTyg'?: Fact<number>
  'plan.rezygnacjaZMacierzynskiegoPoTyg'?: Fact<number>
  'plan.rodzicielskiCzesci'?: Fact<LeavePart[]>
  'plan.pracaNaCzescEtatu'?: Fact<PartTimeWork[]>
  'plan.ojcowskiCzesci'?: Fact<LeavePart[]>
}

export type CaseFactKey = keyof CaseFacts
export type PersonFactKey = keyof PersonFacts
export type FactKey = CaseFactKey | PersonFactKey

export interface FactSet {
  sprawa: CaseFacts
  wnioskodawca: PersonFacts
  drugiRodzic?: PersonFacts
}

export type PersonRef = 'wnioskodawca' | 'drugiRodzic'

export type BenefitId =
  | 'chorobowy_w_ciazy'
  | 'rehabilitacyjne'
  | 'zasilek_do_dnia_porodu'
  | 'macierzynski'
  | 'macierzynski_uzupelniajacy'
  | 'rodzicielski'
  | 'ojcowski'
  | 'przyjecie_dziecka'
  | 'przejecie_przez_ojca'
  | 'pogrzebowy'

export type BenefitStatus = 'przysługuje' | 'nie_przysługuje' | 'brakuje_danych' | 'nie_dotyczy' | 'nie_rozstrzygam'
export type Payer = 'płatnik_składek' | 'zus'
export type ApplicationPlace = 'pracodawca' | 'płatnik_składek' | 'zus'
export type BasisSource = 'szacunek_zus' | 'ustalona_przez_płatnika' | 'minimalna'
export type DeadlineType = 'najpóźniej_do' | 'najpóźniej_przed' | 'okno' | 'przedawnienie'
export type MissingFactType = 'date' | 'integer' | 'number' | 'money' | 'boolean' | 'enum' | 'list'

export interface AssessmentRequest {
  facts: FactSet
  asOf?: IsoDate
  completedDeadlines?: string[]
}

export interface Period { from: IsoDate; to: IsoDate; days: number; weeks: number }
export interface Amount { daily: number; total: number; basis: number; basisSource: BasisSource }
export interface BenefitPart { label: string; period: Period; rate: number; amount?: Amount | null }
export interface Deadline {
  id: string
  type: DeadlineType
  date: IsoDate
  text: string
  consequence?: string | null
  basis?: string | null
}
export interface TraceStep { rule: string; basis: string; met: boolean | null; text: string; condition?: string | null }
export interface Link { label: string; url: string }

export interface BenefitResult {
  id: BenefitId
  person: PersonRef
  status: BenefitStatus
  period?: Period | null
  rate?: number | null
  amount?: Amount | null
  /** Kwota ryczałtowa (zasiłek pogrzebowy) z parametru. */
  lumpSum?: number | null
  parts: BenefitPart[]
  payer?: Payer | null
  whereToApply?: ApplicationPlace | null
  documents: string[]
  deadlines: Deadline[]
  reason?: string | null
  legalBasis?: string | null
  whatWouldChange?: string | null
  notes: string[]
  links: Link[]
  trace: TraceStep[]
}

export interface MissingFact {
  fact: FactKey
  question: string
  type: MissingFactType
  person?: PersonRef | null
  options?: string[] | null
  /** Świadczenia, których dotyczy pytanie (karty tej samej osoby); puste = pytanie ogólne. */
  benefitIds?: BenefitId[] | null
}

export interface ValidationIssue { code: string; message: string; fact?: FactKey | null; person?: PersonRef | null }
export interface NextStep { deadlineId: string; benefitId: BenefitId; person?: PersonRef }
export interface Risk { id: string; text: string; basis?: string | null; link?: Link | null }

export interface AssessmentResult {
  rulesetVersion: string
  benefits: BenefitResult[]
  missingFacts: MissingFact[]
  validationErrors?: ValidationIssue[]
  nextStep?: NextStep | null
  risks: Risk[]
  difficultSituation?: boolean
}

export type ScenarioId =
  | 'odchodze_z_pracy'
  | 'umowa_nie_zostanie_przedluzona'
  | 'przechodze_na_dzialalnosc'
  | 'drugi_rodzic_bierze'
  | 'praca_na_czesc_etatu'
  | 'wariant_wyplaty'
  | 'splacam_zadluzenie'

export interface ScenarioChange {
  id: ScenarioId
  person?: PersonRef
  params?: {
    data?: IsoDate
    dobrowolneChorobowe?: boolean
    tygodnie?: number
    wymiar?: number
    wariant?: PaymentVariant
  }
}

export interface CompareRequest { facts: FactSet; scenarios: ScenarioChange[]; asOf?: IsoDate }
export interface BenefitDiff {
  id: BenefitId
  person: PersonRef
  statusBase: BenefitStatus
  statusVariant: BenefitStatus
  weeksDelta: number | null
  amountDelta: number | null
}
export interface CompareResult {
  base: AssessmentResult
  variant: AssessmentResult
  diff: BenefitDiff[]
  validationErrors: ValidationIssue[]
  /** Pełny zbiór faktów po nakładce scenariuszy — ocena daje dokładnie `variant`. null, gdy scenariusza nie zastosowano. */
  variantFacts?: FactSet | null
  /** Co zmieniono w faktach, prostym językiem. */
  changes?: string[]
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

export interface Health { status: 'ok' | 'degraded'; api: 'ok'; database: 'ok' | 'unavailable' }

/** PESEL jest tylko do wyświetlenia przy logowaniu — nie przechowuj go i nie wysyłaj dalej (zasada 2.5). */
export interface Identity { imie: string; nazwisko: string; nazwiskoRodowe?: string | null; pesel: string; dataUrodzenia: IsoDate }
export interface LoginResponse { sessionId: string; identity: Identity; simulated: true }
export interface PersonaListItem { id: string; opis: string; imie?: string; caseId?: string | null }

export interface ZusAccount {
  simulated: true
  tytuly: InsuranceTitle[]
  podstawySkladek: ContributionBase[]
  przerwy: InsuranceBreak[]
  pobraneSwiadczenia: ReceivedBenefit[]
  zwolnienia: SickLeave[]
  saldo?: ContributionDebt | null
}

export type SubmissionType = 'wniosek_urlop' | 'dane_zam' | 'zapytanie_ezus' | 'odpis_aktu'
export type LeaveTemplate = 'macierzynski_i_rodzicielski' | 'rodzicielski' | 'ojcowski' | 'uzupelniajacy'
export interface SubmissionRequest { type: SubmissionType; template?: LeaveTemplate; fields: Record<string, string> }
export type TemplateFieldKind = 'text' | 'longtext' | 'date' | 'integer' | 'choice'
export interface TemplateField {
  key: string
  label: string
  kind: TemplateFieldKind
  required: boolean
  maxLength: number
  basis: string
  when: { key: string; value: string } | null
  options: { value: string; label: string }[] | null
}
export interface SubmissionTemplate {
  id: string
  type: SubmissionType
  template: LeaveTemplate | null
  recipient: SubmissionReceipt['recipient']
  title: string
  fields: TemplateField[]
}
export interface SubmissionReceipt {
  id: string
  number: string
  type: SubmissionType
  recipient: 'pracodawca' | 'zus' | 'ezus' | 'urzad_stanu_cywilnego'
  submittedAt: string
  pdfUrl?: string | null
  status?: string
  summary?: string
  simulated: true
}

export type CaseDecision = 'odmowa' | 'przyznanie' | 'do_odczytania'
export type CaseAgreement = 'kwota' | 'okres' | 'decyzja'
export interface CaseConfirmedPeriod { swiadczenie: string; od: string; do: string; dni?: number | null; tygodnie?: number | null }
export interface CaseSummary {
  id: string
  tytul: string
  oczekiwanaDecyzja: CaseDecision
  persona?: string | null
  dostepna: boolean
  zgodnosc?: CaseAgreement | null
  /** Okresy świadczeń odczytane z akt sprawy ZUS. */
  potwierdzoneOkresy?: CaseConfirmedPeriod[] | null
}

export type SupportSituation =
  | 'poronienie'
  | 'martwe_urodzenie'
  | 'zgon_dziecka'
  | 'wczesniak'
  | 'ciezka_choroba_dziecka'
  | 'sytuacja_matki'
  | 'samotny_rodzic'
  | 'trudna_sytuacja_materialna'
  | 'przyjecie_dziecka'

export interface Organization {
  id: string
  nazwa: string
  opis: string
  sytuacje: SupportSituation[]
  url?: string | null
  telefon?: string | null
  zweryfikowano: boolean
}

export type ParameterValue = number | string | null
export interface Parameter {
  key: string
  wiersz?: string
  value: ParameterValue
  unit?: string
  validFrom: IsoDate
  legalBasis: string
  status: ParameterStatus
  note?: string
}
export type ParameterStatus = 'ok' | 'do_weryfikacji'
export interface ParameterChange {
  key: string
  value: ParameterValue
  validFrom: IsoDate
  legalBasis?: string
  reason?: string
  /** Bez pola — status poprzedniej wartości tego klucza. */
  status?: ParameterStatus
}
export interface ParameterHistoryEntry {
  key: string
  value: ParameterValue
  previousValue?: ParameterValue
  validFrom: IsoDate
  changedAt: string
  reason?: string | null
}
export interface CatalogEntry { id: BenefitId; nazwa: string; wlaczone: boolean }
export interface IntegrationField { source: FactSource; fact: FactKey; wlaczone: boolean }

export interface AssistantAnswer {
  answer: string
  /** notes: daty wejścia w życie lub brzmienia ustępów cytowanego artykułu (z przypisów aktu). */
  citations: { document: string; fragment: string; article: string; articleText: string; notes?: string[] }[]
  disclaimer: string
  /** faq: gotowa odpowiedź na typowe pytanie (bez modelu). */
  mode: 'faq' | 'ai' | 'search' | 'no_results' | 'calculator'
}
