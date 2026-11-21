import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { FactSource, IntegrationField } from '@/app/api/types'
import { Alert, Card, EmptyState, Section, Stack, Switch } from '@/ui'
import { QueryView } from './QueryView'
import { generalMessage } from './problem'
import { useAdminSession } from './session'

const sourceNames: Record<FactSource, string> = {
  mobywatel: 'mObywatel',
  zus: 'ZUS (PUE)',
  ezla: 'e-ZLA',
  usc: 'USC (akt urodzenia)',
  user: 'Użytkownik',
  assumption: 'Założenie',
}

const factNames: Record<string, string> = {
  'osoba.dataUrodzenia': 'Data urodzenia osoby',
  'ubezpieczenie.tytuly': 'Tytuły ubezpieczenia',
  'ubezpieczenie.podstawySkladek': 'Podstawy składek',
  'ubezpieczenie.przerwy': 'Przerwy w ubezpieczeniu',
  'ubezpieczenie.pobraneSwiadczenia': 'Pobrane świadczenia',
  'platnik.liczbaUbezpieczonych': 'Liczba ubezpieczonych u płatnika',
  'skladki.zadluzenie': 'Zadłużenie składkowe',
  'ezla.zwolnienia': 'Zwolnienia lekarskie',
  'dziecko.dataUrodzenia': 'Data urodzenia dziecka',
  'dziecko.liczba': 'Liczba dzieci',
}

const sameField = (a: IntegrationField, b: IntegrationField) => a.source === b.source && a.fact === b.fact

export function IntegrationsTab() {
  const { password } = useAdminSession()
  const queryClient = useQueryClient()
  const fields = useQuery({
    queryKey: ['admin', 'integrations'],
    queryFn: () => api.admin.integrations(password),
  })

  const save = useMutation({
    mutationFn: (field: IntegrationField) => api.admin.updateIntegrations(password, [field]),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['admin', 'integrations'] }),
  })

  return (
    <Section
      title="Pola integracji"
      description="Pola pobierane automatycznie z zewnętrznych źródeł. Wyłączone pole stanie się pytaniem do użytkownika."
    >
      <QueryView query={fields} loadingLabel="Ładowanie pól integracji…">
        {(items) =>
          items.length === 0 ? (
            <EmptyState title="Brak pól integracji" />
          ) : (
            <Stack gap="md">
              {save.isError ? (
                <Alert variant="error" title="Nie zapisano zmiany">
                  {generalMessage(save.error)}
                </Alert>
              ) : null}
              <Card>
                <ul className="divide-y divide-line">
                  {items.map((field) => {
                    const pendingHere = save.isPending && sameField(save.variables, field)
                    const checked = pendingHere ? save.variables.wlaczone : field.wlaczone
                    return (
                      <li key={`${field.source}/${field.fact}`}>
                        <Switch
                          label={`${sourceNames[field.source] ?? field.source}: ${factNames[field.fact] ?? field.fact}`}
                          description={
                            checked
                              ? 'Status: włączone — pole pobierane automatycznie ze źródła.'
                              : 'Status: wyłączone — użytkownik zostanie zapytany o tę wartość.'
                          }
                          checked={checked}
                          disabled={save.isPending}
                          onChange={(e) =>
                            save.mutate({
                              ...field,
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
