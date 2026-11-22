import { useEffect, useRef } from 'react'
import { Link } from 'react-router'
import type { ScenarioId } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import type { BenefitResult, CompareResult } from '@/app/api/types'
import { Alert, Amount, Button, Card, Disclosure, Period, Section, StatusBadge } from '@/ui'
import { formatDateShort, formatMoney } from '@/ui/format'
import { analyze, benefitTitle, describeChange, signedMoney, signedWeeks, summarize, type BenefitChange } from './diff'
import { statusText } from './labels'

interface ComparisonViewProps {
  result: CompareResult
  scenarioTitle: string
  scenarioId?: ScenarioId
  /** Brak = wynik bez faktów wariantu (np. przykład deweloperski) — zastosować nie można. */
  onApply?: () => void
}

export function ComparisonView({ result, scenarioTitle, scenarioId, onApply }: ComparisonViewProps) {
  const headingRef = useRef<HTMLHeadingElement>(null)
  const analysis = analyze(result)
  const { changed, unchanged, both } = analysis
  const summary = summarize(result, analysis, scenarioId)
  const { sessionId } = useCaseState()
  const unknownAfter = result.variant.benefits.some((b) => b.status === 'brakuje_danych' && changed.some((c) => c.key === `${b.id}:${b.person}`))

  useEffect(() => {
    headingRef.current?.focus()
    headingRef.current?.scrollIntoView({ block: 'start' })
  }, [])

  return (
    <div className="space-y-8">
      <Card tone="highlight" className="space-y-4">
        <div role="status" aria-live="polite" className="space-y-4">
          <h2 ref={headingRef} tabIndex={-1} className="text-xl font-bold text-ink focus:outline-none focus-visible:outline-3 focus-visible:outline-offset-2 focus-visible:outline-brand">
            Różnica: plan bazowy a wariant „{scenarioTitle}”
          </h2>
          {summary ? <p className="text-lg font-semibold text-ink">{summary}</p> : null}
          {changed.length === 0 ? (
            <p className="text-lg text-ink">Ten wariant nie zmienia wyniku: statusy, okresy i kwoty zostają takie same jak w planie bazowym.</p>
          ) : (
            <>
              <ul className="space-y-2 text-lg text-ink">
                {changed.map((c) => (
                  <li key={c.key} className="flex gap-2">
                    <span aria-hidden="true">•</span>
                    <span>{describeChange(c, both)}</span>
                  </li>
                ))}
              </ul>
              <p className="text-xl font-bold text-ink">
                {analysis.totalAmountDelta === null
                  ? 'Łącznie: różnicy w złotych jeszcze nie policzymy — w jednym z planów brakuje kwoty.'
                  : analysis.totalAmountDelta === 0
                    ? `Łącznie: ${formatMoney(0)} — kwoty się nie zmieniają.`
                    : `Łącznie: ${signedMoney(analysis.totalAmountDelta)}`}
              </p>
              {unknownAfter ? (
                <p className="text-ink">
                  W wariancie brakuje jeszcze danych, od których zależy część świadczeń (np. podstawa wymiaru albo Twoja rola wobec dziecka). Uzupełnij je, a policzymy też kwoty.{' '}
                  <Link className="font-semibold text-brand-strong underline underline-offset-4" to={sessionId ? paths.review : paths.manual}>
                    Uzupełnij dane
                  </Link>
                </p>
              ) : null}
              <p className="text-sm text-ink-soft">Kwoty brutto, przed zaliczką na podatek. To wyliczenie informacyjne, nie decyzja ZUS.</p>
            </>
          )}
        </div>
        {result.changes && result.changes.length > 0 ? (
          <div className="space-y-1">
            <h3 className="font-semibold text-ink">Co zmieniamy w Twoich danych w tym wariancie</h3>
            <ul className="list-disc space-y-1 pl-5 text-ink">
              {result.changes.map((c) => (
                <li key={c}>{c}</li>
              ))}
            </ul>
          </div>
        ) : null}
        <div className="flex flex-wrap gap-3 pt-2">
          {onApply ? <Button onClick={onApply}>Zastosuj ten wariant w moim planie</Button> : null}
          <Button variant="secondary" asChild>
            <Link to={paths.result}>Wróć do wyniku</Link>
          </Button>
        </div>
        <p className="text-sm text-ink-soft">Dopóki nie zastosujesz wariantu, Twój plan bazowy się nie zmienia.</p>
      </Card>

      {changed.length > 0 ? (
        <Section title={`Co się zmienia (${changed.length})`}>
          <ul className="space-y-4">
            {changed.map((c) => (
              <li key={c.key}>
                <ChangedBenefit change={c} both={both} />
              </li>
            ))}
          </ul>
        </Section>
      ) : null}

      {analysis.newRisks.length > 0 ? (
        <Section title="Na co uważać w tym wariancie">
          <ul className="space-y-3">
            {analysis.newRisks.map((r) => (
              <li key={r.id}>
                <Alert variant="risk" title="Nowe w wariancie">
                  <p>{r.text}</p>
                  {r.basis ? <p className="text-sm text-ink-soft">Podstawa: {r.basis}</p> : null}
                  {r.link ? (
                    <a className="font-semibold text-brand-strong underline underline-offset-4" href={r.link.url} target="_blank" rel="noreferrer">
                      {r.link.label}
                    </a>
                  ) : null}
                </Alert>
              </li>
            ))}
          </ul>
        </Section>
      ) : null}

      {analysis.deadlines.length > 0 ? (
        <Section title="Zmienione terminy">
          <ul className="space-y-3">
            {analysis.deadlines.map((d) => (
              <li key={d.id}>
                <Card className="space-y-1">
                  <p className="font-semibold text-ink">{d.variant.text}</p>
                  <p className="text-ink">
                    {d.base
                      ? `Termin w planie bazowym: ${formatDateShort(d.base.date)}. Termin w wariancie: ${formatDateShort(d.variant.date)}.`
                      : `Nowy termin w wariancie: ${formatDateShort(d.variant.date)}.`}
                  </p>
                  {d.variant.consequence ? <p className="text-sm text-ink-soft">Skutek zwłoki: {d.variant.consequence}</p> : null}
                </Card>
              </li>
            ))}
          </ul>
        </Section>
      ) : null}

      {unchanged.length > 0 ? (
        <Disclosure title={`Bez zmian (${unchanged.length})`} summary="Świadczenia, których ten wariant nie zmienia">
          <ul className="divide-y divide-line">
            {unchanged.map((c) => (
              <li key={c.key} className="flex flex-wrap items-center justify-between gap-2 py-3">
                <span className="font-medium text-ink">{benefitTitle(c, both)}</span>
                <span className="flex flex-wrap items-center gap-2">
                  <StatusBadge status={c.statusBase} />
                  {c.base?.period ? <span className="text-sm text-ink-soft">{periodText(c.base)}</span> : null}
                </span>
              </li>
            ))}
          </ul>
        </Disclosure>
      ) : null}
    </div>
  )
}

const periodText = (b: BenefitResult) => (b.period ? `${formatDateShort(b.period.from)} – ${formatDateShort(b.period.to)}` : '')

function ChangedBenefit({ change, both }: { change: BenefitChange; both: boolean }) {
  const columns: { label: string; benefit?: BenefitResult; status: BenefitChange['statusBase'] }[] = [
    { label: 'Plan bazowy', benefit: change.base, status: change.statusBase },
    { label: 'Wariant', benefit: change.variant, status: change.statusVariant },
  ]

  return (
    <Card className="space-y-4">
      <h3 className="text-lg font-semibold text-ink">{benefitTitle(change, both)}</h3>
      <div className="grid gap-4 sm:grid-cols-2">
        {columns.map((col) => (
          <section key={col.label} aria-label={col.label} className="min-w-0 space-y-3 rounded-control border border-line bg-surface p-3">
            <p className="text-sm font-semibold uppercase tracking-wide text-ink-soft">{col.label}</p>
            <StatusBadge status={col.status} />
            {col.benefit?.period ? <Period period={col.benefit.period} /> : <p className="text-ink-soft">Brak okresu zasiłku.</p>}
            {col.benefit?.amount ? (
              <Amount value={col.benefit.amount.total} showNote={false} />
            ) : (
              <p className="text-ink-soft">{col.status === 'przysługuje' ? 'Kwota: brakuje podstawy wymiaru.' : 'Brak kwoty.'}</p>
            )}
          </section>
        ))}
      </div>
      <ul className="space-y-1 text-base text-ink">
        {change.statusChanged ? (
          <li>
            Status: {statusText[change.statusBase]} → {statusText[change.statusVariant]}
          </li>
        ) : null}
        {change.weeksDelta ? <li>Czas trwania: {signedWeeks(change.weeksDelta)}</li> : null}
        {change.amountDelta ? <li>Kwota: {signedMoney(change.amountDelta)}</li> : null}
      </ul>
    </Card>
  )
}
