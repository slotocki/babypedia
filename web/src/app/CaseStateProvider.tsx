import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { FactSet } from './api/types'
import { CaseContext, emptyFacts, type CaseState, type DataConnection, type DataSource } from './case-context'

const STORAGE_KEY = 'babyradnik.case'

interface Stored {
  facts: FactSet
  sessionId: string | null
  profileName: string | null
  caseId: string | null
  completedDeadlines: string[]
  connection: DataConnection | null
}

const initial = (): Stored => ({ facts: emptyFacts(), sessionId: null, profileName: null, caseId: null, completedDeadlines: [], connection: null })

const knownSources: DataSource[] = ['zus', 'ezla', 'usc']

function loadConnection(raw: unknown): DataConnection | null {
  if (!raw || typeof raw !== 'object') return null
  const { connectedAt, sources } = raw as Partial<DataConnection>
  if (typeof connectedAt !== 'string' || !Array.isArray(sources)) return null
  return { connectedAt, sources: knownSources.filter((source) => sources.includes(source)) }
}

function load(): Stored {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (raw) {
      const stored = JSON.parse(raw) as Partial<Stored>
      const completed = Array.isArray(stored.completedDeadlines) ? stored.completedDeadlines.filter((x): x is string => typeof x === 'string') : []
      return { ...initial(), ...stored, completedDeadlines: completed, connection: loadConnection(stored.connection) }
    }
  } catch {
    // brak dostępu do sessionStorage
  }
  return initial()
}

export function CaseStateProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<Stored>(load)

  useEffect(() => {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state))
    } catch {
      // brak dostępu do sessionStorage
    }
  }, [state])

  const setCaseFact = useCallback<CaseState['setCaseFact']>((key, fact) => {
    setState((s) => ({ ...s, facts: { ...s.facts, sprawa: { ...s.facts.sprawa, [key]: fact } } }))
  }, [])

  const setPersonFact = useCallback<CaseState['setPersonFact']>((person, key, fact) => {
    setState((s) => ({ ...s, facts: { ...s.facts, [person]: { ...(s.facts[person] ?? {}), [key]: fact } } }))
  }, [])

  const value = useMemo<CaseState>(
    () => ({
      facts: state.facts,
      setFacts: (facts) => setState((s) => ({ ...s, facts })),
      setCaseFact,
      setPersonFact,
      sessionId: state.sessionId,
      profileName: state.profileName,
      connectProfile: (sessionId, profileName, connection) => setState((s) => ({ ...s, sessionId, profileName, connection })),
      caseId: state.caseId,
      replaceCase: (facts, sessionId, caseId, profileName, connection) =>
        setState({ facts, sessionId, caseId, profileName, completedDeadlines: [], connection: connection ?? null }),
      connection: state.connection,
      markRefreshed: () =>
        setState((s) => (s.connection ? { ...s, connection: { ...s.connection, connectedAt: new Date().toISOString() } } : s)),
      completedDeadlines: state.completedDeadlines,
      setDeadlineDone: (id, done) =>
        setState((s) => ({
          ...s,
          completedDeadlines: done ? [...new Set([...s.completedDeadlines, id])] : s.completedDeadlines.filter((x) => x !== id),
        })),
      // Wpis ręczny zaczyna od zera: bez danych, profilu (imię i nazwisko) i połączenia z poprzedniego logowania.
      startManual: () => setState(initial()),
    }),
    [state, setCaseFact, setPersonFact],
  )

  return <CaseContext.Provider value={value}>{children}</CaseContext.Provider>
}
