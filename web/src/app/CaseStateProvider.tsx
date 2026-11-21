import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { FactSet } from './api/types'
import { CaseContext, emptyFacts, type CaseState } from './case-context'

const STORAGE_KEY = 'babyradnik.case'

interface Stored {
  facts: FactSet
  sessionId: string | null
  caseId: string | null
  completedDeadlines: string[]
}

const initial = (): Stored => ({ facts: emptyFacts(), sessionId: null, caseId: null, completedDeadlines: [] })

function load(): Stored {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (raw) {
      const stored = JSON.parse(raw) as Partial<Stored>
      const completed = Array.isArray(stored.completedDeadlines) ? stored.completedDeadlines.filter((x): x is string => typeof x === 'string') : []
      return { ...initial(), ...stored, completedDeadlines: completed }
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
      setSessionId: (sessionId) => setState((s) => ({ ...s, sessionId })),
      caseId: state.caseId,
      setCaseId: (caseId) => setState((s) => ({ ...s, caseId })),
      completedDeadlines: state.completedDeadlines,
      setDeadlineDone: (id, done) =>
        setState((s) => ({
          ...s,
          completedDeadlines: done ? [...new Set([...s.completedDeadlines, id])] : s.completedDeadlines.filter((x) => x !== id),
        })),
      reset: () => setState(initial()),
    }),
    [state, setCaseFact, setPersonFact],
  )

  return <CaseContext.Provider value={value}>{children}</CaseContext.Provider>
}
