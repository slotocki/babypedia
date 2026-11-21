import type { ComponentType } from 'react'
import type { BenefitStatus } from '@/app/api/types'
import { cn } from './cn'
import { CheckIcon, CrossIcon, InfoIcon, MinusIcon, QuestionIcon, type IconProps } from './icons'

const statusMeta: Record<BenefitStatus, { label: string; Icon: ComponentType<IconProps>; className: string }> = {
  przysługuje: { label: 'Przysługuje', Icon: CheckIcon, className: 'border-ok bg-ok-soft text-ok' },
  nie_przysługuje: { label: 'Nie przysługuje', Icon: CrossIcon, className: 'border-bad bg-bad-soft text-bad' },
  brakuje_danych: { label: 'Brakuje danych', Icon: QuestionIcon, className: 'border-warn bg-warn-soft text-warn' },
  nie_dotyczy: { label: 'Nie dotyczy', Icon: MinusIcon, className: 'border-neutral bg-neutral-soft text-neutral' },
  nie_rozstrzygam: { label: 'Nie rozstrzygam — zapytaj ZUS', Icon: InfoIcon, className: 'border-info bg-info-soft text-info' },
}

export function StatusBadge({ status, className }: { status: BenefitStatus; className?: string }) {
  const { label, Icon, className: tone } = statusMeta[status]
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-pill border px-3 py-1 text-sm font-semibold', tone, className)}>
      <Icon className="size-4 shrink-0" />
      {label}
    </span>
  )
}
