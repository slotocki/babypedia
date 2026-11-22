import { describe, expect, it } from 'vitest'
import { answerBlocks, sourceLabel } from './answer-text'

describe('answerBlocks', () => {
  it('łączy kolejne wiersze „- ” w jedną listę, a pozostałe w akapit', () => {
    const text = 'Potrzebujesz:\n- wniosku ZAM\n- skróconego aktu urodzenia\nZłóż je u pracodawcy.\n\nDrugi akapit.'
    expect(answerBlocks(text)).toEqual([
      { list: false, lines: ['Potrzebujesz:'] },
      { list: true, lines: ['wniosku ZAM', 'skróconego aktu urodzenia'] },
      { list: false, lines: ['Złóż je u pracodawcy.'] },
      { list: false, lines: ['Drugi akapit.'] },
    ])
  })

  it('pusta linia rozdziela dwie listy; myślnik bez spacji to zwykły tekst', () => {
    expect(answerBlocks('- a\n\n- b\n-c')).toEqual([
      { list: true, lines: ['a'] },
      { list: true, lines: ['b'] },
      { list: false, lines: ['-c'] },
    ])
  })

  it('pusty tekst nie daje bloków', () => {
    expect(answerBlocks('\n \n')).toEqual([])
  })
})

describe('sourceLabel', () => {
  it('skraca nazwy dokumentów i dopisuje artykuł albo paragraf', () => {
    expect(sourceLabel({ document: 'Ustawa zasiłkowa', article: 'Art. 29' })).toBe('Ustawa zasiłkowa art. 29')
    expect(sourceLabel({ document: 'Rozporządzenie w sprawie dokumentów do zasiłków', article: '§ 18' })).toBe('Rozporządzenie (dokumenty) § 18')
    expect(sourceLabel({ document: 'Poradnik ZUS (materiał pomocniczy)', article: 'Rozdział 3' })).toBe('Poradnik ZUS')
  })
})
