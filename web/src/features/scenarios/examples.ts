import type { AssessmentResult, BenefitId, BenefitResult, BenefitStatus, CompareResult, Deadline, PersonRef, Risk, ScenarioChange } from '@/app/api/types'

export interface DevExample {
  change: ScenarioChange
  result: CompareResult
}

const day = (iso: string, n: number) => {
  const d = new Date(`${iso}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + n - 1)
  return d.toISOString().slice(0, 10)
}

function benefit(
  id: BenefitId,
  status: BenefitStatus,
  opts: { from?: string; weeks?: number; basis?: number; rate?: number; person?: PersonRef; deadlines?: Deadline[]; reason?: string } = {},
): BenefitResult {
  const { from, weeks, basis = 6000, rate = 1, person = 'wnioskodawca', deadlines = [], reason } = opts
  const days = (weeks ?? 0) * 7
  const hasPeriod = status === 'przysługuje' && from && weeks
  const daily = Math.round((basis / 30) * rate * 100) / 100
  return {
    id,
    person,
    status,
    period: hasPeriod ? { from, to: day(from, days), days, weeks } : null,
    rate: hasPeriod ? rate : null,
    amount: hasPeriod ? { daily, total: Math.round(daily * days * 100) / 100, basis, basisSource: 'szacunek_zus' } : null,
    parts: [],
    payer: hasPeriod ? 'płatnik_składek' : null,
    whereToApply: hasPeriod ? 'pracodawca' : null,
    documents: [],
    deadlines,
    reason: reason ?? null,
    legalBasis: null,
    whatWouldChange: null,
    notes: [],
    links: [],
    trace: [],
  }
}

const assessment = (benefits: BenefitResult[], risks: Risk[] = [], validationErrors: AssessmentResult['validationErrors'] = []): AssessmentResult => ({
  rulesetVersion: 'przyklad-dev',
  benefits,
  missingFacts: [],
  validationErrors,
  nextStep: null,
  risks,
  difficultSituation: false,
})

const wniosek = (date: string, rodzaj: string): Deadline => ({
  id: `wniosek_o_urlop_${rodzaj}`,
  type: 'najpóźniej_do',
  date,
  text: `Złóż wniosek o urlop ${rodzaj === 'macierzynski' ? 'macierzyński' : 'rodzicielski'} u pracodawcy`,
  consequence: 'Bez wniosku pracodawca nie udzieli urlopu w tym terminie.',
  basis: null,
})

const statusChange: DevExample = {
  change: { id: 'odchodze_z_pracy', params: { data: '2026-12-31' } },
  result: {
    base: assessment([
      benefit('macierzynski', 'przysługuje', { from: '2027-03-15', weeks: 20, deadlines: [wniosek('2027-02-15', 'macierzynski')] }),
      benefit('rodzicielski', 'przysługuje', { from: '2027-08-02', weeks: 41, rate: 0.7 }),
      benefit('ojcowski', 'nie_dotyczy'),
    ]),
    variant: assessment(
      [
        benefit('macierzynski', 'nie_przysługuje', { reason: 'Ubezpieczenie chorobowe skończy się przed porodem, a od jego ustania minie więcej niż 30 dni.' }),
        benefit('rodzicielski', 'nie_przysługuje', { reason: 'Rodzicielski przysługuje po macierzyńskim.' }),
        benefit('ojcowski', 'nie_dotyczy'),
      ],
      [
        {
          id: 'przerwa_w_ubezpieczeniu',
          text: 'Jeśli ubezpieczenie chorobowe skończy się przed porodem, zasiłek macierzyński może Ci nie przysługiwać. Zapytaj ZUS o swoją sytuację.',
          basis: 'ustawa art. 30',
          link: { label: 'ZUS', url: 'https://www.zus.pl' },
        },
      ],
    ),
    diff: [
      { id: 'macierzynski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'nie_przysługuje', weeksDelta: -20, amountDelta: -28000 },
      { id: 'rodzicielski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'nie_przysługuje', weeksDelta: -41, amountDelta: -40180 },
      { id: 'ojcowski', person: 'wnioskodawca', statusBase: 'nie_dotyczy', statusVariant: 'nie_dotyczy', weeksDelta: null, amountDelta: null },
    ],
    validationErrors: [],
  },
}

const partTime: DevExample = {
  change: { id: 'praca_na_czesc_etatu', params: { wymiar: 0.5, tygodnie: 20 } },
  result: {
    base: assessment([
      benefit('macierzynski', 'przysługuje', { from: '2027-03-15', weeks: 20 }),
      benefit('rodzicielski', 'przysługuje', { from: '2027-08-02', weeks: 41, rate: 0.7, deadlines: [wniosek('2027-07-02', 'rodzicielski')] }),
    ]),
    variant: assessment(
      [
        benefit('macierzynski', 'przysługuje', { from: '2027-03-15', weeks: 20 }),
        benefit('rodzicielski', 'przysługuje', { from: '2027-08-02', weeks: 51, rate: 0.7, deadlines: [wniosek('2027-07-16', 'rodzicielski')] }),
      ],
      [
        {
          id: 'praca_na_rodzicielskim',
          text: 'Za dni pracy na część etatu zasiłek rodzicielski jest niższy o wymiar etatu. Ustal z pracodawcą na piśmie wymiar i okres pracy.',
          basis: 'ustawa art. 31 ust. 4a',
          link: null,
        },
      ],
    ),
    diff: [
      { id: 'macierzynski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'przysługuje', weeksDelta: 0, amountDelta: 0 },
      { id: 'rodzicielski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'przysługuje', weeksDelta: 10, amountDelta: -3240 },
    ],
    validationErrors: [],
  },
}

const nineWeeks: DevExample = {
  change: { id: 'drugi_rodzic_bierze', person: 'drugiRodzic', params: { tygodnie: 33 } },
  result: {
    base: assessment([benefit('rodzicielski', 'przysługuje', { from: '2027-08-02', weeks: 41, rate: 0.7 })]),
    variant: assessment([benefit('rodzicielski', 'przysługuje', { from: '2027-08-02', weeks: 41, rate: 0.7 })]),
    diff: [{ id: 'rodzicielski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'przysługuje', weeksDelta: 0, amountDelta: 0 }],
    validationErrors: [
      {
        code: 'czesc_nieprzenoszalna',
        message: 'Drugi rodzic bierze 33 z 41 tygodni, więc dla Ciebie zostaje 8 tygodni. Każdy rodzic ma 9 tygodni urlopu rodzicielskiego wyłącznie dla siebie.',
        fact: 'plan.rodzicielskiCzesci',
        person: 'drugiRodzic',
      },
    ],
  },
}

export const devExamples: Record<string, DevExample> = { '1': statusChange, '2': partTime, '3': nineWeeks }
