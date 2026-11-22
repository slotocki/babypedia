import { useMutation } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api, ApiError } from '@/app/api/client'
import type { CompareResult, FactSet, ScenarioChange, ScenarioId, ValidationIssue } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { Alert, Button, ErrorState, PageHeader, RadioGroup, Section, SkeletonText } from '@/ui'
import { ComparisonView } from './ComparisonView'
import { ScenarioForm, type ServerErrors } from './ScenarioForm'
import {
  availability,
  emptyValues,
  isFieldIssue,
  primaryField,
  personsInCalculation,
  scenarioList,
  totalLeaveWeeks,
  valuesFromChange,
  type FormValues,
} from './scenarios'
import { useFocusHeading } from './useFocusHeading'

const devExampleParam = (params: URLSearchParams): string | null => (import.meta.env.DEV ? params.get('przyklad') : null)

async function runCompare(facts: FactSet, change: ScenarioChange, example: string | null): Promise<CompareResult> {
  if (import.meta.env.DEV && example) {
    const { devExamples } = await import('./examples')
    const found = devExamples[example]
    if (found) {
      await new Promise((resolve) => setTimeout(resolve, 400))
      return found.result
    }
  }
  return api.compare({ facts, scenarios: [change] })
}

function splitIssues(id: ScenarioId, result: CompareResult | undefined) {
  const fields: ServerErrors = {}
  const general: ValidationIssue[] = []
  if (!result) return { fields, general }
  const seen = new Set<string>()
  for (const issue of [...result.validationErrors, ...(result.variant.validationErrors ?? [])]) {
    if (seen.has(issue.message)) continue
    seen.add(issue.message)
    const field = primaryField[id]
    if (field && isFieldIssue(id, issue.fact)) fields[field] = fields[field] ? `${fields[field]} ${issue.message}` : issue.message
    else general.push(issue)
  }
  return { fields, general }
}

export function ScenariosPage() {
  const { facts, setFacts, sessionId } = useCaseState()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const example = devExampleParam(params)
  const pageRef = useFocusHeading<HTMLDivElement>()
  const formAreaRef = useRef<HTMLDivElement>(null)
  const [selectedId, setSelectedId] = useState<ScenarioId | null>(null)
  const [prefill, setPrefill] = useState<ScenarioChange | null>(null)
  const [submitted, setSubmitted] = useState<ScenarioChange | null>(null)

  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj']) || example !== null
  const totalWeeks = totalLeaveWeeks(facts)

  const compare = useMutation({
    mutationFn: (change: ScenarioChange) => runCompare(facts, change, example),
  })

  const runChange = (change: ScenarioChange) => {
    setSubmitted(change)
    compare.mutate(change)
  }

  useEffect(() => {
    if (!example) return
    let cancelled = false
    void import('./examples').then(({ devExamples }) => {
      const found = devExamples[example]
      if (!found || cancelled) return
      setSelectedId(found.change.id)
      setPrefill(found.change)
      setSubmitted(found.change)
      compare.mutate(found.change)
    })
    return () => {
      cancelled = true
    }
    // jednorazowe wczytanie przykładu deweloperskiego
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [example])

  const single = personsInCalculation(facts).length < 2
  // Niedostępne na końcu; wariant z drugim rodzicem przy liczeniu tylko „dla mnie” tuż przed nimi.
  const rank = (o: { value: string; disabled: boolean }) => (o.disabled ? 2 : o.value === 'drugi_rodzic_bierze' && single ? 1 : 0)
  const options = useMemo(
    () =>
      // Najczęstsze (kolejność listy) na górze; niedostępne na końcu, z wyjaśnieniem.
      scenarioList.map((s) => {
        const a = availability(s.id, facts)
        const forced = prefill?.id === s.id
        const disabled = a.persons.length === 0 && !forced
        return {
          value: s.id,
          label: s.title,
          disabled,
          description: disabled ? (
            <>
              {s.description} <span className="font-medium text-ink">Niedostępne: {a.reason}</span>
            </>
          ) : (
            s.description
          ),
        }
      }).sort((a, b) => rank(a) - rank(b)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [facts, prefill, single],
  )

  const selected = scenarioList.find((s) => s.id === selectedId) ?? null
  const selectedAvailability = selected ? availability(selected.id, facts) : null
  const { fields: serverErrors, general: generalIssues } = splitIssues(submitted?.id ?? selectedId ?? 'wariant_wyplaty', compare.data)
  const hasIssues = Boolean(compare.data) && (Object.keys(serverErrors).length > 0 || generalIssues.length > 0)

  useEffect(() => {
    if (hasIssues) formAreaRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [hasIssues, compare.data])

  const pickScenario = (id: ScenarioId) => {
    setSelectedId(id)
    setPrefill(null)
    setSubmitted(null)
    compare.reset()
  }

  const variantFacts = compare.data?.variantFacts ?? null
  const apply = () => {
    if (!variantFacts) return
    setFacts(variantFacts)
    navigate(paths.result)
  }

  const needsSecondParent = selected?.id === 'drugi_rodzic_bierze' && personsInCalculation(facts).length < 2 && prefill === null
  const initial: FormValues = prefill && prefill.id === selectedId ? valuesFromChange(prefill) : emptyValues
  const showResult = compare.isSuccess && !hasIssues && selected

  return (
    <div ref={pageRef} className="mx-auto max-w-3xl space-y-8">
      <PageHeader
        title="Porównaj warianty"
        description="Sprawdź, co by się zmieniło, gdyby Twoja sytuacja albo plan wyglądały inaczej. Plan bazowy zostaje taki, jaki jest, dopóki sam nie zastosujesz wariantu."
      />

      {!hasEvent ? (
        <Alert variant="warning" title="Najpierw podaj, czego dotyczy sprawa">
          Nie wiemy jeszcze, czego dotyczy sprawa, więc nie mamy z czym porównywać wariantów.
          <div className="pt-3">
            <Button asChild>
              <Link to={paths.start}>Wróć na start</Link>
            </Button>
          </div>
        </Alert>
      ) : (
        <>
          <Section title="1. Wybierz, co chcesz sprawdzić" description="Najczęściej sprawdzane są na górze. Warianty, które nie pasują do Twoich danych, są na końcu, wyszarzone, z krótkim wyjaśnieniem.">
            <RadioGroup name="scenariusz" options={options} value={selectedId ?? ''} onChange={(e) => pickScenario(e.target.value as ScenarioId)} />
          </Section>

          {selected && selectedAvailability ? (
            <div ref={formAreaRef}>
              <Section title="2. Ustaw wariant">
                {needsSecondParent ? (
                  <Alert variant="info" title="Ten wariant wymaga danych obojga rodziców">
                    Liczysz teraz tylko dla siebie, więc nie wiemy nic o drugim rodzicu i nie da się podzielić urlopu rodzicielskiego. Dodaj drugiego rodzica
                    do swoich danych, a potem wróć do tego porównania.
                    <div className="pt-3">
                      <Button asChild>
                        <Link to={sessionId ? paths.review : paths.manual}>Dodaj drugiego rodzica</Link>
                      </Button>
                    </div>
                  </Alert>
                ) : (
                <ScenarioForm
                  key={`${selected.id}-${prefill ? 'p' : 'n'}`}
                  scenario={selected}
                  persons={selectedAvailability.persons.length > 0 ? selectedAvailability.persons : ['wnioskodawca']}
                  totalWeeks={totalWeeks}
                  initial={initial}
                  serverErrors={serverErrors}
                  pending={compare.isPending}
                  onChangeValues={() => {
                    if (hasIssues) compare.reset()
                  }}
                  onSubmit={runChange}
                />
                )}
              </Section>
            </div>
          ) : null}

          {generalIssues.length > 0 ? (
            <Alert variant="warning" title="Ten wariant nie jest możliwy">
              <ul className="list-disc space-y-1 pl-5">
                {generalIssues.map((i) => (
                  <li key={i.message}>{i.message}</li>
                ))}
              </ul>
            </Alert>
          ) : null}

          {hasIssues && Object.keys(serverErrors).length > 0 ? (
            <Alert variant="warning" title="Tego wariantu nie da się policzyć">
              Plan z tym wariantem nie spełnia zasad. Wyjaśnienie jest przy polu powyżej. Zmień wartość i porównaj ponownie.
            </Alert>
          ) : null}

          {compare.isPending ? (
            <div aria-live="polite">
              <SkeletonText lines={5} label="Porównuję plan bazowy z wariantem…" />
            </div>
          ) : null}

          {compare.isError ? <CompareError error={compare.error} onRetry={() => submitted && compare.mutate(submitted)} retrying={compare.isPending} /> : null}

          {showResult ? <ComparisonView key={compare.submittedAt} result={compare.data} scenarioId={selected.id} scenarioTitle={selected.title} onApply={variantFacts ? apply : undefined} /> : null}

          <div>
            <Button variant="secondary" asChild>
              <Link to={paths.result}>Wróć do wyniku</Link>
            </Button>
          </div>
        </>
      )}
    </div>
  )
}

function CompareError({ error, onRetry, retrying }: { error: unknown; onRetry: () => void; retrying: boolean }) {
  const notReady = error instanceof ApiError && error.status === 501
  return (
    <div aria-live="polite">
      <ErrorState
        error={error}
        onRetry={onRetry}
        retrying={retrying}
        title={notReady ? 'Porównywanie wariantów jest jeszcze niedostępne' : 'Nie udało się porównać wariantów'}
        description={
          notReady
            ? 'Ta funkcja jest w trakcie przygotowania po stronie serwera. Twój plan bazowy się nie zmienił. Wróć do wyniku albo spróbuj ponownie za jakiś czas.'
            : undefined
        }
      />
    </div>
  )
}
