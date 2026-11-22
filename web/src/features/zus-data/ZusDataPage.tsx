import { useState, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { ApiError, api } from '@/app/api/client'
import type { ContributionDebt, InsuranceTitle, ZusAccount } from '@/app/api/types'
import { useCaseState, type DataConnection } from '@/app/case-context'
import { paths } from '@/app/paths'
import {
  Alert,
  Button,
  Card,
  CheckIcon,
  CrossIcon,
  ErrorState,
  PageHeader,
  Section,
  SkeletonText,
  SourceBadge,
  formatDate,
  formatDateShort,
  formatMoney,
  formatPercent,
} from '@/ui'
import { ConnectionStatus } from './ConnectionStatus'
import { benefitLabels, formatMonth, sickLeaveCodes, sicknessLabels, titleLabels } from './labels'
import { useFocusHeading } from './useFocusHeading'

const linkClass = 'font-semibold text-brand-strong underline underline-offset-4'
const askPath = `${paths.submissions}?widok=formularz&szablon=zapytanie_ezus`
const certificatePath = `${paths.submissions}?widok=zaswiadczenie`

export function ZusDataPage() {
  const { sessionId, connection } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()

  return (
    <div ref={ref} className="mx-auto max-w-2xl space-y-6">
      <PageHeader
        title="Moje dane w ZUS"
        description="Tak wyglądałyby Twoje dane na koncie ubezpieczonego. Wszystkie dane są fikcyjne."
      />
      {sessionId ? <Account session={sessionId} connection={connection} /> : <NeedsLogin />}
    </div>
  )
}

function NeedsLogin({ expired = false }: { expired?: boolean }) {
  return (
    <Alert variant="info" title={expired ? 'Sesja symulacji wygasła' : 'Dane z konta wymagają zalogowania (symulacja)'}>
      <div className="space-y-3">
        <p>
          {expired
            ? 'Zaloguj się ponownie, żeby zobaczyć dane.'
            : 'Wybrano wpisywanie danych samodzielnie, więc nie mamy konta, z którego można je pobrać. Zaloguj się przez symulowany mObywatel i wybierz fikcyjną osobę.'}
        </p>
        <Button asChild>
          <Link to={paths.login}>Przejdź do logowania (symulacja)</Link>
        </Button>
      </div>
    </Alert>
  )
}

function Account({ session, connection }: { session: string; connection: DataConnection | null }) {
  const { markRefreshed } = useCaseState()
  const query = useQuery({ queryKey: ['sim', 'zus', session], queryFn: () => api.sim.myZus(session) })
  const [refreshed, setRefreshed] = useState(false)
  // Starsze sesje nie mają zapisanej zgody — wtedy pokazujemy wszystko, jak dotąd.
  const shares = (source: DataConnection['sources'][number]) => !connection || connection.sources.includes(source)

  const refresh = async () => {
    setRefreshed(false)
    const result = await query.refetch()
    if (result.isSuccess) {
      markRefreshed()
      setRefreshed(true)
    }
  }

  const status = connection ? (
    <ConnectionStatus
      connection={connection}
      action={
        shares('zus') ? (
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
            <Button variant="secondary" loading={query.isFetching && !query.isPending} loadingLabel="Odświeżam…" onClick={() => void refresh()}>
              Odśwież dane
            </Button>
            <p role="status" className="text-sm text-ink-soft empty:hidden">
              {refreshed && !query.isFetching ? 'Dane odświeżone.' : ''}
            </p>
          </div>
        ) : (
          <Button asChild variant="link">
            <Link to={paths.login}>Połącz ponownie i zmień zgodę</Link>
          </Button>
        )
      }
    />
  ) : null

  if (connection && !shares('zus')) {
    return (
      <div className="space-y-6">
        {status}
        <Alert variant="info" title="Nie udostępniono danych z konta ZUS">
          Przy logowaniu odznaczono konto ubezpieczonego (PUE/eZUS), więc nie pokazujemy tych danych.
        </Alert>
      </div>
    )
  }

  if (query.isPending) return <SkeletonText lines={6} label="Pobieram dane z konta (symulacja)…" />
  if (query.isError) {
    if (query.error instanceof ApiError && query.error.status === 401) return <NeedsLogin expired />
    return <ErrorState error={query.error} onRetry={() => void query.refetch()} retrying={query.isFetching} />
  }

  const zus = query.data
  return (
    <div className="space-y-10">
      {status}
      <Titles titles={zus.tytuly} />
      <Bases bases={zus.podstawySkladek} />
      <Breaks breaks={zus.przerwy} />
      <Benefits benefits={zus.pobraneSwiadczenia} />
      {shares('ezla') ? <SickLeaves leaves={zus.zwolnienia} /> : null}
      <Balance debt={zus.saldo ?? null} />
    </div>
  )
}

function Block({ title, source = 'zus', children }: { title: string; source?: 'zus' | 'ezla'; children: ReactNode }) {
  return (
    <Section title={title} actions={<SourceBadge source={source} />}>
      {children}
    </Section>
  )
}

const period = (from: string, to?: string | null) =>
  to ? `${formatDate(from)} – ${formatDate(to)}` : `od ${formatDate(from)}, trwa nadal`

function Titles({ titles }: { titles: InsuranceTitle[] }) {
  const noSickness = titles.length > 0 && titles.every((t) => t.chorobowe === 'brak')
  return (
    <Block title="Ubezpieczenia i płatnicy">
      {titles.length === 0 ? (
        <Alert variant="warning" title="Brak ubezpieczenia w danych z konta">
          Konto nie pokazuje żadnego tytułu ubezpieczenia. Bez ubezpieczenia chorobowego zasiłek może nie przysługiwać — sprawdź to na ekranie wyniku
          albo zapytaj ZUS.
        </Alert>
      ) : null}
      {noSickness ? (
        <Alert variant="warning" title="Brak ubezpieczenia chorobowego">
          Żaden z tytułów nie obejmuje ubezpieczenia chorobowego. Może to wpłynąć na prawo do zasiłku — zapytaj ZUS, jeśli masz wątpliwości.
        </Alert>
      ) : null}
      <ul className="space-y-3">
        {titles.map((title) => (
          <li key={`${title.rodzajTytulu}-${title.od}-${title.platnikId ?? ''}`}>
            <Card as="article" className="space-y-2">
              <h3 className="text-lg font-bold text-ink">{titleLabels[title.rodzajTytulu]}</h3>
              {title.platnikNazwa ? <p className="text-base text-ink">Płatnik składek: {title.platnikNazwa}</p> : null}
              <p className="text-base text-ink">Okres: {period(title.od, title.do)}</p>
              {title.wymiarEtatu ? (
                <p className="text-base text-ink">Wymiar: {title.wymiarEtatu === 1 ? 'pełny etat' : `${formatPercent(title.wymiarEtatu)} etatu`}</p>
              ) : null}
              {title.kodTytulu ? <p className="text-sm text-ink-soft">Kod tytułu ubezpieczenia: {title.kodTytulu}</p> : null}
              <p
                className={`inline-flex items-center gap-1.5 rounded-pill border px-3 py-1 text-sm font-semibold ${
                  title.chorobowe === 'brak' ? 'border-bad bg-bad-soft text-bad' : 'border-ok bg-ok-soft text-ok'
                }`}
              >
                {title.chorobowe === 'brak' ? <CrossIcon className="size-4 shrink-0" /> : <CheckIcon className="size-4 shrink-0" />}
                {sicknessLabels[title.chorobowe]}
              </p>
            </Card>
          </li>
        ))}
      </ul>
    </Block>
  )
}

function Bases({ bases }: { bases: ZusAccount['podstawySkladek'] }) {
  const sorted = [...bases].sort((a, b) => a.miesiac.localeCompare(b.miesiac))
  const average = sorted.length > 0 ? sorted.reduce((sum, b) => sum + b.kwota, 0) / sorted.length : 0
  return (
    <Block title="Podstawy składek">
      {sorted.length === 0 ? (
        <p className="text-base text-ink">Brak podstaw składek w danych z konta. Bez nich nie da się oszacować wysokości zasiłku.</p>
      ) : (
        <>
          <Card tone="muted" className="space-y-1">
            <p className="text-sm font-semibold text-ink-soft">Średnia z {sorted.length} miesięcy (wyliczona tutaj, nie przez ZUS)</p>
            <p className="text-2xl font-bold tabular-nums text-ink">{formatMoney(average)}</p>
          </Card>
          <ul className="grid gap-2 sm:grid-cols-2">
            {sorted.map((base) => (
              <li key={base.miesiac} className="flex items-baseline justify-between gap-3 rounded-control border border-line bg-surface px-3 py-2">
                <span className="text-base capitalize text-ink">{formatMonth(base.miesiac)}</span>
                <span className="text-base font-semibold tabular-nums text-ink">{formatMoney(base.kwota)}</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </Block>
  )
}

function Breaks({ breaks }: { breaks: ZusAccount['przerwy'] }) {
  return (
    <Block title="Przerwy w ubezpieczeniu">
      {breaks.length === 0 ? (
        <p className="text-base text-ink">Brak przerw w ubezpieczeniu.</p>
      ) : (
        <ul className="space-y-3">
          {breaks.map((item) => (
            <li key={`${item.od}-${item.do}`}>
              <Card as="article" className="space-y-1">
                <p className="text-base font-semibold text-ink">{period(item.od, item.do)}</p>
                {item.opis ? <p className="text-base text-ink-soft">{item.opis}</p> : null}
              </Card>
            </li>
          ))}
        </ul>
      )}
    </Block>
  )
}

function Benefits({ benefits }: { benefits: ZusAccount['pobraneSwiadczenia'] }) {
  return (
    <Block title="Pobierane świadczenia">
      {benefits.length === 0 ? (
        <p className="text-base text-ink">Brak świadczeń pobranych w ostatnim czasie.</p>
      ) : (
        <ul className="space-y-3">
          {benefits.map((item) => (
            <li key={`${item.rodzaj}-${item.od}`}>
              <Card as="article" className="space-y-1">
                <h3 className="text-lg font-bold text-ink">{benefitLabels[item.rodzaj]}</h3>
                <p className="text-base text-ink">{period(item.od, item.do)}</p>
                {item.opis ? <p className="text-base text-ink-soft">{item.opis}</p> : null}
              </Card>
            </li>
          ))}
        </ul>
      )}
    </Block>
  )
}

function SickLeaves({ leaves }: { leaves: ZusAccount['zwolnienia'] }) {
  return (
    <Block title="Zwolnienia lekarskie (e-ZLA)" source="ezla">
      {leaves.length === 0 ? (
        <p className="text-base text-ink">Brak zwolnień lekarskich w danych z konta.</p>
      ) : (
        <ul className="space-y-3">
          {leaves.map((leave) => (
            <li key={`${leave.od}-${leave.do}`}>
              <Card as="article" className="space-y-1">
                <h3 className="text-lg font-bold text-ink">{period(leave.od, leave.do)}</h3>
                <p className="text-base text-ink">
                  Kod: {leave.kod ? (sickLeaveCodes[leave.kod] ?? leave.kod) : 'brak kodu'}
                </p>
                <p className="text-base text-ink">Pobyt w szpitalu: {leave.szpital ? 'tak' : 'nie'}</p>
                {leave.dataWystawienia ? <p className="text-base text-ink">Data wystawienia: {formatDateShort(leave.dataWystawienia)}</p> : null}
              </Card>
            </li>
          ))}
        </ul>
      )}
    </Block>
  )
}

function Balance({ debt }: { debt: ContributionDebt | null }) {
  const owes = debt !== null && debt.kwota > 0
  return (
    <Block title="Saldo składek">
      {debt === null ? (
        <p className="text-base text-ink">Konto nie wykazuje salda składek (brak danych w symulacji).</p>
      ) : owes ? (
        <Alert variant="warning" title={`Zaległe składki: ${formatMoney(debt.kwota)}`}>
          Stan na dzień {formatDate(debt.naDzien)}. Zadłużenie może wpłynąć na prawo do zasiłku — szczegóły w wyniku oceny.
        </Alert>
      ) : (
        <Alert variant="info" title="Brak zaległych składek">
          Stan na dzień {formatDate(debt.naDzien)}.
        </Alert>
      )}
      <p className="text-base text-ink">
        <Link to={certificatePath} className={linkClass}>
          Pobierz zaświadczenie o stanie składek (symulacja)
        </Link>
      </p>
      <p className="text-base text-ink">
        Masz wątpliwości co do danych?{' '}
        <Link to={askPath} className={linkClass}>
          Zadaj pytanie do ZUS (symulacja)
        </Link>
      </p>
    </Block>
  )
}
