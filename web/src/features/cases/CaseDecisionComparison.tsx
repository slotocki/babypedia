import { useId } from 'react'
import type { BenefitResult, CaseDecision } from '@/app/api/types'
import { cn } from '@/ui'
import { decisionLabel } from './decision'

export interface CaseDecisionComparisonProps {
  expected: CaseDecision | null | undefined
  benefits: BenefitResult[]
  className?: string
}

type Outcome = 'przyznanie' | 'odmowa' | 'brak'

function outcomeOf(benefits: BenefitResult[]): Outcome {
  const relevant = benefits.filter((b) => b.status !== 'nie_dotyczy')
  if (relevant.some((b) => b.status === 'przysługuje')) return 'przyznanie'
  if (relevant.length > 0 && relevant.every((b) => b.status === 'nie_przysługuje')) return 'odmowa'
  return 'brak'
}

const outcomeLabel: Record<Outcome, string> = {
  przyznanie: 'przyznanie (co najmniej jedno świadczenie przysługuje)',
  odmowa: 'odmowa (żadne świadczenie nie przysługuje)',
  brak: 'brak rozstrzygnięcia (brakuje danych albo trzeba zapytać ZUS)',
}

export function CaseDecisionComparison({ expected, benefits, className }: CaseDecisionComparisonProps) {
  const headingId = useId()
  if (!expected) return null
  const outcome = outcomeOf(benefits)
  const comparable = expected !== 'do_odczytania' && outcome !== 'brak'
  const verdict = !comparable ? 'nie da się porównać' : expected === outcome ? 'zgodne' : 'różni się'
  const reason =
    expected === 'do_odczytania'
      ? 'Decyzję ZUS w tej sprawie trzeba odczytać z dokumentów sprawy — nie mamy jej zapisanej w systemie.'
      : outcome === 'brak'
        ? 'Wynik Kompasu Rodzica nie rozstrzyga sprawy, więc nie ma z czym porównać decyzji.'
        : null

  return (
    <section aria-labelledby={headingId} className={cn('space-y-2 rounded-card border border-line bg-surface p-4', className)}>
      <h3 id={headingId} className="text-lg font-semibold text-ink">
        Porównanie z decyzją ZUS (sprawa testowa)
      </h3>
      <dl className="space-y-1 text-base text-ink">
        <div>
          <dt className="inline font-semibold">Decyzja ZUS w tej sprawie: </dt>
          <dd className="inline">{decisionLabel[expected]}</dd>
        </div>
        <div>
          <dt className="inline font-semibold">Wynik Kompasu Rodzica: </dt>
          <dd className="inline">{outcomeLabel[outcome]}</dd>
        </div>
        <div>
          <dt className="inline font-semibold">Porównanie: </dt>
          <dd className="inline">{verdict}</dd>
        </div>
      </dl>
      {reason ? <p className="text-sm text-ink-soft">{reason}</p> : null}
    </section>
  )
}
