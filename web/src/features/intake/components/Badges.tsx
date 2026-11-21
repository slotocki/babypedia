import type { FactSource } from '@/app/api/types'
import { sourceLabels } from './source-labels'

export function SimulationBadge({ className = '' }: { className?: string }) {
  return (
    <span
      className={`inline-block rounded border border-amber-700 bg-amber-50 px-2 py-0.5 text-sm font-semibold text-amber-900 ${className}`}
    >
      Symulacja
    </span>
  )
}

export function SourceBadge({ source }: { source: FactSource }) {
  return (
    <span className="inline-block rounded border border-slate-500 bg-slate-50 px-2 py-0.5 text-sm text-slate-800">
      Źródło: {sourceLabels[source]}
    </span>
  )
}
