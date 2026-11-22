import { useId } from 'react'
import type { BenefitResult, CaseDecision, CaseSummary } from '@/app/api/types'
import { cn, formatDate } from '@/ui'
import { benefitName } from '@/features/result/labels'
import { decisionLabel } from './decision'

/**
 * Okres świadczenia odczytany z akt sprawy ZUS (katalog spraw: `potwierdzoneOkresy`).
 * Pole jest opcjonalne — pokazujemy porównanie okresów tylko wtedy, gdy API je zwraca.
 */
interface ConfirmedPeriod {
  swiadczenie: string
  od: string
  do: string
  dni?: number | null
}

type CaseWithRecords = CaseSummary & { potwierdzoneOkresy?: ConfirmedPeriod[] | null }

export interface CaseDecisionComparisonProps {
  expected: CaseDecision | null | undefined
  benefits: BenefitResult[]
  /** Sprawa z katalogu; jeśli zawiera okresy z akt ZUS, porównujemy je z okresami z silnika. */
  caseSummary?: CaseSummary
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

function PeriodRows({ records, benefits }: { records: ConfirmedPeriod[]; benefits: BenefitResult[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[32rem] text-left text-sm">
        <caption className="sr-only">Okresy świadczeń: akta ZUS i wynik Kompasu Rodzica</caption>
        <thead className="text-ink-soft">
          <tr>
            <th scope="col" className="py-1 pr-3 font-semibold">Świadczenie</th>
            <th scope="col" className="py-1 pr-3 font-semibold">Okres w aktach ZUS</th>
            <th scope="col" className="py-1 pr-3 font-semibold">Okres w Kompasie Rodzica</th>
            <th scope="col" className="py-1 font-semibold">Porównanie</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {records.map((record) => {
            const own = benefits.find((b) => b.id === record.swiadczenie && b.person === 'wnioskodawca' && b.period)
            const period = own?.period
            const same = period ? period.from === record.od && period.to === record.do : null
            return (
              <tr key={`${record.swiadczenie}-${record.od}`}>
                <td className="py-1.5 pr-3">{benefitName(record.swiadczenie)}</td>
                <td className="py-1.5 pr-3">
                  {formatDate(record.od)} – {formatDate(record.do)}
                  {record.dni != null ? ` (${record.dni} dni)` : ''}
                </td>
                <td className="py-1.5 pr-3">{period ? `${formatDate(period.from)} – ${formatDate(period.to)} (${period.days} dni)` : 'brak okresu'}</td>
                <td className={cn('py-1.5 font-semibold', same ? 'text-ok' : 'text-bad')}>{same === null ? 'nie da się porównać' : same ? 'zgodne' : 'różni się'}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

export function CaseDecisionComparison({ expected, benefits, caseSummary, className }: CaseDecisionComparisonProps) {
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
  const records = (caseSummary as CaseWithRecords | undefined)?.potwierdzoneOkresy ?? []

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
      {records.length > 0 ? <PeriodRows records={records} benefits={benefits} /> : null}
    </section>
  )
}
