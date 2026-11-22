import { createContext, useContext } from 'react'
import type { Fact, FactSet, PersonFacts, PersonRef, CaseFacts } from './api/types'

export const emptyFacts = (): FactSet => ({ sprawa: {}, wnioskodawca: {} })

/** Źródła, z których użytkownik zgodził się pobrać dane (symulacja). */
export type DataSource = 'zus' | 'ezla' | 'usc'

export interface DataConnection {
  /** Chwila połączenia lub ostatniego odświeżenia (ISO). */
  connectedAt: string
  sources: DataSource[]
}

export interface CaseState {
  facts: FactSet
  setFacts: (facts: FactSet) => void
  setCaseFact: <K extends keyof CaseFacts>(key: K, fact: CaseFacts[K] | undefined) => void
  setPersonFact: <K extends keyof PersonFacts>(person: PersonRef, key: K, fact: PersonFacts[K] | undefined) => void
  /** Id sesji symulowanego logowania. Sesja zna tylko id osoby — nigdy PESEL. */
  sessionId: string | null
  profileName: string | null
  connectProfile: (sessionId: string, profileName: string, connection: DataConnection) => void
  caseId: string | null
  replaceCase: (facts: FactSet, sessionId: string, caseId: string | null, profileName: string, connection?: DataConnection) => void
  /** Symulowane połączenie z PUE/eZUS i innymi rejestrami; null bez logowania. */
  connection: DataConnection | null
  /** Zapisuje nową godzinę pobrania po odświeżeniu danych. */
  markRefreshed: () => void
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
