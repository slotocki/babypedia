import { Link } from 'react-router'
import type { CaseSummary } from '@/app/api/types'
import { paths } from '@/app/paths'
import { EmptyState, ErrorState, Spinner, cn } from '@/ui'
import { agreementLabel, decisionLabel } from './decision'
import type { useLoadCase } from './useLoadCase'

type LoadState = ReturnType<typeof useLoadCase>

/** Imię osoby ze sprawy (z identyfikatora, np. „zus-004-agnieszka-war” → „Agnieszka”). */
function firstName(id: string) {
  const name = id.split('-')[2]
  return name ? name.charAt(0).toLocaleUpperCase('pl') + name.slice(1) : null
}

const tile =
  'group flex h-full w-full flex-col items-start gap-2 rounded-card border p-4 text-left transition-colors focus-visible:outline-offset-4 sm:p-5'

export function CaseList({ cases, loader }: { cases: CaseSummary[]; loader: LoadState }) {
  if (cases.length === 0) {
    return (
      <EmptyState
        title="Brak spraw testowych"
        description="Lista spraw jest pusta. Możesz wypróbować przykładową osobę."
        action={<Link to={paths.login} className="font-semibold text-brand-strong underline underline-offset-4">Wypróbuj na przykładowej osobie</Link>}
      />
    )
  }

  return (
    <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3" aria-busy={loader.busy}>
      {cases.map((item) => {
        const loading = loader.state.status === 'loading' && loader.state.caseId === item.id
        const refused = item.oczekiwanaDecyzja === 'odmowa'
        const name = firstName(item.id)
        return (
          <li key={item.id} className="min-w-0">
            <button
              type="button"
              disabled={!item.dostepna || loader.busy}
              onClick={() => void loader.load(item)}
              aria-label={`Wybierz sprawę: ${item.tytul}`}
              className={cn(
                tile,
                item.dostepna
                  ? 'border-line bg-surface hover:border-brand hover:bg-brand-soft/40 disabled:cursor-wait'
                  : 'cursor-not-allowed border-line bg-canvas text-ink-soft',
              )}
            >
              <span className="flex w-full flex-wrap items-center gap-2">
                <span
                  className={cn(
                    'rounded-pill border px-2.5 py-0.5 text-xs font-semibold',
                    refused ? 'border-bad bg-bad-soft text-bad' : 'border-ok bg-ok-soft text-ok',
                  )}
                >
                  ZUS: {decisionLabel[item.oczekiwanaDecyzja]}
                </span>
                {name ? <span className="text-xs font-semibold text-ink-soft">{name}</span> : null}
              </span>
              <span className="text-base font-semibold leading-snug text-ink first-letter:uppercase">{item.tytul}</span>
              {item.zgodnosc ? <span className="text-sm text-ink-soft">{agreementLabel[item.zgodnosc]}</span> : null}
              <span className="mt-auto pt-1 text-sm font-bold text-brand-strong">
                {loading ? (
                  <span className="inline-flex items-center gap-2">
                    <Spinner label={null} /> Wczytuję dane sprawy…
                  </span>
                ) : item.dostepna ? (
                  <>
                    Sprawdź tę sprawę <span aria-hidden="true">→</span>
                  </>
                ) : (
                  'Dane sprawy nie zostały jeszcze dostarczone'
                )}
              </span>
            </button>
            {loader.state.status === 'error' && loader.state.caseId === item.id ? (
              <div className="mt-2">
                <ErrorState error={loader.state.error} title="Nie udało się załadować danych sprawy" onRetry={() => void loader.load(item)} />
              </div>
            ) : null}
          </li>
        )
      })}
    </ul>
  )
}
