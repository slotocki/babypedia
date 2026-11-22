import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { clearWizardDraft, loadWizardDraft, saveWizardDraft } from './wizardDraft'

function memoryStorage(): Storage {
  const data = new Map<string, string>()
  return {
    get length() {
      return data.size
    },
    clear: () => data.clear(),
    getItem: (key) => data.get(key) ?? null,
    key: (index) => [...data.keys()][index] ?? null,
    removeItem: (key) => {
      data.delete(key)
    },
    setItem: (key, value) => {
      data.set(key, String(value))
    },
  }
}

describe('wizardDraft', () => {
  beforeEach(() => {
    vi.stubGlobal('sessionStorage', memoryStorage())
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  const fromFacts = { zdarzenie: { rodzaj: 'ciaza', data: '2027-03-15' }, praca: { forma: 'pracownik', kwota: '' }, lista: [1] }

  it('nakłada szkic na wartości z faktów, scalając obiekty jeden poziom w głąb', () => {
    saveWizardDraft('sesja-1', { praca: { kwota: '6500' }, lista: [2, 3] })
    expect(loadWizardDraft('sesja-1', fromFacts)).toEqual({
      zdarzenie: { rodzaj: 'ciaza', data: '2027-03-15' },
      praca: { forma: 'pracownik', kwota: '6500' },
      lista: [2, 3],
    })
  })

  it('pomija szkic innej sesji i szkic usunięty', () => {
    saveWizardDraft('sesja-1', { praca: { kwota: '6500' } })
    expect(loadWizardDraft('sesja-2', fromFacts)).toBe(fromFacts)
    clearWizardDraft()
    expect(loadWizardDraft('sesja-1', fromFacts)).toBe(fromFacts)
  })

  it('uszkodzony zapis albo brak sessionStorage nie psuje kreatora', () => {
    sessionStorage.setItem('babyradnik.wpis', '{nie json')
    expect(loadWizardDraft(null, fromFacts)).toBe(fromFacts)
    vi.stubGlobal('sessionStorage', undefined)
    expect(() => saveWizardDraft(null, {})).not.toThrow()
    expect(loadWizardDraft(null, fromFacts)).toBe(fromFacts)
  })
})
