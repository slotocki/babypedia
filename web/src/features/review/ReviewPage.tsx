import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import type { CaseFacts, FactSet, MissingFact, PersonFacts, PersonRef } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { flowSteps } from '@/features/intake/steps'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { Alert, Button, Card, Disclosure, ErrorState, Section, Spinner, Stepper, ToneScope, PageHeader } from '@/ui'
import { AnswerField } from './AnswerField'
import { draftFromValue, isDraftEmpty, parseDraft, type Draft } from './answers'
import { eventDateIssue } from './dates'
import { FactList } from './FactList'
import { getFact, userFactOf, withFact, type AnyFact, type Bucket } from './facts'
import { resolveQuestion, type ResolvedQuestion } from './questions'

/** Podstawowe pytania o zdarzenie jako brakujące fakty — gdy wejście nie przeszło przez kreator (np. „Sprawy ZUS”), a integracje nie dały zdarzenia. */
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

/** Pytanie o datę zdarzenia brzmi zależnie od odpowiedzi „Co się dzieje”. */
const eventDateQuestion: Record<string, string> = {
  planuje: 'Kiedy planujesz, że dziecko się urodzi? (jeśli wiesz)',
  ciaza: 'Kiedy masz termin porodu?',
  urodzenie: 'Kiedy urodziło się dziecko?',
  przyjecie: 'Kiedy dziecko zostało przyjęte do Twojej rodziny?',
}

/** Fakty z kroku „Praca i ubezpieczenie” (/wprowadz-dane), których nie da się wpisać na tym ekranie. */
const workFacts = ['ubezpieczenie.tytuly', 'ubezpieczenie.podstawySkladek', 'skladki.zadluzenie']

/** Odpowiedzi tekstowe (kwoty, liczby) zapisujemy po opuszczeniu pola; wybór z listy i datę — od razu. */
const immediateKinds = new Set(['enum', 'boolean', 'date', 'sytuacjaMatki'])

const without = (map: Record<string, string>, key: string) => Object.fromEntries(Object.entries(map).filter(([k]) => k !== key))

const questionId = (m: MissingFact) => `${m.person ?? 'sprawa'}:${m.fact}`

const plural = (n: number, one: string, few: string, many: string) =>
  n === 1 ? one : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? few : many

function missingEventQuestions(facts: FactSet, fromEngine: MissingFact[]): MissingFact[] {
  return eventQuestions.filter(
    (q) => facts.sprawa[q.fact as keyof FactSet['sprawa']] === undefined && !fromEngine.some((m) => m.fact === q.fact && !m.person),
  )
}

export function ReviewPage() {
  const { facts, setFacts, setCaseFact, setPersonFact, sessionId, caseId } = useCaseState()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [notice, setNotice] = useState<string | null>(null)
  const [params, setParams] = useSearchParams()
  const formRef = useRef<HTMLFormElement>(null)
  const pageRef = useFocusHeading<HTMLDivElement>()
  const questionsHeading = useRef<HTMLHeadingElement>(null)
  const [drafts, setDrafts] = useState<Record<string, Draft>>({})
  const [errors, setErrors] = useState<Record<string, string>>({})
  // Pytania pokazane na tym ekranie (w kolejności pojawienia się). Pytanie, na które już odpowiedziano, zostaje widoczne,
  // żeby można było zmienić odpowiedź; nowe pytania wynikające z odpowiedzi dochodzą na końcu listy.
  const [asked, setAsked] = useState<MissingFact[]>([])

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
  const currentMissing = [...missingEventQuestions(facts, engineMissing), ...engineMissing]
  const fresh = currentMissing.filter((m) => !asked.some((a) => questionId(a) === questionId(m)))
  if (fresh.length > 0) setAsked([...asked, ...fresh])

  const rodzaj = facts.sprawa['zdarzenie.rodzaj']?.value
  const resolve = (m: MissingFact): ResolvedQuestion => {
    const r = resolveQuestion(m)
    if (m.fact === 'zdarzenie.data' && !m.person && rodzaj && eventDateQuestion[rodzaj]) {
      return { ...r, question: { ...m, question: eventDateQuestion[rodzaj] } }
    }
    return r
  }

  const stillMissing = new Set(currentMissing.map(questionId))
  const resolved = [...asked, ...fresh].map(resolve)
  const answeredHere = (r: ResolvedQuestion) =>
    drafts[r.id] !== undefined || getFact(facts, r.bucket, r.question.fact)?.source === 'user'
  const answerable = resolved.filter((r) => r.kind !== 'readonly' && (stillMissing.has(r.id) || answeredHere(r)))
  const readonlyQuestions = resolved.filter((r) => r.kind === 'readonly' && stillMissing.has(r.id))
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

  const draftOf = (r: ResolvedQuestion): Draft =>
    drafts[r.id] ?? draftFromValue(r.kind, getFact(facts, r.bucket, r.question.fact)?.value)

  const unanswered = answerable.filter((r) => isDraftEmpty(r.kind, draftOf(r)) && getFact(facts, r.bucket, r.question.fact) === undefined)

  const focusFirstInvalid = () =>
    setTimeout(() => formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(), 0)

  const storeFact = (bucket: Bucket, key: string, fact: AnyFact | undefined) => {
    if (bucket === 'sprawa') setCaseFact(key as keyof CaseFacts, fact as never)
    else setPersonFact(bucket as PersonRef, key as keyof PersonFacts, fact as never)
  }

  /** Odpowiedź z pola → fakt albo komunikat. Pusta odpowiedź = brak faktu (pytanie zostaje bez odpowiedzi). */
  const check = (r: ResolvedQuestion, draft: Draft): { ok: true; value: unknown } | { ok: false; error: string } | null => {
    if (isDraftEmpty(r.kind, draft)) return null
    const result = parseDraft(r.kind, draft, r.meta, r.options.map((o) => o.value))
    if (result.ok && r.question.fact === 'zdarzenie.data' && !r.question.person) {
      const problem = eventDateIssue(rodzaj, String(result.value))
      if (problem) return { ok: false, error: problem }
    }
    return result
  }

  const apply = (r: ResolvedQuestion, draft: Draft, showError: boolean) => {
    const result = check(r, draft)
    const current = getFact(facts, r.bucket, r.question.fact)
    if (result === null) {
      if (current?.source === 'user') storeFact(r.bucket, r.question.fact, undefined)
      setErrors((e) => without(e, r.id))
      return
    }
    if (result.ok) {
      if (JSON.stringify(current?.value) !== JSON.stringify(result.value) || current?.source !== 'user')
        storeFact(r.bucket, r.question.fact, userFactOf(result.value))
      setErrors((e) => without(e, r.id))
    } else if (showError) {
      setErrors((e) => ({ ...e, [r.id]: result.error }))
    }
  }

  const setDraft = (r: ResolvedQuestion, draft: Draft) => {
    setDrafts((d) => ({ ...d, [r.id]: draft }))
    // Pole daty przekazuje niepełną lub błędną datę dopiero po opuszczeniu pola — wtedy pokazujemy komunikat.
    if (immediateKinds.has(r.kind)) apply(r, draft, Boolean(errors[r.id]) || r.kind === 'date')
    else if (errors[r.id]) {
      const result = check(r, draft)
      if (result === null || result.ok) setErrors((e) => without(e, r.id))
    }
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setNotice(null)
    const newErrors: Record<string, string> = {}
    let next = facts
    for (const r of answerable) {
      const draft = drafts[r.id]
      if (draft === undefined) continue
      const result = check(r, draft)
      if (result === null) continue
      if (result.ok) next = withFact(next, r.bucket, r.question.fact, userFactOf(result.value))
      else newErrors[r.id] = result.error
    }
    if (!next.sprawa['zdarzenie.rodzaj'] || !next.sprawa['zdarzenie.data']) {
      const missing = !next.sprawa['zdarzenie.rodzaj'] ? 'sprawa:zdarzenie.rodzaj' : 'sprawa:zdarzenie.data'
      newErrors[missing] ??= 'Odpowiedz na to pytanie, jeśli możesz — bez niego nie wiemy, czego dotyczy sprawa.'
    }
    setErrors(newErrors)
    if (Object.keys(newErrors).length > 0) {
      focusFirstInvalid()
      return
    }
    setFacts(next)
    // Ostatnia odpowiedź mogła otworzyć kolejne pytanie (np. „umowa na czas określony: tak” → „do kiedy?”) — pokaż je, zanim przejdziesz dalej.
    if (next.sprawa['zdarzenie.rodzaj']) {
      const checked = await queryClient
        .fetchQuery({ queryKey: ['assessment', next], queryFn: () => api.assess({ facts: next }) })
        .catch(() => null)
      const followUps = (checked?.missingFacts ?? []).filter(
        (m) => !asked.some((a) => questionId(a) === questionId(m)) && resolveQuestion(m).kind !== 'readonly',
      )
      if (followUps.length > 0) {
        setAsked((current) => [...current, ...followUps.filter((m) => !current.some((a) => questionId(a) === questionId(m)))])
        setNotice(
          followUps.length === 1
            ? 'Twoja odpowiedź wymaga jeszcze jednego pytania — jest na końcu listy.'
            : `Twoje odpowiedzi wymagają jeszcze ${followUps.length} pytań — są na końcu listy.`,
        )
        setTimeout(() => formRef.current?.querySelector<HTMLElement>(`[data-question="${questionId(followUps[0])}"] input:not([hidden]), [data-question="${questionId(followUps[0])}"] select`)?.focus(), 50)
        return
      }
    }
    navigate(paths.result)
  }

  const changeFact = (bucket: Parameters<typeof withFact>[1], key: string, value: unknown) =>
    setFacts(withFact(facts, bucket, key, userFactOf(value)))

  const title = answerable.length === 0 && hasEvent ? 'Sprawdź i pokaż wynik' : hasImportedFacts ? 'Kilka pytań, których ZUS nie zna' : 'Kilka pytań uzupełniających'
  const updating = hasEvent && assessment.isFetching && assessment.isSuccess

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 px-4 py-6">
      <div ref={pageRef} className="mx-auto max-w-2xl space-y-6">
        <Stepper steps={flowSteps} current={needsConfirmation ? 0 : 1} />
        {needsConfirmation ? (
          <>
            <PageHeader
              title="Sprawdź swoje dane"
              description="To dane pobrane dla wybranej osoby i jej odpowiedzi. Jeśli coś się nie zgadza, popraw to przed przejściem dalej."
            />
            <Card className="space-y-4 p-5 sm:p-7">
              <FactList facts={facts} onChange={changeFact} subset="essential" sessionId={sessionId} />
            </Card>
            <Disclosure title="Pozostałe pobrane informacje">
              <FactList facts={facts} onChange={changeFact} subset="other" sessionId={sessionId} />
            </Disclosure>
            <div className="flex flex-wrap items-center gap-3">
              <Button className="w-full sm:w-auto" onClick={() => {
                setParams({ potwierdzone: '1' })
                requestAnimationFrame(() => questionsHeading.current?.focus())
              }}>Dane się zgadzają — dalej</Button>
              <Button asChild variant="link"><Link to={paths.login}>Zmień profil lub sprawę</Link></Button>
            </div>
          </>
        ) : (
          <>
              <h1 ref={questionsHeading} tabIndex={-1} className="text-3xl font-bold tracking-tight sm:text-4xl">{title}</h1>

              {difficult ? (
                <Alert variant="info" title="Bardzo nam przykro z powodu Twojej straty">
                  <p>
                    To może być bardzo trudny czas. Pytamy tylko o to, co niezbędne, i na każde pytanie możesz nie odpowiadać — wtedy pokażemy,
                    czego brakuje do ustalenia świadczenia.
                  </p>
                  <p className="mt-2">
                    <Link to={paths.support} className="font-semibold text-brand-strong underline underline-offset-4">
                      Zobacz, gdzie szukać wsparcia
                    </Link>
                  </p>
                </Alert>
              ) : null}

              <Card className="p-5 sm:p-7">
                <form ref={formRef} onSubmit={(event) => void submit(event)} noValidate className="space-y-5">
                  <Section title={hasEvent ? 'Pytania, na które potrzebujemy odpowiedzi' : 'Pytania o Twoją sytuację'} className="space-y-6">
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

                    {hasEvent && assessment.isSuccess && answerable.length === 0 && readonlyQuestions.length === 0 ? (
                      <p className="text-ink-soft">Mamy wszystkie potrzebne informacje. Możesz przejść do wyniku.</p>
                    ) : null}

                    {answerable.length > 0 ? (
                      <>
                        <p className="text-ink-soft">
                          Jeśli odpowiedź wymaga doprecyzowania, kolejne pytanie pojawi się od razu poniżej.
                          {hasEvent ? ' Pytanie możesz pominąć — w wyniku pokażemy, których danych brakuje.' : ''}
                        </p>
                        <ul className="space-y-6" aria-busy={assessment.isFetching}>
                          {answerable.map((r) => (
                            <li
                              key={r.id}
                              data-question={r.id}
                              onBlur={(event) => {
                                if (immediateKinds.has(r.kind) || event.currentTarget.contains(event.relatedTarget as Node | null)) return
                                const draft = drafts[r.id]
                                if (draft !== undefined) apply(r, draft, true)
                              }}
                            >
                              <AnswerField
                                id={`pytanie-${r.id}`}
                                label={r.question.question}
                                kind={r.kind}
                                options={r.options}
                                draft={draftOf(r)}
                                onChange={(d) => setDraft(r, d)}
                                error={errors[r.id]}
                                unit={r.meta.unit}
                              />
                            </li>
                          ))}
                        </ul>
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
                    {notice ?? (updating
                      ? 'Sprawdzam, czy odpowiedź wymaga jeszcze jednego pytania…'
                      : unanswered.length > 0 && hasEvent && assessment.isSuccess
                        ? `Bez odpowiedzi: ${unanswered.length} ${plural(unanswered.length, 'pytanie', 'pytania', 'pytań')}. Możesz przejść dalej — w wyniku pokażemy, czego brakuje.`
                        : '')}
                  </p>

                  <Button type="submit" fullWidth className="sm:w-auto" disabled={hasEvent && assessment.isPending}>
                    Pokaż wynik
                  </Button>
                </form>
              </Card>
              <Disclosure title="Zobacz lub popraw zapisane dane">
                <FactList facts={facts} onChange={changeFact} sessionId={sessionId} />
              </Disclosure>
              {sessionId ? <Button asChild variant="link"><Link to={paths.login}>Zmień profil lub sprawę</Link></Button> : null}
          </>
        )}
      </div>
    </ToneScope>
  )
}
