import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Button, DeadlineItem, buttonVariants } from '@/ui'
import type { DeadlineEntry } from './DeadlinesSection'
import { benefitName, personLabels } from './labels'

export function DeadlineItemCard({ entry, showPerson }: { entry: DeadlineEntry; showPerson: boolean }) {
  const { deadline, benefit } = entry
  return (
    <section aria-label="Najbliższy krok" className="space-y-3">
      <DeadlineItem deadline={deadline} emphasized />
      <p className="break-words px-1 text-sm text-ink-soft">
        Dotyczy: <span className="font-semibold text-ink">{benefitName(benefit.id)}</span>
        {showPerson ? ` (${personLabels[benefit.person]})` : null}
      </p>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Button asChild className="w-full sm:w-auto">
          <Link to={paths.submissions}>Przejdź do wniosków</Link>
        </Button>
        <a href="#terminy" className={buttonVariants({ variant: 'link' })}>
          Zobacz wszystkie terminy
        </a>
      </div>
    </section>
  )
}
