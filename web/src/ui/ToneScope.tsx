import type { ReactNode } from 'react'
import { cn } from './cn'

export interface ToneScopeProps {
  tone?: 'default' | 'difficult'
  className?: string
  children: ReactNode
}

export function ToneScope({ tone = 'default', className, children }: ToneScopeProps) {
  return (
    <div data-tone={tone === 'difficult' ? 'difficult' : undefined} className={cn('bg-canvas text-ink', className)}>
      {children}
    </div>
  )
}
