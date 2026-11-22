import { describe, expect, it } from 'vitest'
import type { InsuranceTitle } from '@/app/api/types'
import {
  checkExtras,
  emptyExtraTitle,
  emptyExtras,
  parseFraction,
  showsLeavePlan,
  showsResignation,
  showsSickLeaves,
  titlesWithExtras,
  type ExtrasAnswers,
  type ExtrasScope,
} from './extrasFacts'

describe('parseFraction', () => {
  it.each([
    ['1', 1],
    ['0,63', 0.63],
    ['0.5', 0.5],
    ['5/8', 0.625],
    [' 1 / 3 ', 0.3333],
  ])('„%s” → %d', (text, expected) => {
    expect(parseFraction(text)).toBe(expected)
  })

  it.each(['0', '1,5', '3/0', '3/2', 'pół', ''])('odrzuca „%s”', (text) => {
    expect(parseFraction(text)).toBeNull()
  })
})

describe('zakres sekcji dodatkowych', () => {
  it('martwe urodzenie wyłącza plan urlopu i rezygnację, ale nie zwolnienia', () => {
    const scope = { rodzaj: 'urodzenie', mother: true, stillborn: true }
    expect(showsSickLeaves(scope)).toBe(true)
    expect(showsLeavePlan(scope)).toBe(false)
    expect(showsResignation(scope)).toBe(false)
  })

  it('ojciec nie widzi zwolnień w ciąży ani rezygnacji', () => {
    const scope = { rodzaj: 'urodzenie', mother: false }
    expect(showsSickLeaves(scope)).toBe(false)
    expect(showsResignation(scope)).toBe(false)
    expect(showsLeavePlan(scope)).toBe(true)
  })
})

describe('titlesWithExtras', () => {
  const main: InsuranceTitle = { rodzajTytulu: 'pracownik', od: '2024-01-01', do: null, chorobowe: 'obowiazkowe', wymiarEtatu: 1 }

  it('dwie umowy o pracę u tego samego pracodawcy mają ten sam płatnik', () => {
    const extras = { ...emptyExtras(), tytulDo: '2026-12-31', tytuly: [{ ...emptyExtraTitle(), forma: 'pracownik', od: '2025-01-01', etat: '1/2', platnik: 'ten_sam' }] }
    const [first, second] = titlesWithExtras(main, extras)
    expect(first).toMatchObject({ do: '2026-12-31', platnikId: 'p1' })
    expect(second).toMatchObject({ rodzajTytulu: 'pracownik', chorobowe: 'obowiazkowe', wymiarEtatu: 0.5, platnikId: 'p1' })
  })

  it('zlecenie obok etatu nie dostaje sztucznego płatnika', () => {
    const extras = { ...emptyExtras(), tytuly: [{ ...emptyExtraTitle(), forma: 'zleceniobiorca', od: '2025-01-01', chorobowe: 'brak' }] }
    const titles = titlesWithExtras(main, extras)
    expect(titles[0].platnikId).toBeUndefined()
    expect(titles[1]).toEqual({ rodzajTytulu: 'zleceniobiorca', od: '2025-01-01', do: null, chorobowe: 'brak' })
  })
})

describe('checkExtras', () => {
  const issues = (extras: ExtrasAnswers, scope: ExtrasScope = { rodzaj: 'ciaza', mother: true }) => {
    const found: Record<string, string> = {}
    checkExtras(extras, 'x', '2024-01-01', 'pracownik', scope, (path, message) => {
      found[path] = message
    })
    return found
  }

  it('puste sekcje są poprawne', () => {
    expect(issues(emptyExtras())).toEqual({})
  })

  it('koniec przed początkiem i brak daty końca zwolnienia', () => {
    const found = issues({ ...emptyExtras(), tytulDo: '2023-01-01', zwolnienia: [{ od: '2027-01-10', do: '', kodB: true, szpital: false }] })
    expect(found['x.tytulDo']).toMatch(/nie może być wcześniejsza/)
    expect(found['x.zwolnienia.0.do']).toMatch(/^Podaj datę końca zwolnienia/)
  })

  it('liczba tygodni planu urlopu poza zakresem', () => {
    const found = issues({ ...emptyExtras(), czesci: [{ tygodnie: '60', od: '' }] })
    expect(found['x.czesci.0.tygodnie']).toBe('Wpisz liczbę tygodni od 1 do 52.')
  })
})
