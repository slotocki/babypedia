import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { api } from '@/app/api/client'
import type { Identity, PersonaListItem } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { countFacts, mergeIntegrationFacts, withoutIntegrationFacts } from '@/features/review/facts'
import { Alert, Button, Card, EmptyState, ErrorState, PageHeader, Section, Spinner, Stepper, UserIcon } from '@/ui'
import { flowSteps } from './steps'
import { useFocusHeading } from './useFocusHeading'

const maskPesel = (pesel: string) => `${'•'.repeat(Math.max(pesel.length - 3, 0))}${pesel.slice(-3)}`

type Progress =
  | { state: 'idle' }
  | { state: 'loading'; persona: string }
  | { state: 'error'; error: unknown; persona: PersonaListItem }
  | { state: 'done'; identity: Identity; factCount: number }

export function LoginPage() {
  const { facts, setFacts, setSessionId } = useCaseState()
  const navigate = useNavigate()
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [progress, setProgress] = useState<Progress>({ state: 'idle' })

  // Osoby ze spraw testowych ZUS wybiera się na ekranie początkowym; tutaj zostają tylko osoby wymyślone.
  const personas = useQuery({ queryKey: ['sim', 'personas'], queryFn: api.sim.personas, select: (list) => list.filter((p) => !p.caseId) })
  const busy = progress.state === 'loading'

  const login = async (persona: PersonaListItem) => {
    setProgress({ state: 'loading', persona: persona.imie ?? persona.id })
    try {
      const session = await api.sim.login(persona.id)
      const integration = await api.sim.myFacts(session.sessionId)
      const merged = mergeIntegrationFacts(facts, integration)
      setSessionId(session.sessionId)
      setFacts(merged)
      setProgress({ state: 'done', identity: session.identity, factCount: countFacts(merged) })
    } catch (error) {
      setProgress({ state: 'error', error, persona })
    }
  }

  const enterManually = () => {
    setSessionId(null)
    setFacts(withoutIntegrationFacts(facts))
    navigate(paths.review)
  }

  const missingEvent = !facts.sprawa['zdarzenie.rodzaj']

  return (
    <div ref={pageRef} className="mx-auto max-w-2xl space-y-6">
      <Stepper steps={flowSteps} current={1} />
      <PageHeader title="Jak chcesz uzupełnić dane?" />

      {missingEvent ? (
        <Alert variant="warning" title="Brakuje odpowiedzi na trzy pytania">
          <Link to={paths.start} className="font-semibold text-brand-strong underline underline-offset-4">
            Wróć do trzech pytań
          </Link>
          , żeby wynik był trafny. Możesz też wybrać osobę — jej dane uzupełnią odpowiedzi.
        </Alert>
      ) : null}

      {progress.state === 'done' ? (
        <div className="space-y-4" role="status">
          <Alert variant="info" title="Zalogowano (symulacja)">
            <p>
              {progress.identity.imie} {progress.identity.nazwisko}
              {progress.identity.nazwiskoRodowe ? `, nazwisko rodowe: ${progress.identity.nazwiskoRodowe}` : ''}
            </p>
            <p>PESEL (zamaskowany, fikcyjny): {maskPesel(progress.identity.pesel)}</p>
            <p className="mt-2">Pobrano dane z symulowanych systemów. Liczba danych do sprawdzenia: {progress.factCount}.</p>
          </Alert>
          <Button autoFocus fullWidth className="sm:w-auto" onClick={() => navigate(paths.review)}>
            Dalej: sprawdź dane
          </Button>
        </div>
      ) : (
        <>
          <Card>
            <Section title="Pobierz dane osoby testowej" description="Symulacja logowania przez mObywatel i pobierania danych z ZUS.">
              {personas.isPending ? (
                <p role="status" className="flex items-center gap-2 text-ink-soft">
                  <Spinner label={null} /> Wczytuję listę osób…
                </p>
              ) : null}
              {personas.isError ? (
                <ErrorState error={personas.error} onRetry={() => void personas.refetch()} retrying={personas.isFetching} title="Nie udało się wczytać listy osób" />
              ) : null}
              {personas.isSuccess && personas.data.length === 0 ? (
                <EmptyState title="Brak osób do wyboru" description="Lista osób jest pusta. Możesz wprowadzić dane samodzielnie." />
              ) : null}

              {personas.isSuccess && personas.data.length > 0 ? (
                <ul className="divide-y divide-line" aria-busy={busy}>
                  {personas.data.map((persona) => (
                    <li key={persona.id} className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 py-3">
                      <div className="flex min-w-0 flex-[1_1_12rem] items-start gap-3">
                        <span className="grid size-9 shrink-0 place-items-center rounded-full bg-brand-soft text-brand-strong">
                          <UserIcon className="size-5" />
                        </span>
                        <div className="min-w-0">
                          <p className="font-semibold text-ink">{persona.imie ?? persona.id}</p>
                          <p className="text-sm text-ink-soft">{persona.opis}</p>
                        </div>
                      </div>
                      <Button
                        variant="link"
                        disabled={busy}
                        aria-label={`Pobierz dane: ${persona.imie ?? persona.id} (symulacja)`}
                        onClick={() => void login(persona)}
                        className="shrink-0"
                      >
                        Pobierz dane
                      </Button>
                    </li>
                  ))}
                </ul>
              ) : null}

              {progress.state === 'loading' ? (
                <p role="status" className="flex items-center gap-2 text-ink-soft">
                  <Spinner label={null} /> Loguję (symulacja) i pobieram dane: {progress.persona}…
                </p>
              ) : null}
              {progress.state === 'error' ? (
                <ErrorState
                  error={progress.error}
                  title="Nie udało się zalogować"
                  onRetry={() => void login(progress.persona)}
                  description="Spróbuj ponownie albo wprowadź dane samodzielnie. Twoje odpowiedzi są zachowane."
                />
              ) : null}
            </Section>
          </Card>
          <Button variant="secondary" fullWidth className="sm:w-auto" onClick={enterManually} disabled={busy}>
            Wprowadzę dane samodzielnie
          </Button>
        </>
      )}
    </div>
  )
}
