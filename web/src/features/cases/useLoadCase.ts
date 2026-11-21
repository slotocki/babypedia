import { useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/app/api/client'
import type { CaseSummary } from '@/app/api/types'
import { emptyFacts, useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { mergeIntegrationFacts, userFactOf, withFact } from '@/features/review/facts'

export type LoadCaseState =
  | { status: 'idle' }
  | { status: 'loading'; caseId: string }
  | { status: 'error'; caseId: string; error: unknown; item: CaseSummary }

export function useLoadCase() {
  const { setFacts, setSessionId, setCaseId } = useCaseState()
  const navigate = useNavigate()
  const [state, setState] = useState<LoadCaseState>({ status: 'idle' })

  const load = async (item: CaseSummary) => {
    if (!item.persona || !item.dostepna) return
    setState({ status: 'loading', caseId: item.id })
    try {
      const session = await api.sim.login(item.persona)
      const integration = await api.sim.myFacts(session.sessionId)
      setSessionId(session.sessionId)
      // Sprawa testowa zastępuje wcześniejsze odpowiedzi w całości — to komplet danych z dokumentów jednej osoby.
      const loaded = mergeIntegrationFacts(emptyFacts(), integration)
      setFacts(loaded.sprawa['zdarzenie.dlaKogo'] ? loaded : withFact(loaded, 'sprawa', 'zdarzenie.dlaKogo', userFactOf('ja')))
      setCaseId(item.id)
      navigate(paths.review)
    } catch (error) {
      setState({ status: 'error', caseId: item.id, error, item })
    }
  }

  return { state, load, busy: state.status === 'loading' }
}
