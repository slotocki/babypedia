import { describe, expect, it } from 'vitest'
import { addDaysIso, eventDateIssue, isValidIsoDate } from './dates'

const today = '2026-11-22'

describe('dates', () => {
  it('isValidIsoDate sprawdza istnienie dnia i rok od 1900', () => {
    expect(isValidIsoDate('2028-02-29')).toBe(true)
    expect(isValidIsoDate('2027-02-29')).toBe(false)
    expect(isValidIsoDate('1899-12-31')).toBe(false)
    expect(isValidIsoDate('15.03.2027')).toBe(false)
  })

  it('addDaysIso przechodzi przez koniec miesiąca i roku', () => {
    expect(addDaysIso('2026-12-31', 1)).toBe('2027-01-01')
    expect(addDaysIso('2027-03-01', -1)).toBe('2027-02-28')
  })

  it('eventDateIssue: data urodzenia z przyszłości, termin porodu za daleko, plan z przeszłości', () => {
    expect(eventDateIssue('urodzenie', '2026-11-23', today)).toMatch(/nie może być z przyszłości/)
    expect(eventDateIssue('urodzenie', '2026-11-22', today)).toBeNull()
    expect(eventDateIssue('ciaza', '2027-03-15', today)).toBeNull()
    expect(eventDateIssue('ciaza', '2027-12-01', today)).toMatch(/zbyt odległa/)
    expect(eventDateIssue('ciaza', '2020-01-01', today)).toMatch(/Dziecko się urodziło/)
    expect(eventDateIssue('planuje', '2026-11-21', today)).toMatch(/już minęła/)
  })
})
