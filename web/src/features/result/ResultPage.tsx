import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import { useCaseState, userFact } from '@/app/case-context'
import { paths } from '@/app/paths'
import type { AssessmentResult, BenefitResult, FactSet, PaymentVariant } from '@/app/api/types'
import {
  Alert,
  Button,
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  Section,
  SimulationBanner,
  Skeleton,
  Stepper,
  ToneScope,
  buttonVariants,
} from '@/ui'
import { CaseDecisionComparison } from '@/features/cases'
import { BenefitCard } from './BenefitCard'
import { DeadlineItemCard } from './DeadlineItemCard'
import { DeadlinesSection, type DeadlineEntry } from './DeadlinesSection'
import { benefitKey, personLabels, sortBenefits, statusSummaryLabels, todayIso } from './labels'
import { RisksSection } from './RisksSection'
import { useFocusHeading } from './useFocusHeading'
import { VariantSection } from './VariantSection'

const steps = ['Pytania', 'Logowanie', 'Sprawdź dane', 'Wynik']
const COMPLETED_KEY = 'babyradnik.result.completed'
const EXAMPLE_COUNT = 5

function loadCompleted(): string[] {
  try {
    const raw = sessionStorage.getItem(COMPLETED_KEY)
    const parsed: unknown = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []
  } catch {
    return []
  }
}

function parseExample(value: string | null): number | null {
  const n = Number(value)
  return Number.isInteger(n) && n >= 1 && n <= EXAMPLE_COUNT ? n : null
}

function hasIntegrationFacts(facts: FactSet): boolean {
  const buckets = [facts.sprawa, facts.wnioskodawca, facts.drugiRodzic ?? {}] as Record<string, { source?: string } | undefined>[]
  return buckets.some((bucket) => Object.values(bucket).some((fact) => fact && fact.source !== undefined && fact.source !== 'user' && fact.source !== 'assumption'))
}

function isDifficult(facts: FactSet, data?: AssessmentResult): boolean {
  const { sprawa } = facts
  return (
    data?.difficultSituation === true ||
    sprawa['dziecko.martweUrodzenie']?.value === true ||
    sprawa['dziecko.dataZgonu'] !== undefined ||
    (sprawa['dziecko.liczbaZmarlych']?.value ?? 0) > 0
  )
}

function ExampleSwitcher({ current }: { current: number | null }) {
  return (
    <Alert variant="info" title="Tryb deweloperski: przykładowe odpowiedzi">
      <ul className="flex flex-wrap gap-x-4 gap-y-1">
        {Array.from({ length: EXAMPLE_COUNT }, (_, i) => i + 1).map((n) => (
          <li key={n}>
            <Link to={`${paths.result}?przyklad=${n}`} aria-current={current === n ? 'true' : undefined} className="font-semibold text-brand-strong underline underline-offset-4">
              Przykład {n}
              {current === n ? ' (bieżący)' : ''}
            </Link>
          </li>
        ))}
        <li>
          <Link to={paths.result} className="font-semibold text-brand-strong underline underline-offset-4">
            Prawdziwe API
          </Link>
        </li>
      </ul>
    </Alert>
  )
}

function LoadingState() {
  return (
    <div role="status" aria-busy="true" className="space-y-4">
      <span className="sr-only">Liczymy Twój wynik…</span>
      {[0, 1].map((i) => (
        <Card key={i} className="space-y-3">
          <Skeleton className="h-6 w-3/4" />
          <Skeleton className="h-8 w-40" />
          <Skeleton />
          <Skeleton className="w-2/3" />
        </Card>
      ))}
    </div>
  )
}

export function ResultPage() {
  const { facts, setPersonFact, sessionId, caseId } = useCaseState()
  const [params] = useSearchParams()
  const exampleIndex = import.meta.env.DEV ? parseExample(params.get('przyklad')) : null
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [completed, setCompleted] = useState<string[]>(() => (exampleIndex ? [] : loadCompleted()))

  useEffect(() => {
    if (exampleIndex) return
    try {
      sessionStorage.setItem(COMPLETED_KEY, JSON.stringify(completed))
    } catch {
      // brak dostępu do sessionStorage
    }
  }, [completed, exampleIndex])

  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj'])
  const assessment = useQuery({
    queryKey: exampleIndex ? ['assessment-example', exampleIndex] : completed.length > 0 ? ['assessment', facts, completed] : ['assessment', facts],
    queryFn: async () => {
      if (import.meta.env.DEV && exampleIndex) return (await import('./examples')).examples[exampleIndex - 1]
      return api.assess(completed.length > 0 ? { facts, completedDeadlines: completed } : { facts })
    },
    enabled: exampleIndex !== null || hasEvent,
    placeholderData: keepPreviousData,
  })

  const cases = useQuery({
    queryKey: ['cases'],
    queryFn: () => api.cases.list(),
    enabled: caseId !== null && exampleIndex === null,
    staleTime: 5 * 60 * 1000,
  })
  const caseSummary = caseId ? cases.data?.find((c) => c.id === caseId) : undefined

  const data = assessment.data
  const difficult = isDifficult(facts, data)
  const busy = assessment.isFetching
  const today = todayIso()

  const saveBasis = (benefit: BenefitResult, value: number) => {
    if (exampleIndex) return
    setPersonFact(benefit.person, 'podstawa.ustalonaPrzezPlatnika', userFact(value))
  }

  const changeVariant = (variant: PaymentVariant) => {
    if (exampleIndex) return
    setPersonFact('wnioskodawca', 'plan.wariantWyplaty', userFact(variant))
    if (facts.sprawa['zdarzenie.dlaKogo']?.value === 'ja_i_drugi_rodzic') setPersonFact('drugiRodzic', 'plan.wariantWyplaty', userFact(variant))
  }

  const saveApplicationDate = (date: string) => {
    if (exampleIndex) return
    setPersonFact('wnioskodawca', 'plan.dataWnioskuUsrednionego', userFact(date))
  }

  const toggleDone = (id: string, done: boolean) => setCompleted((list) => (done ? [...new Set([...list, id])] : list.filter((x) => x !== id)))

  const showBanner = sessionId !== null || hasIntegrationFacts(facts)
  const variant = facts.wnioskodawca['plan.wariantWyplaty']?.value ?? 'standardowy'

  const renderBody = (result: AssessmentResult) => {
    const visible = sortBenefits(result.benefits.filter((b) => b.status !== 'nie_dotyczy'))
    const grouped = visible.some((b) => b.person === 'drugiRodzic')
    const entries: DeadlineEntry[] = visible.flatMap((benefit) => benefit.deadlines.map((deadline) => ({ deadline, benefit })))
    const next = result.nextStep
    const nextEntry = next
      ? entries.find(
          (e) => e.deadline.id === next.deadlineId && e.benefit.id === next.benefitId && (!next.person || e.benefit.person === next.person),
        )
      : undefined
    const nextOpen = nextEntry && !completed.includes(nextEntry.deadline.id) ? nextEntry : undefined

    const counts = visible.reduce<Partial<Record<keyof typeof statusSummaryLabels, number>>>((acc, b) => {
      if (b.status !== 'nie_dotyczy') acc[b.status] = (acc[b.status] ?? 0) + 1
      return acc
    }, {})
    const summary = (Object.keys(statusSummaryLabels) as (keyof typeof statusSummaryLabels)[])
      .filter((s) => counts[s])
      .map((s) => `${counts[s]} ${statusSummaryLabels[s]}`)
      .join(' · ')

    const renderCards = (list: BenefitResult[]) => (
      <ul className="space-y-4">
        {list.map((benefit) => (
          <li key={benefitKey(benefit)}>
            <BenefitCard
              benefit={benefit}
              headingLevel={grouped ? 4 : 3}
              missing={result.missingFacts.filter(
                (m) => (m.person ?? benefit.person) === benefit.person && (m.benefitIds ?? []).includes(benefit.id),
              )}
              onSaveBasis={saveBasis}
              busy={busy}
            />
          </li>
        ))}
      </ul>
    )

    return (
      <>
        {nextOpen ? <DeadlineItemCard entry={nextOpen} showPerson={grouped} /> : null}

        <Section title="Co Ci przysługuje" description={summary || undefined}>
          {visible.length === 0 ? (
            <EmptyState title="Nie ma jeszcze świadczeń do pokazania" description="Wróć do kroku „Sprawdź dane” i uzupełnij informacje o swojej sytuacji." />
          ) : grouped ? (
            <div className="space-y-8">
              {(['wnioskodawca', 'drugiRodzic'] as const).map((person) => {
                const list = visible.filter((b) => b.person === person)
                if (list.length === 0) return null
                return (
                  <section key={person} aria-label={personLabels[person]} className="space-y-4">
                    <h3 className="text-lg font-bold text-brand-strong">{personLabels[person]}</h3>
                    {renderCards(list)}
                  </section>
                )
              })}
            </div>
          ) : (
            renderCards(visible)
          )}
        </Section>

        {caseSummary ? (
          <CaseDecisionComparison expected={caseSummary.oczekiwanaDecyzja} benefits={visible} />
        ) : null}

        <DeadlinesSection
          entries={entries}
          completed={completed}
          nextDeadlineId={nextOpen?.deadline.id}
          showPerson={grouped}
          today={today}
          onToggle={toggleDone}
        />

        <RisksSection risks={result.risks} />

        <VariantSection
          variant={variant}
          onChange={changeVariant}
          disabled={busy}
          applicationDateQuestion={result.missingFacts.find(
            (m) => m.fact === 'plan.dataWnioskuUsrednionego' && (m.person ?? 'wnioskodawca') === 'wnioskodawca',
          )}
          onApplicationDate={saveApplicationDate}
        />

        {nextOpen ? null : (
          <Button asChild fullWidth className="sm:w-auto">
            <Link to={paths.submissions}>Przejdź do wniosków i zgłoszeń</Link>
          </Button>
        )}
      </>
    )
  }

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 px-4 py-6">
      <div ref={pageRef} className="mx-auto max-w-3xl space-y-8">
        <Stepper steps={steps} current={3} />
        <PageHeader
          title="Twój wynik"
          description={
            difficult
              ? 'Poniżej zebraliśmy najważniejsze kroki. Możesz zajmować się nimi we własnym tempie.'
              : 'Co Ci przysługuje, co zrobić i do kiedy. To szacunek na podstawie podanych danych. Kompas Rodzica informuje, a decyzję podejmuje ZUS lub płatnik składek.'
          }
        />

        {import.meta.env.DEV ? <ExampleSwitcher current={exampleIndex} /> : null}

        {difficult ? (
          <Alert variant="info" title="Wsparcie jest dla Ciebie">
            Jeśli potrzebujesz rozmowy lub pomocy, zobacz{' '}
            <Link to={paths.support} className="font-semibold text-brand-strong underline underline-offset-4">
              organizacje wsparcia
            </Link>
            .
          </Alert>
        ) : null}

        {showBanner ? <SimulationBanner system="Logowanie i dane z ZUS" /> : null}

        {data?.validationErrors && data.validationErrors.length > 0 ? (
          <Alert variant="warning" title="Plan wymaga poprawy">
            <ul className="list-disc space-y-1 pl-5">
              {data.validationErrors.map((issue) => (
                <li key={`${issue.code}-${issue.fact ?? ''}-${issue.person ?? ''}`} className="break-words">
                  {issue.message}
                </li>
              ))}
            </ul>
            <p className="mt-2">
              <Link to={paths.review} className={buttonVariants({ variant: 'link' })}>
                Popraw dane w „Sprawdź dane”
              </Link>
            </p>
          </Alert>
        ) : null}

        {!exampleIndex && !hasEvent ? (
          <EmptyState
            title="Najpierw odpowiedz na trzy pytania"
            description="Bez nich nie wiemy, czego dotyczy Twoja sprawa."
            action={
              <Button asChild>
                <Link to={paths.start}>Wróć na start</Link>
              </Button>
            }
          />
        ) : assessment.isError && !data ? (
          <ErrorState
            error={assessment.error}
            title="Nie udało się pobrać wyniku"
            onRetry={() => void assessment.refetch()}
            retrying={assessment.isFetching}
          />
        ) : !data ? (
          <LoadingState />
        ) : (
          <div className="space-y-8" aria-busy={busy}>
            <p role="status" className="sr-only">
              {busy ? 'Przeliczam wynik…' : ''}
            </p>
            {busy ? (
              <p aria-hidden="true" className="text-sm font-semibold text-ink-soft">
                Przeliczam wynik…
              </p>
            ) : null}
            {assessment.isError ? (
              <ErrorState error={assessment.error} title="Nie udało się odświeżyć wyniku" onRetry={() => void assessment.refetch()} retrying={busy} />
            ) : null}
            {renderBody(data)}
          </div>
        )}
      </div>
    </ToneScope>
  )
}
