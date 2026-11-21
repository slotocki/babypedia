import { ApiError, api } from '@/app/api/client'
import type { FactSet, LoginResponse, PersonaListItem } from '@/app/api/types'
import { MOCK_PERSONA_PREFIX, MOCK_SESSION_PREFIX, mockFacts, mockLogin, mockPersonas } from './mock'

const notAvailable = (error: unknown) => error instanceof ApiError && (error.status === 501 || error.status === 404)

export async function loadPersonas(): Promise<PersonaListItem[]> {
  try {
    return await api.sim.personas()
  } catch (error) {
    if (notAvailable(error)) return mockPersonas()
    throw error
  }
}

export async function simLogin(personaId: string): Promise<LoginResponse> {
  if (personaId.startsWith(MOCK_PERSONA_PREFIX)) {
    const login = mockLogin(personaId)
    if (login) return login
  }
  return api.sim.login(personaId)
}

export async function simFacts(sessionId: string): Promise<FactSet> {
  if (sessionId.startsWith(MOCK_SESSION_PREFIX)) {
    const facts = mockFacts(sessionId)
    if (facts) return facts
  }
  return api.sim.myFacts(sessionId)
}
