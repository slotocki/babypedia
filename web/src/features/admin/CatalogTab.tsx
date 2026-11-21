import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { CatalogEntry } from '@/app/api/types'
import { Alert, Card, EmptyState, Section, Stack, Switch } from '@/ui'
import { QueryView } from './QueryView'
import { generalMessage } from './problem'
import { useAdminSession } from './session'

export function CatalogTab() {
  const { password } = useAdminSession()
  const queryClient = useQueryClient()
  const catalog = useQuery({
    queryKey: ['admin', 'catalog'],
    queryFn: () => api.admin.catalog(password),
  })

  const save = useMutation({
    mutationFn: (entry: CatalogEntry) => api.admin.updateCatalog(password, [entry]),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['admin', 'catalog'] }),
  })

  return (
    <Section title="Świadczenia w katalogu" description="Włącz lub wyłącz pozycje katalogu świadczeń. Zmiana zapisuje się od razu.">
      <QueryView query={catalog} loadingLabel="Ładowanie katalogu świadczeń…">
        {(entries) =>
          entries.length === 0 ? (
            <EmptyState title="Katalog jest pusty" />
          ) : (
            <Stack gap="md">
              {save.isError ? (
                <Alert variant="error" title="Nie zapisano zmiany">
                  {generalMessage(save.error)}
                </Alert>
              ) : null}
              <Card>
                <ul className="divide-y divide-line">
                  {entries.map((entry) => {
                    const pendingHere = save.isPending && save.variables?.id === entry.id
                    const checked = pendingHere ? save.variables.wlaczone : entry.wlaczone
                    return (
                      <li key={entry.id}>
                        <Switch
                          label={entry.nazwa}
                          description={`Status: ${checked ? 'włączone' : 'wyłączone'}`}
                          checked={checked}
                          disabled={save.isPending}
                          onChange={(e) =>
                            save.mutate({
                              ...entry,
                              wlaczone: e.target.checked,
                            })
                          }
                        />
                      </li>
                    )
                  })}
                </ul>
              </Card>
            </Stack>
          )
        }
      </QueryView>
    </Section>
  )
}
