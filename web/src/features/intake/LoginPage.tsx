import { useMutation, useQuery } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import type { FactSet } from '@/app/api/types'
import { emptyFacts, useCaseState, type DataConnection, type DataSource } from '@/app/case-context'
import { paths } from '@/app/paths'
import { rememberExampleAnswers } from '@/features/review/examples'
import { mergeIntegrationFacts } from '@/features/review/facts'
import { findKind, submissionFormPath } from '@/features/submissions/kinds'
import { dataSources } from '@/features/zus-data/labels'
import { Button, Checkbox, ErrorState, PageHeader, SimulationBanner, Spinner, UserIcon } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

const allSources: DataSource[] = dataSources.map((source) => source.id)

/** Opisy profili zaczynają się od „Wymyślona osoba…” — o fikcyjności mówi już baner. */
const shortDescription = (opis: string) => {
  const text = opis.replace(/^Wymyślona osoba(?:\s*\([^)]*\))?(?::\s*|\s+)/u, '')
  return text.charAt(0).toLocaleUpperCase('pl-PL') + text.slice(1)
}

/** Usuwa dane ze źródeł, na które użytkownik nie wyraził zgody (dane z mObywatela zostają). */
function onlySharedSources(facts: FactSet, sources: DataSource[]): FactSet {
  const keep = (map: object | undefined) =>
    Object.fromEntries(
      Object.entries(map ?? {}).filter(([, fact]) => {
        const source = (fact as { source?: string } | undefined)?.source
        return !allSources.includes(source as DataSource) || sources.includes(source as DataSource)
      }),
    )
  const result = { sprawa: keep(facts.sprawa), wnioskodawca: keep(facts.wnioskodawca) } as FactSet
  if (facts.drugiRodzic) result.drugiRodzic = keep(facts.drugiRodzic) as FactSet['drugiRodzic']
  return result
}

export function LoginPage() {
  const { replaceCase, connectProfile } = useCaseState()
  const navigate = useNavigate()
  const pageRef = useFocusHeading<HTMLDivElement>()
  const consentHeading = useRef<HTMLHeadingElement>(null)
  const [params] = useSearchParams()
  const [selected, setSelected] = useState<string | null>(null)
  const [step, setStep] = useState<'profile' | 'consent'>('profile')
  const [sources, setSources] = useState<DataSource[]>(allSources)
  const requestedKind = findKind(params.get('szablon'))
  const forSubmission = params.get('powrot') === 'wnioski'
  const personas = useQuery({ queryKey: ['sim', 'personas'], queryFn: api.sim.personas })
  const profiles = (personas.data ?? []).filter((person) => !person.caseId)
  const selectedProfile = profiles.find((person) => person.id === selected)

  const firstStep = useRef(true)
  useEffect(() => {
    if (firstStep.current) {
      firstStep.current = false
      return
    }
    // Po zmianie kroku fokus na nagłówek: zgody albo strony (po powrocie do wyboru osoby).
    if (step === 'consent') consentHeading.current?.focus()
    else pageRef.current?.querySelector('h1')?.focus()
  }, [step, pageRef])

  const connection = useMutation({
    mutationFn: async ({ personaId, shared }: { personaId: string; shared: DataSource[] }) => {
      const session = await api.sim.login(personaId)
      const profileName = [session.identity.imie, session.identity.nazwisko].filter(Boolean).join(' ')
      const facts = forSubmission
        ? emptyFacts()
        : mergeIntegrationFacts(emptyFacts(), onlySharedSources(await api.sim.myFacts(session.sessionId), shared))
      const link: DataConnection = { connectedAt: new Date().toISOString(), sources: shared }
      return { sessionId: session.sessionId, profileName, facts, link }
    },
    onSuccess: ({ sessionId, profileName, facts, link }) => {
      if (forSubmission) {
        connectProfile(sessionId, profileName, link)
        navigate(requestedKind ? submissionFormPath(requestedKind.id) : paths.submissions, { replace: true })
      } else {
        // Odpowiedzi zapisane w pliku osoby testowej oznaczamy na „Sprawdź dane” jako przykład do poprawienia.
        rememberExampleAnswers(sessionId, facts)
        replaceCase(facts, sessionId, null, profileName, link)
        navigate(paths.review)
      }
    },
  })

  const toggleSource = (id: DataSource, on: boolean) => {
    setSources((current) => allSources.filter((source) => (source === id ? on : current.includes(source))))
    connection.reset()
  }

  return (
    <div ref={pageRef} className="mx-auto max-w-4xl space-y-6 py-3 sm:py-7">
      <PageHeader
        title="Zaloguj się przez mObywatela"
        description={
          forSubmission
            ? 'Wybierz fikcyjną osobę testową. Zachowamy dane wpisane w kalkulatorze i uzupełnimy imię oraz nazwisko z jej profilu.'
            : 'Wybierz fikcyjną osobę testową. W prawdziwym wdrożeniu w tym miejscu nastąpiłoby przekierowanie do mObywatela.'
        }
      />
      {step === 'profile' ? (
        <>
          <SimulationBanner>Logowanie przez mObywatela nie łączy się z prawdziwym systemem. Wszystkie osoby są fikcyjne.</SimulationBanner>
          {personas.isPending ? <p role="status" className="flex items-center gap-2"><Spinner label={null} /> Wczytuję osoby testowe…</p> : null}
          {personas.isError ? <ErrorState error={personas.error} title="Nie udało się wczytać osób testowych" onRetry={() => void personas.refetch()} /> : null}
          {personas.isSuccess && profiles.length === 0 ? <p>Brak osób testowych do wyboru. Możesz wprowadzić dane ręcznie.</p> : null}
          {profiles.length > 0 ? (
            <form className="space-y-6" onSubmit={(event) => {
              event.preventDefault()
              if (selected) setStep('consent')
            }}>
              <fieldset className="grid min-w-0 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                <legend className="sr-only">Osoby testowe</legend>
                {profiles.map((person) => (
                  <label key={person.id} className="relative flex min-w-0 cursor-pointer items-start gap-3 rounded-card border border-line bg-surface p-4 hover:border-brand has-[:checked]:border-brand has-[:checked]:bg-brand-soft has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand">
                    <input type="radio" name="profil" value={person.id} checked={selected === person.id} onChange={() => { setSelected(person.id); connection.reset() }} className="sr-only" />
                    <span className="grid size-9 shrink-0 place-items-center rounded-full bg-canvas text-brand-strong"><UserIcon className="size-5" /></span>
                    <span className="min-w-0 flex-1">
                      <span className="block font-bold">{person.imie ?? person.id}</span>
                      <span className="block text-sm text-ink-soft">{shortDescription(person.opis)}</span>
                    </span>
                    <span className="mt-1 size-4 shrink-0 rounded-full border border-line-strong" aria-hidden="true">
                      {selected === person.id ? <span className="m-0.5 block size-2.5 rounded-full bg-brand" /> : null}
                    </span>
                  </label>
                ))}
              </fieldset>
              <Button type="submit" disabled={!selected} className="w-full sm:w-auto">
                Zaloguj przez mObywatela
              </Button>
            </form>
          ) : null}
        </>
      ) : (
        <section aria-labelledby="zgoda-naglowek" className="space-y-5">
          <SimulationBanner>
            Połączenie z PUE/eZUS, e-ZLA i rejestrem stanu cywilnego jest udawane. Pobierzemy fikcyjne dane osoby testowej.
          </SimulationBanner>
          <div className="space-y-2">
            <h2 id="zgoda-naglowek" ref={consentHeading} tabIndex={-1} className="text-xl font-bold text-ink focus:outline-none">
              Zgoda na pobranie danych
            </h2>
            <p className="text-base text-ink-soft">
              Zalogowano jako <strong className="text-ink">{selectedProfile?.imie ?? selected}</strong> (symulacja mObywatela). Zaznacz, skąd możemy
              pobrać dane.
            </p>
          </div>
          <form className="space-y-5" onSubmit={(event) => {
            event.preventDefault()
            if (selected && sources.length > 0 && !connection.isPending) connection.mutate({ personaId: selected, shared: sources })
          }}>
            <fieldset disabled={connection.isPending} className="min-w-0 space-y-1 rounded-card border border-line bg-surface p-4">
              <legend className="px-1 text-base font-bold text-ink">Udostępnij dane z:</legend>
              {dataSources.map((source) => (
                <Checkbox
                  key={source.id}
                  name="zrodla"
                  value={source.id}
                  checked={sources.includes(source.id)}
                  onChange={(event) => toggleSource(source.id, event.target.checked)}
                  label={<span className="font-semibold">{source.label}</span>}
                  description={source.description}
                />
              ))}
            </fieldset>
            <p id="zgoda-podpowiedz" role="status" className="text-base text-warn empty:hidden">
              {sources.length === 0 ? 'Zaznacz co najmniej jedno źródło, żeby pobrać dane.' : ''}
            </p>
            {connection.isError ? <ErrorState error={connection.error} title="Nie udało się pobrać danych" /> : null}
            <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
              <Button
                type="submit"
                disabled={sources.length === 0}
                aria-describedby={sources.length === 0 ? 'zgoda-podpowiedz' : undefined}
                loading={connection.isPending}
                loadingLabel="Łączę i pobieram dane…"
                className="w-full sm:w-auto"
              >
                {forSubmission ? 'Połącz i przejdź do wniosku (symulacja)' : 'Połącz i pobierz dane (symulacja)'}
              </Button>
              <Button type="button" variant="link" disabled={connection.isPending} onClick={() => { setStep('profile'); connection.reset() }}>
                Zmień osobę
              </Button>
            </div>
          </form>
        </section>
      )}
      <div className="flex flex-wrap gap-x-4">
        <Button asChild variant="link"><Link to={forSubmission ? paths.result : paths.start}>Wróć</Link></Button>
        {!forSubmission ? <Button asChild variant="link"><Link to={paths.cases}>Sprawdź na przykładach ZUS</Link></Button> : null}
      </div>
    </div>
  )
}
