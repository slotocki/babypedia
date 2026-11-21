import type { FactSource } from '@/app/api/types'

export const sourceLabels: Record<FactSource, string> = {
  mobywatel: 'mObywatel (symulacja)',
  zus: 'ZUS (symulacja)',
  ezla: 'e-ZLA (symulacja)',
  usc: 'Rejestr stanu cywilnego (symulacja)',
  user: 'Ty',
  assumption: 'Założenie',
}
