import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { api } from '@/app/api/client'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { flowSteps } from '@/features/intake/steps'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { Alert, Button, ErrorState, Section, SimulationBanner, Spinner, Stepper, ToneScope, PageHeader } from '@/ui'
import { AnswerField } from './AnswerField'
import { draftFromValue, isDraftEmpty, parseDraft, type Draft } from './answers'
import { FactList } from './FactList'
import { getFact, userFactOf, withFact } from './facts'
import { resolveQuestion } from './questions'

export function ReviewPage() {
  const { facts, setFacts, sessionId } = useCaseState()
  const navigate = useNavigate()
  const formRef = useRef<HTMLFormElement>(null)
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [drafts, setDrafts] = useState<Record<string, Draft>>({})
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [saved, setSaved] = useState<string | null>(null)

  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj'])
  const assessment = useQuery({
    queryKey: ['assessment', facts],
    queryFn: () => api.assess({ facts }),
    enabled: hasEvent,
    placeholderData: keepPreviousData,
  })

  const resolved = (assessment.data?.missingFacts ?? []).map(resolveQuestion)
  const answerable = resolved.filter((r) => r.kind !== 'readonly')
  const unsupported = resolved.filter((r) => r.kind === 'readonly')

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
        <Stepper steps={flowSteps} current={2} />
        <PageHeader
          title="Sprawdź swoje dane"
          description={
            <>
              {sessionId
                ? 'Poniżej są dane, które pobraliśmy z symulowanych systemów, oraz Twoje odpowiedzi. Przy każdej informacji widać, skąd pochodzi.'
                : 'Poniżej są Twoje odpowiedzi. Resztę informacji uzupełnisz w pytaniach pod spodem.'}{' '}
              Jeśli coś się nie zgadza, wybierz „Popraw”.
            </>
          }
        />
        {sessionId ? <SimulationBanner system="Logowanie i pobieranie danych" /> : null}

        {!hasEvent ? (
          <Alert variant="warning" title="Najpierw odpowiedz na trzy pytania">
            Nie wiemy jeszcze, czego dotyczy sprawa.{' '}
            <Link to={paths.start} className="font-semibold text-brand-strong underline underline-offset-4">
              Wróć do trzech pytań
            </Link>
            .
          </Alert>
        ) : (
          <>
            {difficult ? (
              <Alert variant="info" title="Pytamy tylko o to, co niezbędne">
                Rozumiemy, że to może być trudny czas. Na każde pytanie możesz nie odpowiadać — wtedy pokażemy, czego brakuje do ustalenia
                świadczenia.
              </Alert>
            ) : null}

            <Section title="Dane w Twojej sprawie">
              <FactList facts={facts} onChange={changeFact} />
            </Section>

            <form ref={formRef} onSubmit={submit} noValidate className="space-y-6">
              <Section title="Czego jeszcze potrzebujemy" className="space-y-6">
                {assessment.isPending ? (
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

                {assessment.isSuccess && resolved.length === 0 ? (
                  <Alert variant="info" title="Nic więcej nie trzeba">
                    Na tym etapie mamy wszystkie informacje, o które musimy zapytać.
                  </Alert>
                ) : null}

                {answerable.length > 0 ? (
                  <>
                    <p className="text-ink-soft">
                      Te informacje są potrzebne do dokładniejszego wyniku. Możesz pominąć pytanie — wtedy przy świadczeniu zobaczysz, że
                      brakuje danych.
                    </p>
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

              <p role="status" className="min-h-6 text-ink-soft">
                {saved ?? (assessment.isFetching && assessment.isSuccess ? 'Aktualizuję pytania…' : '')}
              </p>

              <Button type="submit" fullWidth className="sm:w-auto">
                Pokaż, co mi przysługuje
              </Button>
            </form>
          </>
        )}
      </div>
    </ToneScope>
  )
}
