import type { AssessmentResult, BenefitResult } from '@/app/api/types'

type BenefitInput = Partial<BenefitResult> & Pick<BenefitResult, 'id' | 'status'>

const benefit = (input: BenefitInput): BenefitResult => ({
  person: 'wnioskodawca',
  parts: [],
  documents: [],
  deadlines: [],
  notes: [],
  links: [],
  trace: [],
  ...input,
})

const hidden = (id: BenefitResult['id'], person: BenefitResult['person'] = 'wnioskodawca') => benefit({ id, status: 'nie_dotyczy', person })

const zus = { label: 'ZUS: zasiłek macierzyński', url: 'https://www.zus.pl' }

const przysluguje: AssessmentResult = {
  rulesetVersion: 'przyklad-1',
  benefits: [
    benefit({
      id: 'macierzynski',
      status: 'przysługuje',
      period: { from: '2027-03-15', to: '2027-08-01', days: 140, weeks: 20 },
      rate: 1,
      amount: { daily: 230.11, total: 32215.4, basis: 6903.2, basisSource: 'szacunek_zus' },
      payer: 'płatnik_składek',
      whereToApply: 'pracodawca',
      documents: ['Wniosek o urlop macierzyński', 'Zaświadczenie lekarskie o przewidywanej dacie porodu'],
      deadlines: [
        {
          id: 'wniosek_macierzynski',
          type: 'najpóźniej_przed',
          date: '2027-03-01',
          text: 'Złóż wniosek o macierzyński najpóźniej 14 dni przed początkiem urlopu.',
          consequence: 'Urlop może zacząć się później.',
          basis: 'Poradnik ZUS, s. 27',
        },
        {
          id: 'wniosek_usredniona',
          type: 'najpóźniej_do',
          date: '2027-04-05',
          text: 'Złóż wniosek o wariant 81,5% w ciągu 21 dni od porodu.',
          consequence: 'Za urlop rodzicielski przysługuje 70% podstawy.',
          basis: 'ustawa zasiłkowa art. 30a',
        },
      ],
      notes: ['Kwoty brutto, przed zaliczką na podatek.'],
      links: [zus],
      trace: [
        {
          rule: 'ubezpieczenie_w_dniu_porodu',
          basis: 'ustawa zasiłkowa art. 29 ust. 1 w związku z art. 30 ust. 1, Kodeks pracy art. 177 § 3 oraz art. 180 § 1',
          met: true,
          condition: 'Podlegasz ubezpieczeniu chorobowemu w dniu porodu',
          text: 'W dniu porodu podlegasz ubezpieczeniu chorobowemu jako pracownik.',
        },
        {
          rule: 'wymiar_urlopu',
          basis: 'Kodeks pracy art. 180 § 1',
          met: true,
          condition: 'Rodzisz jedno dziecko',
          text: 'Przy jednym dziecku urlop macierzyński trwa 20 tygodni.',
        },
        {
          rule: 'zadluzenie',
          basis: 'ustawa zasiłkowa art. 2a',
          met: null,
          condition: 'Brak zadłużenia z tytułu składek',
          text: 'Nie sprawdzamy zadłużenia, bo dotyczy ono tylko osób prowadzących działalność.',
        },
      ],
    }),
    benefit({
      id: 'rodzicielski',
      status: 'przysługuje',
      period: { from: '2027-08-02', to: '2028-03-12', days: 224, weeks: 32 },
      rate: 0.7,
      amount: { daily: 161.07, total: 36079.68, basis: 6903.2, basisSource: 'ustalona_przez_płatnika' },
      parts: [
        {
          label: 'Część 1 (do 5 części, razem 32 tygodnie)',
          period: { from: '2027-08-02', to: '2027-12-26', days: 148, weeks: 21.14 },
          rate: 0.7,
          amount: { daily: 161.07, total: 23838.36, basis: 6903.2, basisSource: 'ustalona_przez_płatnika' },
        },
        {
          label: 'Część 2',
          period: { from: '2028-01-03', to: '2028-03-12', days: 76, weeks: 10.86 },
          rate: 0.7,
          amount: null,
        },
      ],
      payer: 'płatnik_składek',
      whereToApply: 'pracodawca',
      documents: ['Wniosek o urlop rodzicielski'],
      deadlines: [
        {
          id: 'wniosek_rodzicielski',
          type: 'najpóźniej_przed',
          date: '2027-07-12',
          text: 'Złóż wniosek o urlop rodzicielski najpóźniej 21 dni przed jego początkiem.',
          consequence: null,
          basis: 'Kodeks pracy art. 182¹d',
        },
        {
          id: 'okno_rodzicielski',
          type: 'okno',
          date: '2032-12-31',
          text: 'Urlop rodzicielski możesz wykorzystać do końca roku, w którym dziecko skończy 6 lat.',
          consequence: 'Po tym terminie prawo do urlopu rodzicielskiego wygasa.',
          basis: 'Kodeks pracy art. 182¹c',
        },
        {
          id: 'zgloszenie_zaleglosc',
          type: 'najpóźniej_do',
          date: '2026-11-02',
          text: 'Zgłoś płatnikowi zmianę danych do wniosku.',
          consequence: 'Wypłata zasiłku może się opóźnić.',
          basis: null,
        },
      ],
      trace: [
        {
          rule: 'rodzicielski_wymiar',
          basis: 'Kodeks pracy art. 182¹a § 1',
          met: true,
          text: 'Przy jednym dziecku urlop rodzicielski trwa do 41 tygodni; część nieprzenoszalną drugiego rodzica (9 tygodni) odliczamy.',
        },
      ],
    }),
    benefit({ id: 'chorobowy_w_ciazy', status: 'nie_dotyczy' }),
    hidden('pogrzebowy'),
  ],
  missingFacts: [],
  nextStep: { deadlineId: 'wniosek_macierzynski', benefitId: 'macierzynski' },
  risks: [
    {
      id: 'termin_21_dni',
      text: 'Jeśli nie złożysz wniosku o wariant 81,5% w ciągu 21 dni od porodu, za urlop rodzicielski dostaniesz 70% podstawy.',
      basis: 'ustawa zasiłkowa art. 30a ust. 1 w związku z art. 31 ust. 3',
      link: { label: 'Urlop rodzicielski na gov.pl', url: 'https://www.gov.pl/web/rodzina/urlop-rodzicielski' },
    },
    {
      id: 'dobrowolne_chorobowe',
      text: 'Dobrowolne ubezpieczenie chorobowe ustaje po nabyciu prawa do zasiłku macierzyńskiego; trzeba zgłosić się do niego ponownie.',
    },
  ],
}

const bezKwoty: AssessmentResult = {
  rulesetVersion: 'przyklad-2',
  benefits: [
    benefit({
      id: 'macierzynski',
      status: 'przysługuje',
      period: { from: '2027-03-15', to: '2027-08-01', days: 140, weeks: 20 },
      rate: 1,
      amount: null,
      payer: 'zus',
      whereToApply: 'zus',
      documents: ['Wniosek ZAM'],
      deadlines: [
        {
          id: 'wniosek_zam',
          type: 'najpóźniej_przed',
          date: '2027-03-01',
          text: 'Złóż wniosek ZAM najpóźniej 14 dni przed początkiem zasiłku.',
          consequence: 'Zasiłek zacznie się później.',
          basis: null,
        },
      ],
      notes: ['Podaj podstawę, żeby zobaczyć kwotę.'],
      trace: [{ rule: 'ubezpieczenie_w_dniu_porodu', basis: 'ustawa zasiłkowa art. 29', met: true, text: 'W dniu porodu podlegasz ubezpieczeniu chorobowemu.' }],
    }),
    benefit({
      id: 'rodzicielski',
      status: 'przysługuje',
      period: { from: '2027-08-02', to: '2028-03-12', days: 224, weeks: 32 },
      rate: 0.7,
      amount: null,
      payer: 'zus',
      whereToApply: 'zus',
    }),
  ],
  missingFacts: [],
  validationErrors: [
    {
      code: 'rodzicielski_za_dlugo',
      message: 'Jeden rodzic nie może wziąć 33 tygodni urlopu rodzicielskiego z 41, bo 9 tygodni jest zarezerwowane dla drugiego rodzica.',
      fact: 'plan.rodzicielskiCzesci',
      person: 'wnioskodawca',
    },
  ],
  nextStep: { deadlineId: 'wniosek_zam', benefitId: 'macierzynski' },
  risks: [],
}

const odmowa: AssessmentResult = {
  rulesetVersion: 'przyklad-3',
  benefits: [
    benefit({
      id: 'macierzynski',
      status: 'nie_przysługuje',
      reason: 'Masz zaległości w składkach powyżej progu (120,00 zł), a prowadzisz działalność, więc zasiłek nie przysługuje do czasu ich spłaty.',
      legalBasis: 'ustawa z dnia 25 czerwca 1999 r. o świadczeniach pieniężnych z ubezpieczenia społecznego w razie choroby i macierzyństwa, art. 2a ust. 1 i 2',
      whatWouldChange: 'Spłać całe zadłużenie (120,00 zł) do 15 maja 2027. Po spłacie prawo do zasiłku zostanie zachowane.',
      deadlines: [
        {
          id: 'splata_zadluzenia',
          type: 'najpóźniej_do',
          date: '2027-05-15',
          text: 'Spłać zadłużenie składkowe 120,00 zł.',
          consequence: 'Po tej dacie prawo do zasiłku przepada.',
          basis: 'ustawa zasiłkowa art. 2a ust. 2',
        },
        {
          id: 'przedawnienie_roszczenia',
          type: 'przedawnienie',
          date: '2028-02-01',
          text: 'Roszczenie o zasiłek przedawnia się po 6 miesiącach od ostatniego dnia okresu zasiłku.',
          consequence: 'Roszczenie przepada.',
          basis: 'ustawa zasiłkowa art. 67',
        },
      ],
      trace: [
        { rule: 'ubezpieczenie_w_dniu_porodu', basis: 'ustawa zasiłkowa art. 29 ust. 1', met: true, text: 'W dniu porodu podlegasz dobrowolnemu ubezpieczeniu chorobowemu.' },
        {
          rule: 'zadluzenie',
          basis: 'ustawa zasiłkowa art. 2a ust. 1',
          met: false,
          condition: 'Zadłużenie nie przekracza 1% płacy minimalnej (48,06 zł)',
          text: 'Twoje zadłużenie wynosi 120,00 zł i przekracza próg 48,06 zł.',
        },
      ],
    }),
    benefit({
      id: 'chorobowy_w_ciazy',
      status: 'nie_przysługuje',
      reason: 'Nie minął jeszcze 90-dniowy okres wyczekiwania dla dobrowolnego ubezpieczenia chorobowego.',
      legalBasis: 'ustawa zasiłkowa art. 4 ust. 1 pkt 2',
      whatWouldChange: null,
    }),
    benefit({ id: 'ojcowski', status: 'nie_dotyczy' }),
  ],
  missingFacts: [],
  nextStep: { deadlineId: 'splata_zadluzenia', benefitId: 'macierzynski' },
  risks: [
    {
      id: 'zadluzenie',
      text: 'Zadłużenie składkowe powyżej progu w dniu powstania prawa wstrzymuje zasiłek do czasu spłaty całości.',
      basis: 'ustawa zasiłkowa art. 2a',
      link: { label: 'ZUS: rozliczenia z ZUS', url: 'https://www.zus.pl' },
    },
  ],
}

const brakDanych: AssessmentResult = {
  rulesetVersion: 'przyklad-4',
  benefits: [
    benefit({
      id: 'macierzynski',
      status: 'brakuje_danych',
      trace: [
        { rule: 'ubezpieczenie_w_dniu_porodu', basis: 'ustawa zasiłkowa art. 29 ust. 1', met: null, condition: 'Podlegasz ubezpieczeniu chorobowemu w dniu porodu', text: 'Nie wiemy, do kiedy trwa Twoja umowa.' },
      ],
    }),
    benefit({
      id: 'rodzicielski',
      status: 'brakuje_danych',
    }),
    benefit({
      id: 'chorobowy_w_ciazy',
      status: 'przysługuje',
      period: { from: '2027-01-10', to: '2027-02-28', days: 50, weeks: 7.14 },
      rate: 1,
      amount: { daily: 230.11, total: 11505.5, basis: 6903.2, basisSource: 'minimalna' },
      payer: 'zus',
      whereToApply: 'pracodawca',
    }),
  ],
  missingFacts: [
    { fact: 'umowa.dataKonca', question: 'Do kiedy trwa Twoja umowa?', type: 'date', person: 'wnioskodawca', options: null },
    { fact: 'umowa.naCzasOkreslony', question: 'Czy masz umowę na czas określony?', type: 'boolean', person: 'wnioskodawca', options: null },
    { fact: 'przyjecie.sciezka', question: 'Którą ścieżkę przyjęcia dziecka wybierasz?', type: 'enum', person: null, options: ['przysposobienie'] },
  ],
  nextStep: null,
  risks: [{ id: 'koniec_umowy', text: 'Koniec umowy przed porodem może wpłynąć na prawo do zasiłku macierzyńskiego.', basis: 'ustawa zasiłkowa art. 30' }],
}

const trudna: AssessmentResult = {
  rulesetVersion: 'przyklad-5',
  difficultSituation: true,
  benefits: [
    benefit({
      id: 'macierzynski',
      status: 'przysługuje',
      period: { from: '2026-10-08', to: '2026-12-02', days: 56, weeks: 8 },
      rate: 1,
      amount: { daily: 230.11, total: 12886.16, basis: 6903.2, basisSource: 'ustalona_przez_płatnika' },
      payer: 'zus',
      whereToApply: 'zus',
      documents: ['Wniosek ZAM', 'Akt zgonu dziecka lub karta martwego urodzenia'],
      deadlines: [
        {
          id: 'wniosek_zam',
          type: 'najpóźniej_do',
          date: '2027-04-01',
          text: 'Złóż wniosek ZAM o zasiłek za 8 tygodni po porodzie.',
          consequence: null,
          basis: 'Kodeks pracy art. 180¹ § 1',
        },
        {
          id: 'przedawnienie_macierzynski',
          type: 'przedawnienie',
          date: '2027-06-02',
          text: 'Roszczenie o zasiłek przedawnia się po 6 miesiącach od ostatniego dnia okresu.',
          consequence: 'Roszczenie przepada.',
          basis: 'ustawa zasiłkowa art. 67',
        },
      ],
      trace: [{ rule: 'martwe_urodzenie', basis: 'Kodeks pracy art. 180¹ § 1', met: true, text: 'Przy martwym urodzeniu przysługuje 8 tygodni po porodzie.' }],
    }),
    benefit({
      id: 'przejecie_przez_ojca',
      status: 'nie_rozstrzygam',
      person: 'drugiRodzic',
      reason: 'Sytuacja matki wymaga potwierdzenia dokumentami. Nie rozstrzygamy, czy ojciec może przejąć zasiłek.',
      links: [{ label: 'ZUS: kontakt', url: 'https://www.zus.pl' }],
    }),
    benefit({
      id: 'ojcowski',
      status: 'przysługuje',
      person: 'drugiRodzic',
      period: { from: '2026-10-08', to: '2026-10-21', days: 14, weeks: 2 },
      rate: 1,
      amount: null,
      payer: 'zus',
      whereToApply: 'zus',
    }),
    benefit({
      id: 'pogrzebowy',
      status: 'przysługuje',
      period: null,
      amount: { daily: 0, total: 7000, basis: 7000, basisSource: 'minimalna' },
      whereToApply: 'zus',
      documents: ['Wniosek Z-12'],
      deadlines: [
        {
          id: 'wniosek_z12',
          type: 'okno',
          date: '2027-10-01',
          text: 'Wniosek Z-12 o zasiłek pogrzebowy złóż w ciągu 12 miesięcy od pochówku.',
          consequence: 'Po tym terminie zasiłek pogrzebowy nie przysługuje.',
          basis: 'Poradnik ZUS, s. 35',
        },
      ],
      notes: ['Zasiłek pogrzebowy wynosi 7 000 zł.'],
    }),
  ],
  missingFacts: [],
  nextStep: { deadlineId: 'wniosek_zam', benefitId: 'macierzynski' },
  risks: [{ id: 'przedawnienie', text: 'Roszczenie o zasiłek przedawnia się po 6 miesiącach od ostatniego dnia okresu zasiłku.', basis: 'ustawa zasiłkowa art. 67' }],
}

export const examples: AssessmentResult[] = [przysluguje, bezKwoty, odmowa, brakDanych, trudna]
