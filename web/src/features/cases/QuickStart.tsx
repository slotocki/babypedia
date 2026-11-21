import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Button, Card, ErrorState, Section, SimulationBadge, SkeletonText } from '@/ui'
import { decisionLabel } from './decision'
import { useLoadCase } from './useLoadCase'

/** Szybki start na ekranie początkowym: gotowe sprawy testowe ZUS, które wypełniają cały przebieg jednym kliknięciem. */
export function QuickStart() {
  const cases = useQuery({ queryKey: ['cases'], queryFn: api.cases.list })
  const loader = useLoadCase()
  const available = cases.data?.filter((item) => item.dostepna) ?? []

  if (cases.isSuccess && available.length === 0) return null

  return (
    <Section
      title="Szybki start: gotowy przykład"
      description="Wybierz sprawę testową ZUS. Wczytamy jej dane, a Ty od razu zobaczysz wynik i porównanie z decyzją ZUS."
    >
      {cases.isPending ? <SkeletonText lines={3} label="Wczytuję przykłady…" /> : null}
      {cases.isError ? (
        <ErrorState error={cases.error} title="Nie udało się wczytać przykładów" onRetry={() => void cases.refetch()} retrying={cases.isFetching} />
      ) : null}

      {available.length > 0 ? (
        <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2" aria-busy={loader.busy}>
          {available.map((item) => {
            const loading = loader.state.status === 'loading' && loader.state.caseId === item.id
            return (
              <li key={item.id}>
                <Card className="flex h-full flex-col justify-between gap-3">
                  <div className="min-w-0 space-y-1">
                    <h3 className="text-base font-semibold text-ink">{item.tytul}</h3>
                    <p className="text-sm text-ink-soft">Decyzja ZUS: {decisionLabel[item.oczekiwanaDecyzja]}</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-3">
                    <Button
                      variant="secondary"
                      loading={loading}
                      loadingLabel="Wczytuję dane…"
                      disabled={loader.busy}
                      aria-label={`Wczytaj przykład: ${item.tytul}`}
                      onClick={() => void loader.load(item)}
                    >
                      Wczytaj przykład
                    </Button>
                    <SimulationBadge />
                  </div>
                  {loader.state.status === 'error' && loader.state.caseId === item.id ? (
                    <ErrorState error={loader.state.error} title="Nie udało się wczytać danych" onRetry={() => void loader.load(item)} />
                  ) : null}
                </Card>
              </li>
            )
          })}
        </ul>
      ) : null}

      <p className="text-ink-soft">
        Masz plik PDF sprawy?{' '}
        <Link to={paths.cases} className="font-semibold text-brand-strong underline underline-offset-4">
          Dodaj plik, a rozpoznamy sprawę
        </Link>
        .
      </p>
    </Section>
  )
}
