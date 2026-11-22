import type { ReactNode } from 'react'
import type { DataConnection } from '@/app/case-context'
import { CheckIcon, CrossIcon, cn } from '@/ui'
import { dataSources } from './labels'

const formatTime = (iso: string) => {
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? null : date.toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' })
}

export function ConnectionStatus({ connection, action, className }: { connection: DataConnection; action?: ReactNode; className?: string }) {
  const time = formatTime(connection.connectedAt)
  const withZus = connection.sources.includes('zus')

  return (
    <section aria-label="Stan połączenia" className={cn('space-y-3 rounded-card border border-line bg-surface p-4', className)}>
      <p
        className={cn(
          'inline-flex max-w-full flex-wrap items-center gap-x-1.5 rounded-pill border px-3 py-1 text-sm font-semibold',
          withZus ? 'border-ok bg-ok-soft text-ok' : 'border-line-strong bg-neutral-soft text-ink-soft',
        )}
      >
        {withZus ? <CheckIcon className="size-4 shrink-0" /> : <CrossIcon className="size-4 shrink-0" />}
        <span>{withZus ? 'Połączono z PUE/eZUS (symulacja)' : 'Bez połączenia z PUE/eZUS (symulacja)'}</span>
        {time ? <span>· pobrano o {time}</span> : null}
      </p>
      <div>
        <p className="text-sm font-semibold text-ink-soft">Źródła danych</p>
        <ul className="mt-1 space-y-1">
          {dataSources.map((source) => {
            const on = connection.sources.includes(source.id)
            return (
              <li key={source.id} className={cn('flex items-start gap-2 text-base', on ? 'text-ink' : 'text-ink-soft')}>
                {on ? <CheckIcon className="mt-1 size-4 shrink-0 text-ok" /> : <CrossIcon className="mt-1 size-4 shrink-0" />}
                <span className="min-w-0">
                  {source.label}
                  {on ? <span className="sr-only"> — połączono</span> : ' — nie udostępniono'}
                </span>
              </li>
            )
          })}
        </ul>
      </div>
      {action}
    </section>
  )
}
