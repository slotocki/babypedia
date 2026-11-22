import { describe, expect, it } from 'vitest'
import type { AssessmentResult, BenefitResult, CompareResult } from '@/app/api/types'
import { analyze, describeChange, signedMoney, signedWeeks, summarize } from './diff'

const plain = (text: string | null) => text?.replace(/\s/g, ' ') ?? null

const benefit = (over: Partial<BenefitResult>): BenefitResult => ({
  id: 'macierzynski',
  person: 'wnioskodawca',
  status: 'przysługuje',
  parts: [],
  documents: [],
  deadlines: [],
  notes: [],
  trace: [],
  links: [],
  ...over,
}) as BenefitResult

const assessment = (benefits: BenefitResult[], risks: AssessmentResult['risks'] = []): AssessmentResult => ({
  rulesetVersion: 'test',
  benefits,
  missingFacts: [],
  risks,
})

const period = (from: string, to: string, weeks: number) => ({ from, to, days: weeks * 7, weeks })
const amount = (total: number) => ({ daily: 200, total, basis: 6000, basisSource: 'szacunek_zus' as const })

describe('analyze', () => {
  it('rozdziela zmienione i niezmienione świadczenia, sumuje różnice kwot, znajduje nowe ryzyka i terminy', () => {
    const result: CompareResult = {
      base: assessment(
        [
          benefit({ id: 'macierzynski', period: period('2027-03-15', '2027-08-01', 20), amount: amount(30000), deadlines: [{ id: 'zam', type: 'najpóźniej_do', date: '2027-03-01', text: 'ZAM' }] }),
          benefit({ id: 'rodzicielski', period: period('2027-08-02', '2028-05-01', 41), amount: amount(40000) }),
          benefit({ id: 'pogrzebowy', status: 'nie_dotyczy' }),
        ],
        [{ id: 'stare', text: 'Stare ryzyko' }],
      ),
      variant: assessment(
        [
          benefit({ id: 'macierzynski', period: period('2027-03-15', '2027-08-01', 20), amount: amount(30000), deadlines: [{ id: 'zam', type: 'najpóźniej_do', date: '2027-03-08', text: 'ZAM' }] }),
          benefit({ id: 'rodzicielski', status: 'nie_przysługuje', deadlines: [{ id: 'nowy', type: 'okno', date: '2027-09-01', text: 'Nowy' }] }),
          benefit({ id: 'pogrzebowy', status: 'nie_dotyczy' }),
        ],
        [{ id: 'stare', text: 'Stare ryzyko' }, { id: 'nowe', text: 'Nowe ryzyko' }],
      ),
      diff: [
        { id: 'macierzynski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'przysługuje', weeksDelta: 0, amountDelta: 0 },
        { id: 'rodzicielski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'nie_przysługuje', weeksDelta: -41, amountDelta: -40000 },
      ],
      validationErrors: [],
    }
    const a = analyze(result)
    expect(a.changed.map((c) => c.id)).toEqual(['rodzicielski'])
    expect(a.unchanged.map((c) => c.id)).toEqual(['macierzynski'])
    expect(a.totalAmountDelta).toBe(-40000)
    expect(a.newRisks.map((r) => r.id)).toEqual(['nowe'])
    expect(a.deadlines.map((d) => [d.id, d.kind])).toEqual([['zam', 'przesuniety'], ['nowy', 'nowy']])
    expect(a.both).toBe(false)
    expect(plain(describeChange(a.changed[0], a.both))).toBe('Urlop rodzicielski: przysługuje → nie przysługuje, −41 tygodni (mniej), −40 000,00 zł (mniej)')
    expect(summarize(result, a)).toBe('W tym wariancie zmienia się status: Urlop rodzicielski — nie przysługuje.')
  })

  it('gdy różnica kwot jest w granicach 1%, mówi „prawie tyle samo”', () => {
    const result: CompareResult = {
      base: assessment([benefit({ amount: amount(30000) })]),
      variant: assessment([benefit({ amount: amount(30100) })]),
      diff: [{ id: 'macierzynski', person: 'wnioskodawca', statusBase: 'przysługuje', statusVariant: 'przysługuje', weeksDelta: 0, amountDelta: 100 }],
      validationErrors: [],
    }
    expect(plain(summarize(result, analyze(result), 'wariant_wyplaty'))).toBe(
      'Oba warianty dają prawie tyle samo (różnica 100,00 zł); uśredniony wypłaca równo przez cały czas.',
    )
  })
})

describe('signedWeeks / signedMoney', () => {
  it('znak, odmiana i słowo „więcej/mniej”', () => {
    expect(signedWeeks(0)).toBe('0 tygodni')
    expect(signedWeeks(1)).toBe('+1 tydzień (więcej)')
    expect(signedWeeks(-2)).toBe('−2 tygodnie (mniej)')
    expect(plain(signedMoney(1234.5))).toBe('+1 234,50 zł (więcej)')
  })
})
