import { useMutation, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '@/app/api/client'
import { emptyFacts, useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { mergeIntegrationFacts } from '@/features/review/facts'
import { findKind, submissionFormPath } from '@/features/submissions/kinds'
import { Button, ErrorState, PageHeader, Spinner, UserIcon } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

export function LoginPage() {
  const { replaceCase, connectProfile } = useCaseState()
  const navigate = useNavigate()
  const pageRef = useFocusHeading<HTMLDivElement>()
  const [params] = useSearchParams()
  const [selected, setSelected] = useState<string | null>(null)
  const requestedKind = findKind(params.get('szablon'))
  const forSubmission = params.get('powrot') === 'wnioski'
  const personas = useQuery({ queryKey: ['sim', 'personas'], queryFn: api.sim.personas })
  const profiles = (personas.data ?? []).filter((person) => !person.caseId)

  const connection = useMutation({
    mutationFn: async (personaId: string) => {
      const session = await api.sim.login(personaId)
      const profileName = [session.identity.imie, session.identity.nazwisko].filter(Boolean).join(' ')
      const facts = forSubmission ? emptyFacts() : mergeIntegrationFacts(emptyFacts(), await api.sim.myFacts(session.sessionId))
      return { sessionId: session.sessionId, profileName, facts }
    },
    onSuccess: ({ sessionId, profileName, facts }) => {
      if (forSubmission) {
        connectProfile(sessionId, profileName)
        navigate(requestedKind ? submissionFormPath(requestedKind.id) : paths.submissions, { replace: true })
      } else {
        replaceCase(facts, sessionId, null, profileName)
        navigate(paths.review)
      }
    },
  })

  return (
    <div ref={pageRef} className="mx-auto max-w-4xl space-y-6 py-3 sm:py-7">
      <PageHeader
        title={forSubmission ? 'Wybierz profil do wniosku' : 'Wybierz profil'}
        description={forSubmission ? 'Zachowamy dane wpisane w kalkulatorze i uzupełnimy imię oraz nazwisko z wybranego profilu.' : 'Z wybranego profilu pobierzemy dostępne informacje. Potem możesz je sprawdzić i poprawić.'}
      />
      <p className="text-sm text-ink-soft">To połączenie demonstracyjne. Profile zawierają dane fikcyjne.</p>
      {personas.isPending ? <p role="status" className="flex items-center gap-2"><Spinner label={null} /> Wczytuję profile…</p> : null}
      {personas.isError ? <ErrorState error={personas.error} title="Nie udało się wczytać profili" onRetry={() => void personas.refetch()} /> : null}
      {personas.isSuccess && profiles.length === 0 ? <p>Brak profili do wyboru. Możesz wprowadzić dane ręcznie.</p> : null}
      {profiles.length > 0 ? (
        <form className="space-y-6" onSubmit={(event) => {
          event.preventDefault()
          if (selected && !connection.isPending) connection.mutate(selected)
        }}>
          <fieldset disabled={connection.isPending} className="grid min-w-0 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <legend className="sr-only">Dostępne profile</legend>
            {profiles.map((person) => (
              <label key={person.id} className="relative flex min-w-0 cursor-pointer items-start gap-3 rounded-card border border-line bg-surface p-4 hover:border-brand has-[:checked]:border-brand has-[:checked]:bg-brand-soft has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand">
                <input type="radio" name="profil" value={person.id} checked={selected === person.id} onChange={() => { setSelected(person.id); connection.reset() }} className="sr-only" />
                <span className="grid size-9 shrink-0 place-items-center rounded-full bg-canvas text-brand-strong"><UserIcon className="size-5" /></span>
                <span className="min-w-0 flex-1">
                  <span className="block font-bold">{person.imie ?? person.id}</span>
                  <span className="block text-sm text-ink-soft">{person.opis}</span>
                </span>
                <span className="mt-1 size-4 shrink-0 rounded-full border border-line-strong" aria-hidden="true">
                  {selected === person.id ? <span className="m-0.5 block size-2.5 rounded-full bg-brand" /> : null}
                </span>
              </label>
            ))}
          </fieldset>
          {connection.isError ? <ErrorState error={connection.error} title="Nie udało się pobrać danych" /> : null}
          <Button type="submit" disabled={!selected} loading={connection.isPending} loadingLabel="Pobieram dane…" className="w-full sm:w-auto">
            {forSubmission ? 'Przejdź do wniosku' : 'Pobierz dane i przejdź dalej'}
          </Button>
        </form>
      ) : null}
      <div className="flex flex-wrap gap-x-4">
        <Button asChild variant="link"><Link to={forSubmission ? paths.result : paths.start}>Wróć</Link></Button>
        {!forSubmission ? <Button asChild variant="link"><Link to={paths.cases}>Sprawdź na przykładach ZUS</Link></Button> : null}
      </div>
    </div>
  )
}
