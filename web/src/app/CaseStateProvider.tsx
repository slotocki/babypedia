import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { FactSet } from './api/types'
import { CaseContext, emptyFacts, type CaseState } from './case-context'

const STORAGE_KEY = 'babyradnik.case'

interface Stored {
  facts: FactSet
  sessionId: string | null
  caseId: string | null
}

function load(): Stored {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (raw) return JSON.parse(raw) as Stored
  } catch {
    // brak dostępu do sessionStorage
  }
  return { facts: emptyFacts(), sessionId: null, caseId: null }
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
      setSessionId: (sessionId) => setState((s) => ({ ...s, sessionId })),
      caseId: state.caseId,
      setCaseId: (caseId) => setState((s) => ({ ...s, caseId })),
      reset: () => setState({ facts: emptyFacts(), sessionId: null, caseId: null }),
    }),
    [state, setCaseFact, setPersonFact],
  )

  return <CaseContext.Provider value={value}>{children}</CaseContext.Provider>
}
