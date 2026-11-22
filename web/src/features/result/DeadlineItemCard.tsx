import { Link } from 'react-router'
import type { BenefitResult } from '@/app/api/types'
import { Button, CalendarIcon, Card, DeadlineItem, buttonVariants, formatDate } from '@/ui'
import { AVERAGED_DEADLINE, benefitList, explainLabel, type DeadlineEntry } from './insights'
import { placeLabels } from './labels'

export function DeadlineItemCard({ entry, benefits, showPerson, application, variantLabel, averagedChosen = false }: {
  entry: DeadlineEntry
  /** Wszystkie świadczenia z tym samym terminem (domyślnie tylko świadczenie z wpisu). */
  benefits?: BenefitResult[]
  showPerson: boolean
  application?: { href: string; label: string } | null
  /** Opis wariantu wypłaty pokazanego na kartach, np. „standardowym (100% / 70%)”. */
  variantLabel?: string | null
  /** Użytkownik wybrał wariant 81,5% — wtedy wniosek o ten wariant jest obowiązkowy dla tego wariantu, nie „do decyzji”. */
  averagedChosen?: boolean
}) {
  const { deadline, benefit } = entry
  const hasApplication = benefit.status === 'przysługuje' && !!benefit.whereToApply
  const optional = deadline.id === AVERAGED_DEADLINE && !averagedChosen
  return (
    <Card as="section" aria-label="Najbliższy krok" className="space-y-3 border-l-4 border-l-brand bg-brand-soft shadow-none">
      {optional ? (
        <div className="flex items-start gap-3">
          <CalendarIcon className="mt-0.5 size-6 shrink-0 text-brand-strong" />
          <div className="min-w-0 space-y-1">
            <p className="text-sm font-semibold text-ink-soft">Do decyzji · możesz, nie musisz</p>
            <p className="text-lg font-bold text-ink">
              do <time dateTime={deadline.date}>{formatDate(deadline.date)}</time>
            </p>
            <p className="text-base text-ink">
              {variantLabel ? 'Karty pokazują wariant standardowy. ' : null}
              Możesz zamiast niego wybrać stałą stawkę 81,5% — to Twój wybór, nie obowiązek. Wniosek o ten wariant złóż najpóźniej{' '}
              {formatDate(deadline.date)}.
            </p>
          </div>
        </div>
      ) : (
        <DeadlineItem deadline={deadline} emphasized className="border-0 bg-transparent p-0" />
      )}
      <div className="space-y-1 text-sm text-ink-soft">
        <p className="break-words">
          Dotyczy: <span className="font-semibold text-ink">{benefitList(benefits ?? [benefit], showPerson)}</span>
        </p>
        {hasApplication && benefit.whereToApply && !optional ? <p>Gdzie złożyć wniosek: {placeLabels[benefit.whereToApply]}</p> : null}
      </div>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        {application && !optional ? (
          <Button asChild className="w-full sm:w-auto">
            <Link to={application.href}>{explainLabel(application.label)}</Link>
          </Button>
        ) : null}
        <a href="#terminy" className={buttonVariants({ variant: application && !optional ? 'link' : 'secondary' })}>Zobacz wszystkie terminy</a>
      </div>
    </Card>
  )
}
