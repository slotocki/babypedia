import { createContext, useContext } from 'react'

export interface AdminSession {
  password: string
  lock: () => void
}

export const AdminSessionContext = createContext<AdminSession | null>(null)

export function useAdminSession(): AdminSession {
  const session = useContext(AdminSessionContext)
  if (!session) throw new Error('Brak sesji administratora')
  return session
}
