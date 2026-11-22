import { describe, expect, it } from 'vitest'
import { formatDate, formatDateShort, formatDays, formatMoney, formatWeeks, parseMoney, pluralize } from './format'

// Intl wstawia twarde spacje (U+00A0 / U+202F) — porównujemy po zamianie na zwykłe.
const plain = (text: string) => text.replace(/\s/g, ' ')

describe('format', () => {
  it('kwoty po polsku z separatorem tysięcy, także dla 4 cyfr', () => {
    expect(plain(formatMoney(1234.5))).toBe('1 234,50 zł')
    expect(plain(formatMoney(6500))).toBe('6 500,00 zł')
    expect(plain(formatMoney(0))).toBe('0,00 zł')
  })

  it('daty bez przesunięcia strefy czasowej', () => {
    expect(formatDate('2027-03-15')).toBe('15 marca 2027')
    expect(formatDateShort('2027-01-01')).toBe('01.01.2027')
    expect(formatDate('2027-12-31T23:30:00Z')).toBe('31 grudnia 2027')
  })

  it.each([
    [1, 'tydzień'],
    [2, 'tygodnie'],
    [5, 'tygodni'],
    [12, 'tygodni'],
    [22, 'tygodnie'],
    [114, 'tygodni'],
  ])('odmiana: %i %s', (n, word) => {
    expect(pluralize(n, 'tydzień', 'tygodnie', 'tygodni')).toBe(word)
  })

  it('tygodnie i dni z liczbą', () => {
    expect(formatWeeks(20)).toBe('20 tygodni')
    expect(formatWeeks(41)).toBe('41 tygodni')
    expect(formatDays(1)).toBe('1 dzień')
    expect(formatDays(3)).toBe('3 dni')
  })

  it('parseMoney przyjmuje zapis z przecinkiem, spacjami i „zł”', () => {
    expect(parseMoney('6 500,50')).toBe(6500.5)
    expect(parseMoney('4806 zł')).toBe(4806)
    expect(parseMoney('12,345')).toBeNull()
    expect(parseMoney('-100')).toBeNull()
    expect(parseMoney('abc')).toBeNull()
  })
})
