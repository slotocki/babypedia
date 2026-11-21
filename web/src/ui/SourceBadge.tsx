import type { ComponentType } from 'react'
import type { FactSource } from '@/app/api/types'
import { cn } from './cn'
import { BuildingIcon, FileIcon, LightbulbIcon, UserIcon, type IconProps } from './icons'
import { SimulationBadge } from './SimulationBadge'

const sourceMeta: Record<FactSource, { label: string; Icon: ComponentType<IconProps>; simulated: boolean }> = {
  mobywatel: { label: 'mObywatel', Icon: UserIcon, simulated: true },
  zus: { label: 'ZUS', Icon: BuildingIcon, simulated: true },
  ezla: { label: 'e-ZLA', Icon: FileIcon, simulated: true },
  usc: { label: 'Rejestr stanu cywilnego', Icon: BuildingIcon, simulated: true },
  user: { label: 'Ty', Icon: UserIcon, simulated: false },
  assumption: { label: 'Założenie', Icon: LightbulbIcon, simulated: false },
}

export function SourceBadge({ source, className }: { source: FactSource; className?: string }) {
  const { label, Icon, simulated } = sourceMeta[source]
  return (
    <span className={cn('inline-flex flex-wrap items-center gap-1.5', className)}>
      <span className="inline-flex items-center gap-1.5 rounded-pill border border-line-strong bg-surface px-2.5 py-0.5 text-sm font-medium text-ink-soft">
        <Icon className="size-4 shrink-0" />
        <span className="sr-only">Źródło: </span>
        {label}
      </span>
      {simulated ? <SimulationBadge /> : null}
    </span>
  )
}
