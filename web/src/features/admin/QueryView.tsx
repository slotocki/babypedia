import type { ReactNode } from 'react'
import type { UseQueryResult } from '@tanstack/react-query'
import { Button, ErrorState, SkeletonText } from '@/ui'
import { errorStatus } from './problem'
import { useAdminSession } from './session'

interface QueryViewProps<T> {
  query: UseQueryResult<T>
  loadingLabel: string
  children: (data: T) => ReactNode
}

export function QueryView<T>({ query, loadingLabel, children }: QueryViewProps<T>) {
  const { lock } = useAdminSession()

  if (query.isPending) return <SkeletonText lines={4} label={loadingLabel} />

  if (query.isError) {
    if (errorStatus(query.error) === 401) {
      return (
        <div className="space-y-3">
          <ErrorState error={query.error} title="Hasło administratora jest nieprawidłowe lub sesja wygasła" />
          <Button variant="secondary" onClick={lock}>
            Wpisz hasło ponownie
          </Button>
        </div>
      )
    }
    return <ErrorState error={query.error} onRetry={() => void query.refetch()} retrying={query.isFetching} />
  }

  return <>{children(query.data)}</>
}
