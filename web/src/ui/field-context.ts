import { createContext, useContext } from 'react'

export interface FieldControl {
  id?: string
  'aria-describedby'?: string
  'aria-invalid'?: true
}

export const FieldContext = createContext<FieldControl>({})

export function useFieldControl(): FieldControl {
  return useContext(FieldContext)
}
