import { useCallback, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { Parameter } from '@/app/api/types'
import { Button, PageHeader, Stack, Tabs } from '@/ui'
import { CatalogTab } from './CatalogTab'
import { HistoryTab } from './HistoryTab'
import { IntegrationsTab } from './IntegrationsTab'
import { LoginForm } from './LoginForm'
import { OrganizationsTab } from './OrganizationsTab'
import { ParametersTab } from './ParametersTab'
import { AdminSessionContext } from './session'

export function AdminPage() {
  const queryClient = useQueryClient()
  const [password, setPassword] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | undefined>()

  const lock = useCallback(() => {
    setPassword(null)
    queryClient.removeQueries({ queryKey: ['admin'] })
  }, [queryClient])

  const session = useMemo(() => (password === null ? null : { password, lock }), [password, lock])

  if (!session) {
    return (
      <LoginForm
        notice={notice}
        onUnlock={(value, parameters: Parameter[]) => {
          queryClient.setQueryData(['admin', 'parameters'], parameters)
          setNotice(undefined)
          setPassword(value)
        }}
      />
    )
  }

  return (
    <AdminSessionContext value={session}>
      <Stack gap="lg">
        <PageHeader
          title="Panel administratora"
          description="Zmiana parametru obowiązuje od wskazanej daty i od razu wpływa na obliczenia za okresy od tej daty."
          action={
            <Button
              variant="secondary"
              onClick={() => {
                setNotice('Wylogowano z panelu administratora.')
                lock()
              }}
            >
              Wyloguj z panelu
            </Button>
          }
        />
        <Tabs
          label="Sekcje panelu administratora"
          items={[
            {
              value: 'parametry',
              label: 'Parametry',
              content: <ParametersTab />,
            },
            {
              value: 'historia',
              label: 'Historia zmian',
              content: <HistoryTab />,
            },
            {
              value: 'swiadczenia',
              label: 'Świadczenia',
              content: <CatalogTab />,
            },
            {
              value: 'integracje',
              label: 'Integracje',
              content: <IntegrationsTab />,
            },
            {
              value: 'organizacje',
              label: 'Organizacje',
              content: <OrganizationsTab />,
            },
          ]}
        />
      </Stack>
    </AdminSessionContext>
  )
}
