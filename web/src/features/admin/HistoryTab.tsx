import { useQuery } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { Parameter } from '@/app/api/types'
import { Card, EmptyState, Section, Stack, formatDate } from '@/ui'
import { QueryView } from './QueryView'
import { useAdminSession } from './session'
import { formatDateTime, formatParameterValue } from './values'

export function HistoryTab() {
  const { password } = useAdminSession()
  const history = useQuery({
    queryKey: ['admin', 'history'],
    queryFn: () => api.admin.parameterHistory(password),
  })
  const parameters = useQuery({
    queryKey: ['admin', 'parameters'],
    queryFn: () => api.admin.parameters(password),
  })

  const units = new Map<string, Parameter['unit']>()
  for (const p of parameters.data ?? []) if (!units.has(p.key)) units.set(p.key, p.unit)

  return (
    <Section
      title="Historia zmian"
      description="Najnowsze zmiany na górze. „Obowiązuje od” to data, od której nowa wartość działa w obliczeniach."
    >
      <QueryView query={history} loadingLabel="Ładowanie historii zmian…">
        {(entries) =>
          entries.length === 0 ? (
            <EmptyState title="Brak zmian" description="Nikt jeszcze nie zmienił żadnego parametru." />
          ) : (
            <Stack as="ul" gap="md" className="list-none">
              {[...entries]
                .sort((a, b) => b.changedAt.localeCompare(a.changedAt))
                .map((entry, index) => {
                  const unit = units.get(entry.key)
                  return (
                    <li key={`${entry.changedAt}-${entry.key}-${index}`}>
                      <Card as="div">
                        <Stack gap="sm">
                          <p className="text-sm text-ink-soft">
                            <span className="font-semibold text-ink">Zmieniono: </span>
                            {formatDateTime(entry.changedAt)}
                          </p>
                          <p className="break-words font-mono text-base font-semibold text-ink">{entry.key}</p>
                          <p className="text-ink">
                            <span className="text-ink-soft">Wartość: </span>
                            {formatParameterValue(entry.previousValue, unit)} <span aria-hidden="true">→</span>
                            <span className="sr-only"> zmieniona na </span> <strong>{formatParameterValue(entry.value, unit)}</strong>
                          </p>
                          <p className="text-ink">
                            <span className="text-ink-soft">Obowiązuje od: </span>
                            {formatDate(entry.validFrom)}
                          </p>
                          <p className="text-ink">
                            <span className="text-ink-soft">Powód: </span>
                            {entry.reason ? entry.reason : 'nie podano'}
                          </p>
                        </Stack>
                      </Card>
                    </li>
                  )
                })}
            </Stack>
          )
        }
      </QueryView>
    </Section>
  )
}
