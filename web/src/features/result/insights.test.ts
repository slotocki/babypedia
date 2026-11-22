import { describe, expect, it } from 'vitest'
import type { BenefitResult, Deadline, TraceStep } from '@/app/api/types'
import { conditionOf, explainLabel, keySteps, linksFor, mergeDeadlines, ownShareOf, rateOnlyNote, riskDuplicatesDeadline, shownElsewhere } from './insights'

const benefit = (over: Partial<BenefitResult> = {}): BenefitResult => ({
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

const step = (rule: string, met: boolean | null): TraceStep => ({ rule, basis: '', met, text: rule })

describe('uwagi silnika na karcie świadczenia', () => {
  it('conditionOf wyciąga warunek bez przedrostka i kropki', () => {
    const b = benefit({ notes: ['Świadczenie warunkowe: dotyczy sytuacji, gdy lekarz wystawi Ci zwolnienie lekarskie (L4) w okresie ciąży.'] })
    expect(conditionOf(b)).toBe('gdy lekarz wystawi Ci zwolnienie lekarskie (L4) w okresie ciąży')
    expect(conditionOf(benefit())).toBeNull()
  })

  it('ownShareOf czyta okres i liczbę dni z uwagi o części urlopu rodzicielskiego', () => {
    const note = 'Kwota obejmuje 01.06.2027–30.09.2027 (122 dni) — Twoją część urlopu.'
    expect(ownShareOf(benefit({ notes: ['inna', note] }))).toEqual({ share: { from: '2027-06-01', to: '2027-09-30', days: 122 }, note })
  })

  it('rateOnlyNote tylko wtedy, gdy nie ma okresu ani kwoty', () => {
    const note = 'Stawka: 100% podstawy. Kwotę policzymy, gdy będzie znany okres zasiłku.'
    expect(rateOnlyNote(benefit({ notes: [note] }))).toBe(note)
    expect(rateOnlyNote(benefit({ notes: [note], period: { from: '2027-03-15', to: '2027-08-01', days: 140, weeks: 20 } }))).toBeNull()
  })

  it('shownElsewhere ukrywa uwagi pokazane w innym miejscu karty', () => {
    const notes = ['Świadczenie warunkowe: gdy X.', 'Pokazany jest łączny wymiar obojga rodziców.', 'Zwykła uwaga.']
    expect([...shownElsewhere(benefit({ notes }))]).toEqual(notes.slice(0, 2))
  })
})

describe('linksFor', () => {
  const links = [
    { label: 'Urlop rodzicielski', url: 'https://www.gov.pl/web/rodzina/urlop' },
    { label: 'PIP', url: 'https://www.pip.gov.pl/porady' },
    { label: 'ZUS', url: 'https://www.zus.pl/swiadczenia' },
  ]
  it('w trudnej sytuacji usuwa linki o rodzicielstwie', () => {
    expect(linksFor(links, true).map((l) => l.label)).toEqual(['ZUS'])
    expect(linksFor(links, false)).toBe(links)
  })
})

describe('keySteps', () => {
  it('pokazuje kroki niespełnione i kluczowe, najwyżej 6, w kolejności ścieżki', () => {
    const trace = [
      step('ubezpieczenie_w_dniu_zdarzenia', true),
      step('podstawa_ustalona_przez_platnika', false),
      step('jakis_szczegol', true),
      step('stawka_100', true),
      step('stawka_dzienna', true),
      step('okres_wyczekiwania', null),
      step('macierzynski_okres', true),
      step('kwota_za_okres', true),
      step('zasilek_pogrzebowy', true),
      step('srednia_wynagrodzen_pracownika', true),
    ]
    expect(keySteps(trace).map((s) => s.rule)).toEqual([
      'ubezpieczenie_w_dniu_zdarzenia',
      'stawka_100',
      'okres_wyczekiwania',
      'macierzynski_okres',
      'kwota_za_okres',
      'zasilek_pogrzebowy',
    ])
  })
})

describe('terminy i ryzyka', () => {
  const deadline: Deadline = { id: 'wniosek_usredniona', type: 'najpóźniej_do', date: '2027-04-05', text: 'Złóż wniosek' }

  it('ten sam termin przy dwóch świadczeniach tej samej osoby łączy w jeden', () => {
    const a = benefit({ id: 'macierzynski' })
    const b = benefit({ id: 'rodzicielski' })
    const merged = mergeDeadlines([
      { deadline, benefit: a },
      { deadline, benefit: b },
      { deadline, benefit: benefit({ person: 'drugiRodzic' }) },
    ])
    expect(merged).toHaveLength(2)
    expect(merged[0].benefits).toEqual([a, b])
  })

  it('ryzyko powtarzające termin jest pomijane tylko, gdy termin jest na liście', () => {
    expect(riskDuplicatesDeadline('termin_21_dni', [{ deadline, benefit: benefit() }])).toBe(true)
    expect(riskDuplicatesDeadline('termin_21_dni', [])).toBe(false)
    expect(riskDuplicatesDeadline('inne', [{ deadline, benefit: benefit() }])).toBe(false)
  })

  it('explainLabel rozwija skrót ZAM', () => {
    expect(explainLabel('Pobierz wzór wniosku ZAM')).toBe('Pobierz wzór wniosku o zasiłek macierzyński (ZAM)')
  })
})
