import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import type { Identity, PersonaListItem } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { countFacts, mergeIntegrationFacts, withoutIntegrationFacts } from '@/features/review/facts'
import { Button } from './components/Button'
import { SimulationBadge } from './components/Badges'
import { Alert, ErrorState, Spinner } from './components/Feedback'
import { PageHeading } from './components/PageHeading'
import { loadPersonas, simFacts, simLogin } from './sim'

const maskPesel = (pesel: string) => `${'•'.repeat(Math.max(pesel.length - 3, 0))}${pesel.slice(-3)}`

type Progress =
  | { state: 'idle' }
  | { state: 'loading'; persona: string }
  | { state: 'error'; error: unknown; personaId: string }
  | { state: 'done'; identity: Identity; factCount: number }

export function LoginPage() {
  const { facts, setFacts, setSessionId } = useCaseState()
  const navigate = useNavigate()
  const [progress, setProgress] = useState<Progress>({ state: 'idle' })

  const personas = useQuery({ queryKey: ['sim', 'personas'], queryFn: loadPersonas })

  const login = async (persona: PersonaListItem) => {
    setProgress({ state: 'loading', persona: persona.imie ?? persona.id })
    try {
      const session = await simLogin(persona.id)
      setSessionId(session.sessionId)
      const integration = await simFacts(session.sessionId)
      const merged = mergeIntegrationFacts(facts, integration)
      setFacts(merged)
      setProgress({ state: 'done', identity: session.identity, factCount: countFacts(merged) })
    } catch (error) {
      setProgress({ state: 'error', error, personaId: persona.id })
    }
  }

  const enterManually = () => {
    setSessionId(null)
    setFacts(withoutIntegrationFacts(facts))
    navigate(paths.review)
  }

  const missingEvent = !facts.sprawa['zdarzenie.rodzaj']

  return (
    <section aria-labelledby="page-title" className="mx-auto max-w-2xl">
      <PageHeading step="Krok 2 z 3">Pobierz dane, żeby nie wpisywać ich ręcznie</PageHeading>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <SimulationBadge />
        <span className="text-slate-800">Logowanie przez mObywatel (symulacja)</span>
      </div>
      <p className="mt-2 text-slate-800">
        To tylko symulacja: nie łączymy się z prawdziwym mObywatelem ani z ZUS. Poniższe osoby są wymyślone. Po zalogowaniu pokażemy dane,
        które „pobraliśmy”, a Ty je sprawdzisz.
      </p>

      {missingEvent ? (
        <div className="mt-4">
          <Alert tone="warning" title="Brakuje odpowiedzi na trzy pytania">
            <Link to={paths.start} className="underline underline-offset-4">
              Wróć do trzech pytań
            </Link>
            , żeby wynik był trafny. Możesz też wybrać osobę — jej dane uzupełnią odpowiedzi.
          </Alert>
        </div>
      ) : null}

      {progress.state === 'done' ? (
        <div className="mt-6 space-y-4">
          <Alert tone="info" title="Zalogowano (symulacja)">
            <p>
              {progress.identity.imie} {progress.identity.nazwisko}
              {progress.identity.nazwiskoRodowe ? `, nazwisko rodowe: ${progress.identity.nazwiskoRodowe}` : ''}
            </p>
            <p>PESEL (zamaskowany): {maskPesel(progress.identity.pesel)}</p>
            <p className="mt-2">Pobrano dane z symulowanych systemów. Liczba danych do sprawdzenia: {progress.factCount}.</p>
          </Alert>
          <Button autoFocus className="w-full sm:w-auto" onClick={() => navigate(paths.review)}>
            Dalej: sprawdź dane
          </Button>
        </div>
      ) : (
        <div className="mt-6">
          <h2 className="text-lg font-semibold">Wybierz osobę (symulacja)</h2>

          {personas.isPending ? <Spinner label="Wczytuję listę osób…" /> : null}
          {personas.isError ? (
            <div className="mt-3">
              <ErrorState error={personas.error} onRetry={() => void personas.refetch()} title="Nie udało się wczytać listy osób" />
            </div>
          ) : null}
          {personas.isSuccess && personas.data.length === 0 ? (
            <div className="mt-3">
              <Alert tone="info" title="Brak osób do wyboru">
                Lista osób jest pusta. Możesz wprowadzić dane samodzielnie.
              </Alert>
            </div>
          ) : null}

          {personas.isSuccess && personas.data.length > 0 ? (
            <ul className="mt-3 space-y-3" aria-busy={progress.state === 'loading'}>
              {personas.data.map((persona) => (
                <li key={persona.id}>
                  <button
                    type="button"
                    disabled={progress.state === 'loading'}
                    onClick={() => void login(persona)}
                    className="flex min-h-11 w-full flex-col items-start rounded-lg border border-slate-500 bg-white p-4 text-left hover:bg-teal-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    <span className="text-base font-semibold text-slate-900">Zaloguj jako {persona.imie ?? persona.id}</span>
                    <span className="text-sm text-slate-700">{persona.opis}</span>
                  </button>
                </li>
              ))}
            </ul>
          ) : null}

          {progress.state === 'loading' ? <Spinner label={`Loguję (symulacja) i pobieram dane: ${progress.persona}…`} /> : null}
          {progress.state === 'error' ? (
            <div className="mt-3">
              <ErrorState
                error={progress.error}
                title="Nie udało się zalogować"
                onRetry={() => {
                  const again = personas.data?.find((p) => p.id === progress.personaId)
                  if (again) void login(again)
                }}
              />
            </div>
          ) : null}

          <div className="mt-8 border-t border-slate-200 pt-6">
            <h2 className="text-lg font-semibold">Nie chcesz się logować?</h2>
            <p className="mt-1 text-slate-800">Odpowiesz na kilka pytań sam. Niczego nie pobieramy z żadnych systemów.</p>
            <Button variant="secondary" className="mt-3 w-full sm:w-auto" onClick={enterManually} disabled={progress.state === 'loading'}>
              Wprowadzę dane sam
            </Button>
          </div>
        </div>
      )}
    </section>
  )
}
