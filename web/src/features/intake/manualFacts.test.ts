import { describe, expect, it } from 'vitest'
import type { FactSet } from '@/app/api/types'
import { getFact } from '@/features/review/facts'
import { emptyExtras, emptySickLeave } from './extrasFacts'
import { applyManualFacts, contributionMonths, parseAmount, shiftMonth, workAnswersFrom, workInputFrom, type ManualInput } from './manualFacts'

const empty = (): FactSet => ({ sprawa: {}, wnioskodawca: {} }) as FactSet
const value = (facts: FactSet, person: 'wnioskodawca' | 'drugiRodzic', key: string) => getFact(facts, person, key)?.value

const anna: ManualInput = {
  today: '2026-11-22',
  eventDate: '2027-03-15',
  rola: 'matka',
  rodzaj: 'ciaza',
  ja: { forma: 'pracownik', od: '2024-01-01', czasOkreslony: false, wymiarEtatu: 1, kwota: 6500, ponad20: true },
}

describe('shiftMonth / parseAmount', () => {
  it('przesuwa miesiące przez granicę roku', () => {
    expect(shiftMonth('2027-01', -1)).toBe('2026-12')
    expect(shiftMonth('2026-11', 14)).toBe('2028-01')
    expect(shiftMonth('2027-03', -14)).toBe('2026-01')
  })

  it('czyta kwoty wpisane po polsku', () => {
    expect(parseAmount('6 500,50')).toBe(6500.5)
    expect(parseAmount('6500')).toBe(6500)
    expect(parseAmount('6.500,00')).toBeNull()
    expect(parseAmount('')).toBeNull()
  })
})

describe('contributionMonths', () => {
  it('bierze 14 miesięcy przed wcześniejszym z: dziś, data zdarzenia', () => {
    const months = contributionMonths('2020-01-01', 5000, '2026-11-22', '2027-03-15')
    expect(months).toHaveLength(14)
    expect(months[0].miesiac).toBe('2025-09')
    expect(months.at(-1)?.miesiac).toBe('2026-10')
    expect(months.every((m) => m.kwota === 5000)).toBe(true)
  })

  it('liczy od pierwszego pełnego miesiąca ubezpieczenia', () => {
    const months = contributionMonths('2026-06-15', 5000, '2026-11-22')
    expect(months.map((m) => m.miesiac)).toEqual(['2026-07', '2026-08', '2026-09', '2026-10'])
  })

  it('przy zdarzeniu z przeszłości liczy przed zdarzeniem', () => {
    const months = contributionMonths('2020-01-01', 5000, '2026-11-22', '2025-05-10')
    expect(months.at(-1)?.miesiac).toBe('2025-04')
  })
})

describe('applyManualFacts', () => {
  it('umowa o pracę: tytuł z obowiązkowym chorobowym, podstawy i liczba ubezpieczonych', () => {
    const facts = applyManualFacts(empty(), anna)
    expect(value(facts, 'wnioskodawca', 'osoba.rola')).toBe('matka')
    expect(value(facts, 'wnioskodawca', 'ubezpieczenie.tytuly')).toEqual([
      { rodzajTytulu: 'pracownik', od: '2024-01-01', do: null, chorobowe: 'obowiazkowe', wymiarEtatu: 1 },
    ])
    expect(value(facts, 'wnioskodawca', 'ubezpieczenie.podstawySkladek')).toHaveLength(14)
    expect(value(facts, 'wnioskodawca', 'umowa.naCzasOkreslony')).toBe(false)
    expect(value(facts, 'wnioskodawca', 'platnik.liczbaUbezpieczonych')).toBe(21)
    expect(getFact(facts, 'wnioskodawca', 'ubezpieczenie.tytuly')?.source).toBe('user')
  })

  it('brak pracy: pusta lista tytułów i brak podstaw', () => {
    const facts = applyManualFacts(empty(), { ...anna, ja: { forma: 'brak' } })
    expect(value(facts, 'wnioskodawca', 'ubezpieczenie.tytuly')).toEqual([])
    expect(getFact(facts, 'wnioskodawca', 'ubezpieczenie.podstawySkladek')).toBeUndefined()
  })

  it('działalność: chorobowe dobrowolne i saldo zadłużenia na dziś, bez faktów umowy', () => {
    const facts = applyManualFacts(empty(), { ...anna, ja: { forma: 'dzialalnosc', od: '2023-05-01', chorobowe: true, kwota: 4000, zalegleSkladki: 0 } })
    expect(value(facts, 'wnioskodawca', 'ubezpieczenie.tytuly')).toMatchObject([{ rodzajTytulu: 'dzialalnosc', chorobowe: 'dobrowolne' }])
    expect(value(facts, 'wnioskodawca', 'skladki.zadluzenie')).toEqual({ naDzien: '2026-11-22', kwota: 0 })
    expect(getFact(facts, 'wnioskodawca', 'umowa.naCzasOkreslony')).toBeUndefined()
  })

  it('„nie wiem” przy liczbie ubezpieczonych nie ustawia faktu', () => {
    const facts = applyManualFacts(empty(), { ...anna, ja: { ...anna.ja, ponad20: null } })
    expect(getFact(facts, 'wnioskodawca', 'platnik.liczbaUbezpieczonych')).toBeUndefined()
  })

  it('drugi rodzic dostaje przeciwną rolę, a po wyłączeniu jego fakty użytkownika znikają', () => {
    const both = applyManualFacts(empty(), { ...anna, drugi: { forma: 'pracownik', od: '2022-01-01', kwota: 7000 } })
    expect(value(both, 'drugiRodzic', 'osoba.rola')).toBe('ojciec')
    const alone = applyManualFacts(both, anna)
    expect(alone.drugiRodzic).toBeUndefined()
  })

  it('martwe urodzenie: plan urlopu rodzicielskiego i rezygnacja nie są zapisywane', () => {
    const extras = {
      ...emptyExtras(),
      czesci: [{ tygodnie: '10', od: '' }],
      rezygnacja: true,
      rezygnacjaPoTyg: '14',
      zwolnienia: [{ ...emptySickLeave(), od: '2027-01-10', do: '2027-01-20' }],
    }
    const facts = applyManualFacts(empty(), { ...anna, rodzaj: 'urodzenie', stillborn: true, ja: { ...anna.ja, extras } })
    expect(getFact(facts, 'wnioskodawca', 'plan.rodzicielskiCzesci')).toBeUndefined()
    expect(getFact(facts, 'wnioskodawca', 'plan.rezygnacjaZMacierzynskiegoPoTyg')).toBeUndefined()
    expect(value(facts, 'wnioskodawca', 'ezla.zwolnienia')).toEqual([{ od: '2027-01-10', do: '2027-01-20', kod: 'B', szpital: false }])
  })
})

describe('workAnswersFrom ↔ workInputFrom', () => {
  it('odpowiedzi odtworzone z faktów dają z powrotem te same fakty', () => {
    const input = { ...anna, ja: { ...anna.ja, czasOkreslony: true, umowaDo: '2027-06-30', wymiarEtatu: 0.5, kwota: 6500.5, extras: emptyExtras() } }
    const facts = applyManualFacts(empty(), input)
    const answers = workAnswersFrom(facts, 'wnioskodawca')
    expect(answers).toMatchObject({ forma: 'pracownik', od: '2024-01-01', czasOkreslony: 'tak', umowaDo: '2027-06-30', etat: '0,5', kwota: '6500,5', ponad20: 'tak' })
    const again = applyManualFacts(empty(), { ...input, ja: workInputFrom(answers) })
    expect(again).toEqual(facts)
  })
})
