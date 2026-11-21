import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Button, Disclosure, ErrorState, SkeletonText } from '@/ui'
import { agreementLabel, decisionLabel } from './decision'
import { useLoadCase } from './useLoadCase'

/** Szybki start na ekranie początkowym: gotowe sprawy testowe ZUS, które wypełniają cały przebieg jednym kliknięciem. */
export function QuickStart() {
  const cases = useQuery({ queryKey: ['cases'], queryFn: api.cases.list })
  const loader = useLoadCase()
  const order = { kwota: 0, okres: 1, decyzja: 2 }
  const rank = (item: { zgodnosc?: keyof typeof order | null }) => (item.zgodnosc ? order[item.zgodnosc] : 3)
  const available = (cases.data?.filter((item) => item.dostepna) ?? []).sort((a, b) => rank(a) - rank(b))

  if (cases.isSuccess && available.length === 0) return null

  return (
    <Disclosure title="Sprawy testowe ZUS" summary="Zobacz przykładowy wynik na danych testowych">
      <div className="space-y-3">
        {cases.isPending ? <SkeletonText lines={3} label="Wczytuję przykłady…" /> : null}
        {cases.isError ? (
          <ErrorState error={cases.error} title="Nie udało się wczytać przykładów" onRetry={() => void cases.refetch()} retrying={cases.isFetching} />
        ) : null}

        {available.length > 0 ? (
          <ul className="divide-y divide-line" aria-busy={loader.busy}>
            {available.map((item) => {
              const loading = loader.state.status === 'loading' && loader.state.caseId === item.id
              return (
                <li key={item.id}>
                  <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 py-2">
                    <div className="min-w-0 space-y-1">
                      <h3 className="text-base font-semibold text-ink">{item.tytul}</h3>
                      <p className="text-sm text-ink-soft">Decyzja ZUS: {decisionLabel[item.oczekiwanaDecyzja]}</p>
                      {item.zgodnosc ? <p className="text-sm font-semibold text-ink">{agreementLabel[item.zgodnosc]}</p> : null}
                    </div>
                    <Button
                      variant="link"
                      loading={loading}
                      loadingLabel="Wczytuję dane…"
                      disabled={loader.busy}
                      aria-label={`Zobacz wynik: ${item.tytul}`}
                      onClick={() => void loader.load(item)}
                    >
                      Zobacz wynik
                    </Button>
                  </div>
                  {loader.state.status === 'error' && loader.state.caseId === item.id ? (
                    <ErrorState error={loader.state.error} title="Nie udało się wczytać danych" onRetry={() => void loader.load(item)} />
                  ) : null}
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
      </div>
    </Disclosure>
  )
}
