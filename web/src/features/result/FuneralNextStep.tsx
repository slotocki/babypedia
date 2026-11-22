import { Link } from 'react-router'
import type { BenefitResult } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Amount, Button, Card, DeadlineItem } from '@/ui'
import { benefitName, placeLabels } from './labels'

/** Uwaga silnika (FuneralBenefit), gdy bez daty pochówku nie da się policzyć terminu wniosku. */
const BURIAL_DATE_NOTE = 'Podaj datę pochówku'

/**
 * Zasiłek pogrzebowy w „Co dalej?”. Termin i druk pokazujemy wyłącznie wtedy, gdy podał je silnik
 * (termin liczy się od dnia pochówku); bez daty pochówku pokazujemy uwagę silnika i odsyłamy do uzupełnienia danych.
 */
export function FuneralNextStep({ benefit }: { benefit: BenefitResult }) {
  const burialNote = benefit.notes.find((n) => n.startsWith(BURIAL_DATE_NOTE))
  return (
    <Card as="section" aria-label={benefitName(benefit.id)} className="space-y-3 border-l-4 border-l-brand shadow-none">
      <h3 className="text-base font-semibold text-ink">{benefitName(benefit.id)}</h3>
      {benefit.lumpSum != null ? <Amount value={benefit.lumpSum} showNote={false} /> : null}
      {benefit.deadlines.map((deadline) => (
        <DeadlineItem key={deadline.id} deadline={deadline} className="border-0 bg-transparent p-0" />
      ))}
      {benefit.whereToApply ? <p className="text-sm text-ink-soft">Gdzie złożyć wniosek: {placeLabels[benefit.whereToApply]}</p> : null}
      {benefit.deadlines.length === 0 && burialNote ? (
        <div className="space-y-2">
          <p className="text-sm text-ink">{burialNote}</p>
          <Button asChild variant="secondary" className="w-full sm:w-auto">
            <Link to={paths.review}>Uzupełnij datę pochówku</Link>
          </Button>
        </div>
      ) : null}
    </Card>
  )
}
