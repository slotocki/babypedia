import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import { emptyFacts, useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { mergeIntegrationFacts, userFactOf, withFact } from '@/features/review/facts'
import { eventKindLabels } from '@/features/review/fact-labels'
import { findKind, submissionFormPath } from '@/features/submissions/kinds'
import { Alert, Button, Card, ErrorState, Field, PageHeader, Select, ShieldIcon, SimulationBadge, Spinner } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

export function LoginPage() {
  const { caseId, replaceCase, setSessionId } = useCaseState()
  const navigate = useNavigate()
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [params] = useSearchParams()
  const [selected, setSelected] = useState<string | null>(null)
  const requestedKind = findKind(params.get('szablon'))
  const forSubmission = params.get('powrot') === 'wnioski'

  const personas = useQuery({ queryKey: ['sim', 'personas'], queryFn: api.sim.personas })
  const cases = useQuery({ queryKey: ['cases'], queryFn: api.cases.list, enabled: !forSubmission })
  const profiles = (personas.data ?? []).filter((person) => !person.caseId)
  const examples = forSubmission ? [] : (cases.data ?? []).filter((item) => item.dostepna && item.persona)
  const picked = selected ?? examples.find((item) => item.id === caseId)?.persona ??
    profiles.find((person) => person.id === 'anna-przyklad')?.id ?? profiles[0]?.id ?? examples[0]?.persona ?? ''
  const profile = profiles.find((person) => person.id === picked)
  const example = examples.find((item) => item.persona === picked)
  const selectionReady = selected !== null || (personas.isFetched && (forSubmission || cases.isFetched))

  const connection = useQuery({
    queryKey: ['sim', 'connection', picked, forSubmission],
    enabled: Boolean(picked) && selectionReady,
    staleTime: Infinity,
    gcTime: 0,
    retry: false,
    queryFn: async () => {
      const session = await api.sim.login(picked)
      const loaded = forSubmission ? emptyFacts() : mergeIntegrationFacts(emptyFacts(), await api.sim.myFacts(session.sessionId))
      const facts = example && !loaded.sprawa['zdarzenie.dlaKogo']
        ? withFact(loaded, 'sprawa', 'zdarzenie.dlaKogo', userFactOf('ja'))
        : loaded
      return { sessionId: session.sessionId, facts }
    },
  })

  const proceed = () => {
    if (!connection.data) return
    if (forSubmission) {
      setSessionId(connection.data.sessionId)
      navigate(requestedKind ? submissionFormPath(requestedKind.id) : paths.submissions, { replace: true })
      return
    }
    replaceCase(connection.data.facts, connection.data.sessionId, example?.id ?? null)
    navigate(paths.review)
  }

  const event = connection.data?.facts.sprawa['zdarzenie.rodzaj']?.value
  const date = connection.data?.facts.sprawa['zdarzenie.data']?.value

  return (
    <div ref={pageRef} className="mx-auto max-w-2xl space-y-5 py-3 sm:py-7">
      <PageHeader
        title={forSubmission ? 'Wybierz profil do wniosku' : 'Połącz z mObywatelem'}
        description={forSubmission ? 'Profil testowy służy do zapisania i pobrania dokumentu. Użyjemy danych z Twojego wyniku.' : 'Wybierz profil lub zapisaną sprawę, a wczytamy dane z symulowanych systemów.'}
      />
      <Card className="space-y-5 p-5 sm:p-8">
        <SimulationBadge />
        {personas.isError ? <ErrorState error={personas.error} title="Nie udało się wczytać profili" onRetry={() => void personas.refetch()} /> : null}
        {cases.isError ? <ErrorState error={cases.error} title="Nie udało się wczytać spraw ZUS" onRetry={() => void cases.refetch()} /> : null}

        {picked ? (
          <Field label={forSubmission ? 'Profil testowy' : 'Profil lub sprawa testowa'}>
            <Select value={picked} onChange={(event) => setSelected(event.target.value)}>
              {profiles.length > 0 ? (
                <optgroup label="Profile testowe">
                  {profiles.map((person) => <option key={person.id} value={person.id}>{person.imie ?? person.id}</option>)}
                </optgroup>
              ) : null}
              {examples.length > 0 ? (
                <optgroup label="Sprawy testowe ZUS">
                  {examples.map((item) => <option key={item.id} value={item.persona ?? ''}>{item.tytul}</option>)}
                </optgroup>
              ) : null}
            </Select>
          </Field>
        ) : null}

        {!selectionReady || (picked && connection.isPending) ? (
          <p role="status" className="flex items-center gap-2 text-ink-soft"><Spinner label={null} /> Wczytuję dane testowe…</p>
        ) : null}
        {selectionReady && !picked && !personas.isError && !cases.isError ? (
          <Alert variant="info" title="Brak dostępnych przykładów">Możesz skorzystać z ręcznego wprowadzania danych.</Alert>
        ) : null}
        {connection.isError ? <ErrorState error={connection.error} title="Nie udało się pobrać danych" onRetry={() => void connection.refetch()} /> : null}

        {connection.data ? (
          <>
            <div className="flex items-start gap-3 rounded-card bg-canvas p-4">
              <ShieldIcon className="mt-0.5 size-5 shrink-0 text-brand-strong" />
              <div className="min-w-0 space-y-1">
                <p className="font-semibold text-ink">{example?.tytul ?? profile?.imie ?? 'Profil testowy'}</p>
                {profile?.opis ? <p className="text-sm text-ink-soft">{profile.opis}</p> : null}
                {event ? <p className="text-sm text-ink-soft">{eventKindLabels[event]}{date ? ` · ${new Intl.DateTimeFormat('pl-PL', { dateStyle: 'long', timeZone: 'UTC' }).format(new Date(date))}` : ''}</p> : null}
              </div>
            </div>
            <Button fullWidth className="sm:w-auto" onClick={proceed}>
              {forSubmission ? 'Przejdź do wniosku' : 'Sprawdź pobrane dane'}
            </Button>
          </>
        ) : null}
      </Card>
      <Button asChild variant="link">
        <Link to={forSubmission ? paths.result : paths.start}>{forSubmission ? 'Wróć do wyniku' : 'Wróć do wyboru sposobu'}</Link>
      </Button>
    </div>
  )
}
