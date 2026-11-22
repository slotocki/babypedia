import { describe, expect, it } from 'vitest'
import { displayDate, maskDate, outwardDate, parseDate } from './date-text'

describe('parseDate', () => {
  it.each([
    ['15.03.2027', '2027-03-15'],
    ['15032027', '2027-03-15'],
    ['2027-03-15', '2027-03-15'],
    ['5.3.2027', '2027-03-05'],
    [' 15 . 03 . 2027 ', '2027-03-15'],
    ['15/03/2027', '2027-03-15'],
    ['15-03-2027', '2027-03-15'],
    ['20270315', '2027-03-15'],
    ['29.02.2028', '2028-02-29'],
  ])('„%s” → %s', (text, iso) => {
    expect(parseDate(text)).toBe(iso)
  })

  it.each(['31.02.2027', '29.02.2027', '15.13.2027', '00.03.2027', '15.03.1899', '1503', '15.03.27', 'jutro', ''])(
    'odrzuca „%s”',
    (text) => {
      expect(parseDate(text)).toBeNull()
    },
  )
})

describe('displayDate / maskDate / outwardDate', () => {
  it('pokazuje datę ISO po polsku, a inny tekst zostawia', () => {
    expect(displayDate('2027-03-15')).toBe('15.03.2027')
    expect(displayDate('2027-02-31')).toBe('2027-02-31')
    expect(displayDate('15.03')).toBe('15.03')
  })

  it('dostawia kropki w trakcie pisania i ucina nadmiar cyfr', () => {
    expect(maskDate('1')).toBe('1')
    expect(maskDate('150')).toBe('15.0')
    expect(maskDate('1503')).toBe('15.03')
    expect(maskDate('15032')).toBe('15.03.2')
    expect(maskDate('1503202799')).toBe('15.03.2027')
  })

  it('na zewnątrz przekazuje ISO albo tekst do walidacji', () => {
    expect(outwardDate('15.03.2027')).toBe('2027-03-15')
    expect(outwardDate(' 31.02.2027 ')).toBe('31.02.2027')
  })
})
