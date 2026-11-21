import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { ApiError, api } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Button, Card, EmptyState, ErrorState, SimulationBadge, SkeletonText } from '@/ui'
import { submissionPdfPath } from './api'
import { dateTimeLabel, recipientLabels, statusLabel, typeLabels } from './kinds'
import { NeedsLogin } from './NeedsLogin'
import { PdfActions } from './PdfActions'

export function MySubmissions({ session }: { session: string }) {
  const query = useQuery({
    queryKey: ['submissions', 'mine', session],
    queryFn: () => api.submissions.list(session),
    refetchOnMount: 'always',
  })

  if (query.isPending) return <SkeletonText />
  if (query.isError) {
    if (query.error instanceof ApiError && query.error.status === 401) return <NeedsLogin expired />
    return <ErrorState error={query.error} onRetry={() => void query.refetch()} retrying={query.isFetching} />
  }

  const items = [...query.data].sort((a, b) => b.submittedAt.localeCompare(a.submittedAt))

  if (items.length === 0) {
    return (
      <EmptyState
        title="Nie masz jeszcze żadnych zgłoszeń"
        description="Gdy złożysz wniosek (symulacja), pojawi się tutaj z numerem i plikiem PDF."
        action={
          <Button asChild>
            <Link to={paths.submissions}>Wybierz wniosek</Link>
          </Button>
        }
      />
    )
  }

  return (
    <ul className="space-y-4">
      {items.map((item) => (
        <li key={item.id}>
          <Card as="article" className="space-y-3" aria-label={`Zgłoszenie ${item.number}`}>
            <div className="flex flex-wrap items-center gap-2">
              <SimulationBadge />
              <span className="rounded-pill border border-ok bg-ok-soft px-3 py-0.5 text-sm font-semibold text-ok">{statusLabel(item.status)}</span>
            </div>
            <h2 className="break-words text-lg font-bold text-ink">{item.number}</h2>
            <dl className="grid gap-2 sm:grid-cols-3">
              <Item label="Rodzaj" value={typeLabels[item.type]} />
              <Item label="Adresat" value={recipientLabels[item.recipient]} />
              <Item label="Data złożenia" value={dateTimeLabel(item.submittedAt)} />
            </dl>
            {item.summary ? <p className="text-base text-ink">{item.summary}</p> : null}
            <PdfActions path={item.pdfUrl ?? submissionPdfPath(item.id)} fileName={`${item.number}.pdf`} session={session} withOpen />
          </Card>
        </li>
      ))}
    </ul>
  )
}

function Item({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-sm font-semibold text-ink-soft">{label}</dt>
      <dd className="text-base text-ink">{value}</dd>
    </div>
  )
}
