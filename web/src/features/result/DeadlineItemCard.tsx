import { Link } from 'react-router'
import { Button, Card, DeadlineItem, buttonVariants } from '@/ui'
import type { DeadlineEntry } from './DeadlinesSection'
import { benefitName, personLabels, placeLabels } from './labels'

export function DeadlineItemCard({ entry, showPerson, application }: {
  entry: DeadlineEntry
  showPerson: boolean
  application?: { href: string; label: string } | null
}) {
  const { deadline, benefit } = entry
  const hasApplication = benefit.status === 'przysługuje' && !!benefit.whereToApply
  return (
    <Card as="section" aria-label="Najbliższy krok" className="space-y-3 border-l-4 border-l-brand bg-brand-soft shadow-none">
      <DeadlineItem deadline={deadline} emphasized className="border-0 bg-transparent p-0" />
      <div className="space-y-1 text-sm text-ink-soft">
        <p className="break-words">
          Dotyczy: <span className="font-semibold text-ink">{benefitName(benefit.id)}</span>
          {showPerson ? ` (${personLabels[benefit.person]})` : null}
        </p>
        {hasApplication && benefit.whereToApply ? <p>Gdzie złożyć wniosek: {placeLabels[benefit.whereToApply]}</p> : null}
      </div>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        {application ? (
          <Button asChild className="w-full sm:w-auto">
            <Link to={application.href}>{application.label}</Link>
          </Button>
        ) : null}
        <a href="#terminy" className={buttonVariants({ variant: application ? 'link' : 'secondary' })}>Zobacz terminy</a>
      </div>
    </Card>
  )
}
