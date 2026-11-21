import { createContext, useContext } from 'react'
import type { Fact, FactSet, PersonFacts, PersonRef, CaseFacts } from './api/types'

export const emptyFacts = (): FactSet => ({ sprawa: {}, wnioskodawca: {} })

export interface CaseState {
  facts: FactSet
  setFacts: (facts: FactSet) => void
  setCaseFact: <K extends keyof CaseFacts>(key: K, fact: CaseFacts[K] | undefined) => void
  setPersonFact: <K extends keyof PersonFacts>(person: PersonRef, key: K, fact: PersonFacts[K] | undefined) => void
  /** Id sesji symulowanego logowania. Sesja zna tylko id osoby — nigdy PESEL. */
  sessionId: string | null
  profileName: string | null
  connectProfile: (sessionId: string, profileName: string) => void
  caseId: string | null
  replaceCase: (facts: FactSet, sessionId: string, caseId: string | null, profileName: string) => void
  /** Id terminów oznaczonych jako „Zrobione” (wysyłane jako completedDeadlines). */
  completedDeadlines: string[]
  setDeadlineDone: (id: string, done: boolean) => void
  startManual: () => void
}

export const CaseContext = createContext<CaseState | null>(null)

export function useCaseState(): CaseState {
  const ctx = useContext(CaseContext)
  if (!ctx) throw new Error('useCaseState poza CaseStateProvider')
  return ctx
}

export const userFact = <T,>(value: T): Fact<T> => ({ value, source: 'user' })
