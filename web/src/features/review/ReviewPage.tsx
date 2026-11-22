import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import type { FactSet, MissingFact } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { flowSteps } from '@/features/intake/steps'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { Alert, Button, Card, Disclosure, ErrorState, Section, Spinner, Stepper, ToneScope, PageHeader } from '@/ui'
import { AnswerField } from './AnswerField'
import { draftFromValue, isDraftEmpty, parseDraft, type Draft } from './answers'
import { FactList } from './FactList'
import { getFact, userFactOf, withFact } from './facts'
import { resolveQuestion } from './questions'

/** Podstawowe pytania o zdarzenie jako brakujące fakty — gdy wejście nie przeszło przez ekran 1 (np. „Sprawy ZUS”), a integracje nie dały zdarzenia. */
const eventQuestions: MissingFact[] = [
  {
    fact: 'zdarzenie.rodzaj',
    question: 'Co się dzieje: planujesz dziecko, jesteś w ciąży, dziecko się urodziło czy przyjmujesz dziecko na wychowanie?',
    type: 'enum',
    person: null,
    options: ['planuje', 'ciaza', 'urodzenie', 'przyjecie'],
  },
  { fact: 'zdarzenie.data', question: 'Jaka jest przewidywana data porodu, data urodzenia dziecka albo data przyjęcia dziecka?', type: 'date', person: null },
  { fact: 'zdarzenie.dlaKogo', question: 'Dla kogo liczymy: tylko dla Ciebie czy dla Ciebie i drugiego rodzica?', type: 'enum', person: null, options: ['ja', 'ja_i_drugi_rodzic'] },
]

/** Fakty z kroku „Praca i ubezpieczenie” (/wprowadz-dane), których nie da się wpisać na tym ekranie. */
const workFacts = ['ubezpieczenie.tytuly', 'ubezpieczenie.podstawySkladek', 'skladki.zadluzenie']

function missingEventQuestions(facts: FactSet, fromEngine: MissingFact[]): MissingFact[] {
  return eventQuestions.filter(
    (q) => facts.sprawa[q.fact as keyof FactSet['sprawa']] === undefined && !fromEngine.some((m) => m.fact === q.fact && !m.person),
  )
}

export function ReviewPage() {
  const { facts, setFacts, sessionId, caseId } = useCaseState()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const formRef = useRef<HTMLFormElement>(null)
  const pageRef = useFocusHeading<HTMLDivElement>()
  const questionsHeading = useRef<HTMLHeadingElement>(null)
  const [drafts, setDrafts] = useState<Record<string, Draft>>({})
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saved, setSaved] = useState<string | null>(null)

  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj'])
  const hasImportedFacts = [facts.sprawa, facts.wnioskodawca, facts.drugiRodzic ?? {}].some((bucket) =>
    Object.values(bucket).some((fact) => fact && fact.source !== 'user' && fact.source !== 'assumption'),
  )
  const needsConfirmation = Boolean(sessionId && (caseId || hasImportedFacts)) && params.get('potwierdzone') !== '1'
  const assessment = useQuery({
    queryKey: ['assessment', facts],
    queryFn: () => api.assess({ facts }),
    enabled: hasEvent && !needsConfirmation,
    placeholderData: keepPreviousData,
  })

  const engineMissing = hasEvent ? (assessment.data?.missingFacts ?? []) : []
  const resolved = [...missingEventQuestions(facts, engineMissing), ...engineMissing].map(resolveQuestion)
  const answerable = resolved.filter((r) => r.kind !== 'readonly')
  const readonlyQuestions = resolved.filter((r) => r.kind === 'readonly')
  // Przy ręcznym wprowadzaniu (bez danych z logowania) braki o pracy uzupełnia krok „Praca i ubezpieczenie”.
  const fillableWork = hasImportedFacts ? [] : readonlyQuestions.filter((r) => workFacts.includes(r.question.fact))
  const unsupported = readonlyQuestions.filter((r) => !fillableWork.includes(r))
  const workSteps = [...new Set(fillableWork.map((r) => (r.bucket === 'drugiRodzic' ? 'drugi' : 'praca')))]

  const sprawa = facts.sprawa
  const difficult =
    assessment.data?.difficultSituation === true ||
    sprawa['dziecko.martweUrodzenie']?.value === true ||
    sprawa['dziecko.dataZgonu'] !== undefined ||
    (sprawa['dziecko.liczbaZmarlych']?.value ?? 0) > 0

  const draftOf = (r: (typeof resolved)[number]): Draft =>
    drafts[r.id] ?? draftFromValue(r.kind, getFact(facts, r.bucket, r.question.fact)?.value)

  const focusFirstInvalid = () =>
    setTimeout(() => formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(), 0)

  const collect = (): { next: typeof facts; count: number } | null => {
    const newErrors: Record<string, string> = {}
    let next = facts
    let count = 0
    for (const r of answerable) {
      const draft = draftOf(r)
      if (isDraftEmpty(r.kind, draft)) continue
      const result = parseDraft(r.kind, draft, r.meta, r.options.map((o) => o.value))
      if (result.ok) {
        next = withFact(next, r.bucket, r.question.fact, userFactOf(result.value))
        count += 1
      } else {
        newErrors[r.id] = result.error
      }
    }
    setErrors(newErrors)
    if (Object.keys(newErrors).length > 0) {
      focusFirstInvalid()
      return null
    }
    return { next, count }
  }

  const saveOnly = () => {
    const result = collect()
    if (!result) {
      setSaved(null)
      return
    }
    if (result.count === 0) {
      setSaved('Nie wpisano jeszcze żadnej odpowiedzi.')
      return
    }
    setFacts(result.next)
    setDrafts({})
    setSaved(`Zapisano odpowiedzi: ${result.count}. Sprawdzam, czy potrzebuję czegoś jeszcze.`)
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const result = collect()
    if (!result) return
    if (result.count > 0) setFacts(result.next)
    if (!result.next.sprawa['zdarzenie.rodzaj'] || !result.next.sprawa['zdarzenie.data']) {
      const missing = !result.next.sprawa['zdarzenie.rodzaj'] ? 'sprawa:zdarzenie.rodzaj' : 'sprawa:zdarzenie.data'
      setErrors((e) => ({ ...e, [missing]: 'Odpowiedz na to pytanie — bez niego nie wiemy, czego dotyczy sprawa.' }))
      focusFirstInvalid()
      return
    }
    navigate(paths.result)
  }

  const setDraft = (id: string, draft: Draft) => {
    setDrafts((d) => ({ ...d, [id]: draft }))
    setSaved(null)
  }

  const changeFact = (bucket: Parameters<typeof withFact>[1], key: string, value: unknown) =>
    setFacts(withFact(facts, bucket, key, userFactOf(value)))

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 px-4 py-6">
      <div ref={pageRef} className="mx-auto max-w-2xl space-y-6">
        <Stepper steps={flowSteps} current={needsConfirmation ? 0 : 1} />
        {needsConfirmation ? (
          <>
            <PageHeader title="Potwierdź pobrane dane" description="Sprawdź dane wybranego profilu lub sprawy. Jeśli trzeba, popraw je przed przejściem dalej." />
            <Card className="space-y-4 p-5 sm:p-7">
              <FactList facts={facts} onChange={changeFact} subset="essential" />
            </Card>
            <Disclosure title="Pozostałe pobrane informacje">
              <FactList facts={facts} onChange={changeFact} subset="other" />
            </Disclosure>
            <div className="flex flex-wrap items-center gap-3">
              <Button className="w-full sm:w-auto" onClick={() => {
                setParams({ potwierdzone: '1' })
                requestAnimationFrame(() => questionsHeading.current?.focus())
              }}>Potwierdź dane</Button>
              <Button asChild variant="link"><Link to={paths.login}>Zmień profil lub sprawę</Link></Button>
            </div>
          </>
        ) : (
          <>
              <h1 ref={questionsHeading} tabIndex={-1} className="text-3xl font-bold tracking-tight sm:text-4xl">Uzupełnij brakujące dane</h1>

              {difficult ? (
                <Alert variant="info" title="Pytamy tylko o to, co niezbędne">
                  Rozumiemy, że to może być trudny czas. Na każde pytanie możesz nie odpowiadać — wtedy pokażemy, czego brakuje do ustalenia
                  świadczenia.
                </Alert>
              ) : null}

              <Card className="p-5 sm:p-7">
                <form ref={formRef} onSubmit={submit} noValidate className="space-y-5">
                  <Section title={hasEvent ? 'Pytania uzupełniające' : 'Pytania o Twoją sytuację'} className="space-y-6">
                    {hasEvent && assessment.isPending ? (
                      <p role="status" className="flex items-center gap-2 text-ink-soft">
                        <Spinner label={null} /> Sprawdzam, czego jeszcze brakuje…
                      </p>
                    ) : null}
                    {assessment.isError ? (
                      <ErrorState
                        error={assessment.error}
                        onRetry={() => void assessment.refetch()}
                        retrying={assessment.isFetching}
                        title="Nie udało się sprawdzić, czego brakuje"
                      />
                    ) : null}

                    {hasEvent && assessment.isSuccess && resolved.length === 0 ? (
                      <p className="text-ink-soft">Mamy wszystkie potrzebne informacje. Możesz przejść do wyniku.</p>
                    ) : null}

                    {answerable.length > 0 ? (
                      <>
                        {hasEvent ? <p className="text-ink-soft">Jeśli pominiesz pytanie, w wyniku pokażemy, których danych brakuje do oceny świadczenia.</p> : null}
                        <ul className="space-y-6" aria-busy={assessment.isFetching}>
                          {answerable.map((r) => (
                            <li key={r.id}>
                              <AnswerField
                                id={`pytanie-${r.id}`}
                                label={r.question.question}
                                kind={r.kind}
                                options={r.options}
                                draft={draftOf(r)}
                                onChange={(d) => setDraft(r.id, d)}
                                error={errors[r.id]}
                                unit={r.meta.unit}
                              />
                            </li>
                          ))}
                        </ul>
                        <div>
                          <Button variant="secondary" onClick={saveOnly} disabled={assessment.isFetching}>
                            Zapisz odpowiedzi i sprawdź ponownie
                          </Button>
                        </div>
                      </>
                    ) : null}

                    {workSteps.length > 0 ? (
                      <Alert variant="warning" title="Brakuje danych o pracy i wynagrodzeniu">
                        <p>Bez nich nie sprawdzimy prawa do zasiłków ani nie policzymy kwot.</p>
                        <ul className="mt-2 space-y-1">
                          {workSteps.map((krok) => (
                            <li key={krok}>
                              <Link to={`${paths.manual}?krok=${krok}`} className="font-semibold text-brand-strong underline underline-offset-4">
                                {krok === 'drugi' ? 'Uzupełnij pracę i wynagrodzenie drugiego rodzica' : 'Uzupełnij pracę i wynagrodzenie'}
                              </Link>
                            </li>
                          ))}
                        </ul>
                      </Alert>
                    ) : null}

                    {unsupported.length > 0 ? (
                      <Alert variant="warning" title="Pytania, na które nie da się tu odpowiedzieć">
                        <ul className="list-disc pl-5">
                          {unsupported.map((r) => (
                            <li key={r.id}>{r.question.question}</li>
                          ))}
                        </ul>
                        <p className="mt-1">W wyniku oznaczymy to jako brak danych.</p>
                      </Alert>
                    ) : null}
                  </Section>

                  <p role="status" className="text-sm text-ink-soft empty:hidden">
                    {saved ?? (assessment.isFetching && assessment.isSuccess ? 'Aktualizuję pytania…' : '')}
                  </p>

                  <Button type="submit" fullWidth className="sm:w-auto" disabled={hasEvent && (assessment.isPending || assessment.isFetching)}>
                    Pokaż wynik
                  </Button>
                </form>
              </Card>
              <Disclosure title="Zobacz lub popraw zapisane dane">
                <FactList facts={facts} onChange={changeFact} />
              </Disclosure>
              {sessionId ? <Button asChild variant="link"><Link to={paths.login}>Zmień profil lub sprawę</Link></Button> : null}
          </>
        )}
      </div>
    </ToneScope>
  )
}
