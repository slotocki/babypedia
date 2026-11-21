import { useId, useMemo, useState } from 'react'
import { Link } from 'react-router'
import type { CaseSummary } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Button, Card, EmptyState, ErrorState, Field, Section, SimulationBadge, TextInput } from '@/ui'
import { decisionLabel, normalize } from './decision'
import type { useLoadCase } from './useLoadCase'

type LoadState = ReturnType<typeof useLoadCase>

export function CaseList({ cases, loader }: { cases: CaseSummary[]; loader: LoadState }) {
  const [query, setQuery] = useState('')
  const statusId = useId()
  const needle = normalize(query.trim())
  const visible = useMemo(() => cases.filter((c) => normalize(c.tytul).includes(needle)), [cases, needle])
  const anyAvailable = cases.some((c) => c.dostepna)

  return (
    <Section title="Sprawy testowe" description={`Liczba spraw: ${cases.length}.`}>
      {!anyAvailable ? (
        <div className="rounded-card border-l-4 border-info bg-info-soft p-4 text-ink">
          <p className="font-semibold">Dane tych spraw nie zostały jeszcze dostarczone.</p>
          <p className="mt-1">Dane przepisujemy z dokumentów, więc na razie żadnej sprawy nie da się załadować.</p>
          <p className="mt-2">
            <Link to={paths.login} className="font-semibold text-brand-strong underline underline-offset-4">
              Wypróbuj na przykładowej osobie
            </Link>{' '}
            (symulacja) — to działa już teraz.
          </p>
        </div>
      ) : null}

      {cases.length > 0 ? (
        <Field label="Szukaj sprawy po tytule">
          <TextInput type="search" value={query} onChange={(event) => setQuery(event.target.value)} autoComplete="off" aria-describedby={statusId} />
        </Field>
      ) : null}
      <p id={statusId} role="status" className="text-sm text-ink-soft">
        {query.trim() ? `Pasuje spraw: ${visible.length} z ${cases.length}.` : ''}
      </p>

      {cases.length === 0 ? (
        <EmptyState title="Brak spraw testowych" description="Lista spraw jest pusta. Możesz dodać plik PDF albo wypróbować przykładową osobę." action={<Link to={paths.login} className="font-semibold text-brand-strong underline underline-offset-4">Wypróbuj na przykładowej osobie</Link>} />
      ) : visible.length === 0 ? (
        <EmptyState title="Żadna sprawa nie pasuje do wyszukiwania" description="Zmień słowa w polu wyszukiwania." action={<Button variant="secondary" onClick={() => setQuery('')}>Wyczyść wyszukiwanie</Button>} />
      ) : (
        <ul className="space-y-3" aria-busy={loader.busy}>
          {visible.map((item) => {
            const loading = loader.state.status === 'loading' && loader.state.caseId === item.id
            return (
              <li key={item.id}>
                <Card tone={item.dostepna ? 'default' : 'muted'} className="space-y-3">
                  <div className="min-w-0 space-y-1">
                    <h3 className="text-base font-semibold text-ink">{item.tytul}</h3>
                    <p className="text-sm text-ink-soft">Oczekiwana decyzja ZUS: {decisionLabel[item.oczekiwanaDecyzja]}</p>
                    <p className="text-sm font-semibold text-ink">
                      Status: {item.dostepna ? 'dostępna' : 'dane sprawy nie zostały jeszcze dostarczone'}
                    </p>
                  </div>
                  {item.dostepna ? (
                    <div className="flex flex-wrap items-center gap-3">
                      <Button
                        variant="secondary"
                        loading={loading}
                        loadingLabel="Ładuję dane sprawy (symulacja)…"
                        disabled={loader.busy}
                        aria-label={`Wybierz sprawę: ${item.tytul}`}
                        onClick={() => void loader.load(item)}
                      >
                        Wybierz sprawę
                      </Button>
                      <SimulationBadge />
                    </div>
                  ) : null}
                  {loader.state.status === 'error' && loader.state.caseId === item.id ? (
                    <ErrorState error={loader.state.error} title="Nie udało się załadować danych sprawy" onRetry={() => void loader.load(item)} />
                  ) : null}
                </Card>
              </li>
            )
          })}
        </ul>
      )}
    </Section>
  )
}
