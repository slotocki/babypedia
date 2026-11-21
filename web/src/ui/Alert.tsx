import type { ComponentType, ReactNode } from 'react'
import { cn } from './cn'
import { ErrorIcon, InfoIcon, ShieldIcon, WarningIcon, type IconProps } from './icons'

export type AlertVariant = 'info' | 'warning' | 'error' | 'risk'

const variants: Record<AlertVariant, { prefix: string; Icon: ComponentType<IconProps>; className: string }> = {
  info: { prefix: 'Informacja', Icon: InfoIcon, className: 'border-info bg-info-soft text-info' },
  warning: { prefix: 'Uwaga', Icon: WarningIcon, className: 'border-warn bg-warn-soft text-warn' },
  error: { prefix: 'Błąd', Icon: ErrorIcon, className: 'border-bad bg-bad-soft text-bad' },
  risk: { prefix: 'Na co uważać', Icon: ShieldIcon, className: 'border-risk bg-risk-soft text-risk' },
}

export interface AlertProps {
  variant?: AlertVariant
  title?: ReactNode
  children?: ReactNode
  className?: string
}

export function Alert({ variant = 'info', title, children, className }: AlertProps) {
  const { prefix, Icon, className: tone } = variants[variant]
  return (
    <div role={variant === 'error' ? 'alert' : undefined} className={cn('flex items-start gap-3 rounded-card border-l-4 p-4', tone, className)}>
      <Icon className="mt-0.5 size-6 shrink-0" />
      <div className="min-w-0 space-y-1">
        <p className="text-base font-semibold">
          <span className="sr-only">{prefix}: </span>
          {title ?? prefix}
        </p>
        {children ? <div className="text-base text-ink">{children}</div> : null}
      </div>
    </div>
  )
}
