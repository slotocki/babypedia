import type { ReactNode } from 'react'
import { cn } from './cn'
import { FlaskIcon } from './icons'

export interface SimulationBannerProps {
  system?: string
  children?: ReactNode
  className?: string
}

export function SimulationBanner({ system, children, className }: SimulationBannerProps) {
  return (
    <div
      className={cn('flex items-start gap-3 rounded-card border-2 border-dashed border-warn bg-warn-soft p-3 text-warn sm:p-4', className)}
    >
      <FlaskIcon className="mt-0.5 size-6 shrink-0" />
      <p className="min-w-0 text-base">
        <strong className="font-bold uppercase tracking-wide">Symulacja.</strong>{' '}
        {children ?? (
          <>
            {system ? `${system} działa tu na fikcyjnych danych. ` : 'Dane są fikcyjne. '}
            Nie łączymy się z prawdziwym systemem.
          </>
        )}
      </p>
    </div>
  )
}
