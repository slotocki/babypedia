import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import { useCaseState, userFact } from '@/app/case-context'
import { paths } from '@/app/paths'
import type { AssessmentResult, BenefitResult, FactSet, PaymentVariant } from '@/app/api/types'
import {
  Alert,
  Button,
  Card,
  Disclosure,
  EmptyState,
  ErrorState,
  Section,
  Skeleton,
  Stepper,
  ToneScope,
  buttonVariants,
  formatPercent,
} from '@/ui'
import { CaseDecisionComparison } from '@/features/cases'
import { flowSteps } from '@/features/intake/steps'
import { applicationFor } from '@/features/submissions/kinds'
import { LegalAssistant } from '@/features/support/LegalAssistant'
import { BenefitCard } from './BenefitCard'
import { DeadlineItemCard } from './DeadlineItemCard'
import { DeadlinesSection, type DeadlineEntry } from './DeadlinesSection'
import { FuneralNextStep } from './FuneralNextStep'
import { conditionOf, mergeDeadlines } from './insights'
import { benefitKey, benefitName, personLabels, sortBenefits, todayIso } from './labels'
import { ResultSummary } from './ResultSummary'
import { RisksSection } from './RisksSection'
import { useFocusHeading } from './useFocusHeading'
import { VariantSection } from './VariantSection'

const EXAMPLE_COUNT = 5

/** Pasek przykładowych odpowiedzi pokazujemy tylko na jawne życzenie: /wynik?dev=1 (i tylko w trybie deweloperskim Vite). */
const DEV_PARAM = 'dev'

function parseExample(value: string | null): number | null {
  const n = Number(value)
  return Number.isInteger(n) && n >= 1 && n <= EXAMPLE_COUNT ? n : null
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
            <Link to={`${paths.result}?${DEV_PARAM}=1&przyklad=${n}`} aria-current={current === n ? 'true' : undefined} className="font-semibold text-brand-strong underline underline-offset-4">
              Przykład {n}
              {current === n ? ' (bieżący)' : ''}
            </Link>
          </li>
        ))}
        <li>
          <Link to={`${paths.result}?${DEV_PARAM}=1`} className="font-semibold text-brand-strong underline underline-offset-4">
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
  const { facts, setPersonFact, sessionId, profileName, caseId, completedDeadlines, setDeadlineDone } = useCaseState()
  const [params] = useSearchParams()
  const devMode = import.meta.env.DEV && params.get(DEV_PARAM) === '1'
  const exampleIndex = devMode ? parseExample(params.get('przyklad')) : null
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [exampleCompleted, setExampleCompleted] = useState<string[]>([])
  const completed = exampleIndex ? exampleCompleted : completedDeadlines

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

  const toggleDone = (id: string, done: boolean) => {
    if (exampleIndex) setExampleCompleted((list) => (done ? [...new Set([...list, id])] : list.filter((x) => x !== id)))
    else setDeadlineDone(id, done)
  }

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
    const nextBenefits = nextOpen
      ? mergeDeadlines(entries).find((m) => m.entry.deadline.id === nextOpen.deadline.id && m.benefits.includes(nextOpen.benefit))?.benefits
      : undefined

    // Świadczenia warunkowe (np. chorobowy w ciąży bez L4) idą pod pozostałe; w sytuacji straty dziecka odmowy zwijamy.
    const conditional = (b: BenefitResult) => conditionOf(b) !== null
    const tucked = (b: BenefitResult) => difficult && b.status === 'nie_przysługuje'
    const main = [...visible.filter((b) => !conditional(b) && !tucked(b)), ...visible.filter((b) => conditional(b) && !tucked(b))]
    const notForYou = visible.filter(tucked)

    const averaged = variant === 'usredniony'
    const maternity = visible.find((b) => b.id === 'macierzynski' && b.status === 'przysługuje' && b.rate != null)
    const parental = visible.find((b) => b.id === 'rodzicielski' && b.status === 'przysługuje' && b.rate != null)
    const variantLabel = averaged
      ? 'uśrednionym (81,5%)'
      : maternity?.rate != null && parental?.rate != null
        ? `standardowym (${formatPercent(maternity.rate)} za urlop macierzyński, ${formatPercent(parental.rate)} za rodzicielski)`
        : null
    const variantNote = variantLabel && (maternity || parental) ? `Kwoty pokazujemy w wariancie ${variantLabel}.` : undefined

    const funeral = visible.filter((b) => b.id === 'pogrzebowy' && b.status === 'przysługuje' && nextOpen?.benefit !== b)

    const renderCards = (list: BenefitResult[]) => (
      <ul className="space-y-4">
        {list.map((benefit) => (
          <li key={benefitKey(benefit)}>
            <BenefitCard
              benefit={benefit}
              application={exampleIndex ? null : applicationFor(benefit, facts)}
              headingLevel={grouped ? 4 : 3}
              missing={result.missingFacts.filter(
                (m) => (m.person ?? benefit.person) === benefit.person && (m.benefitIds == null || m.benefitIds.includes(benefit.id)),
              )}
              onSaveBasis={saveBasis}
              busy={busy}
              difficult={difficult}
            />
          </li>
        ))}
      </ul>
    )

    const renderGrouped = (list: BenefitResult[]) =>
      grouped ? (
        <div className="space-y-8">
          {(['wnioskodawca', 'drugiRodzic'] as const).map((person) => {
            const mine = list.filter((b) => b.person === person)
            if (mine.length === 0) return null
            return (
              <section key={person} aria-label={personLabels[person]} className="space-y-4">
                <h3 className="text-lg font-bold text-brand-strong">{personLabels[person]}</h3>
                {renderCards(mine)}
              </section>
            )
          })}
        </div>
      ) : (
        renderCards(list)
      )

    return (
      <>
        {visible.length > 0 ? <ResultSummary benefits={visible} grouped={grouped} difficult={difficult} /> : null}
        <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
          <div className="min-w-0 space-y-4">
            <Section title={difficult ? 'Co Ci przysługuje' : 'Twoje świadczenia'} description={variantNote}>
              {visible.length === 0 ? (
                <EmptyState title="Nie ma jeszcze świadczeń do pokazania" description="Wróć do kroku „Sprawdź dane” i uzupełnij informacje o swojej sytuacji." />
              ) : (
                renderGrouped(main)
              )}
              {notForYou.length > 0 ? (
                <Disclosure title="Te świadczenia nie dotyczą Twojej sytuacji — szczegóły" summary={notForYou.map((b) => benefitName(b.id)).join(', ')}>
                  {renderGrouped(notForYou)}
                </Disclosure>
              ) : null}
            </Section>
            {caseSummary ? <CaseDecisionComparison expected={caseSummary.oczekiwanaDecyzja} benefits={visible} caseSummary={caseSummary} /> : null}
          </div>

          <aside aria-label="Dalsze kroki" className="min-w-0 space-y-4">
            <h2 className="text-xl font-bold tracking-tight">Co dalej?</h2>
            {nextOpen ? (
              <DeadlineItemCard
                entry={nextOpen}
                benefits={nextBenefits}
                showPerson={grouped}
                application={applicationFor(nextOpen.benefit, facts)}
                variantLabel={variantLabel}
                averagedChosen={averaged}
              />
            ) : null}
            {funeral.map((b) => (
              <FuneralNextStep key={benefitKey(b)} benefit={b} />
            ))}
            <Card className="flex flex-col items-start gap-2">
              <Button asChild variant="link"><Link to={paths.submissions}>Wszystkie wnioski i dokumenty</Link></Button>
              {difficult ? null : <Button asChild variant="link"><Link to={paths.scenarios}>Porównaj warianty</Link></Button>}
              <Button asChild variant="link"><Link to={paths.review}>Zmień dane</Link></Button>
              {sessionId ? <Button asChild variant="link"><Link to={paths.login}>Zmień profil lub sprawę</Link></Button> : null}
            </Card>
          </aside>
        </div>

        <div className="space-y-4">
          <DeadlinesSection
            entries={entries}
            completed={completed}
            next={nextOpen ? { deadlineId: nextOpen.deadline.id, benefitId: nextOpen.benefit.id, person: nextOpen.benefit.person } : null}
            showPerson={grouped}
            today={today}
            onToggle={toggleDone}
            averagedChosen={averaged}
          />
          <Disclosure title={difficult ? 'Na co uważać' : 'Ryzyka i wariant wypłaty'}>
            <div className="space-y-5">
              <RisksSection risks={result.risks} entries={entries} />
              {difficult ? null : (
                <VariantSection
                  variant={variant}
                  onChange={changeVariant}
                  disabled={busy}
                  applicationDateQuestion={result.missingFacts.find(
                    (m) => m.fact === 'plan.dataWnioskuUsrednionego' && (m.person ?? 'wnioskodawca') === 'wnioskodawca',
                  )}
                  onApplicationDate={saveApplicationDate}
                />
              )}
            </div>
          </Disclosure>
        </div>
      </>
    )
  }

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 px-4 py-6">
      <div ref={pageRef} className="mx-auto max-w-5xl space-y-6">
        {difficult ? (
          <Alert variant="info" title="Wsparcie jest dla Ciebie">
            Nie musisz załatwiać wszystkiego naraz. Jeśli potrzebujesz rozmowy lub pomocy, zobacz{' '}
            <Link to={paths.support} className="font-semibold text-brand-strong underline underline-offset-4">
              organizacje wsparcia
            </Link>
            .
          </Alert>
        ) : null}
        <Stepper steps={flowSteps} current={2} />
        <header className="flex flex-wrap items-start justify-between gap-3 rounded-card border border-line bg-surface p-5 sm:p-7">
          <div className="max-w-xl space-y-2">
            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">{difficult ? 'Co możesz teraz załatwić' : 'Twój plan'}</h1>
            <p className="text-ink-soft">
              {difficult
                ? 'Poniżej zebraliśmy najważniejsze kroki. Możesz zajmować się nimi we własnym tempie.'
                : 'Wynik na podstawie dostępnych danych. Decyzję podejmuje ZUS lub płatnik składek.'}
            </p>
          </div>
          {sessionId ? (
            <Link to={paths.account} className="inline-flex min-h-11 items-center rounded-control bg-canvas px-3 text-sm font-semibold text-brand-strong hover:bg-brand-soft">
              {profileName || 'Moje konto'}
            </Link>
          ) : null}
        </header>

        {devMode ? <ExampleSwitcher current={exampleIndex} /> : null}

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
            title="Najpierw podaj, czego dotyczy sprawa"
            description="Wprowadź dane ręcznie, połącz się przez mObywatela albo wybierz sprawę ZUS na starcie."
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
            <LegalAssistant calculatorPath={hasEvent ? paths.review : paths.start} />
          </div>
        )}
      </div>
    </ToneScope>
  )
}
