import { useState, type FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { Parameter } from '@/app/api/types'
import { Alert, Button, Card, Field, PageHeader, Stack, TextInput } from '@/ui'
import { errorStatus, generalMessage } from './problem'

interface LoginFormProps {
  onUnlock: (password: string, parameters: Parameter[]) => void
  notice?: string
}

export function LoginForm({ onUnlock, notice }: LoginFormProps) {
  const [password, setPassword] = useState('')
  const [submittedEmpty, setSubmittedEmpty] = useState(false)

  const login = useMutation({
    mutationFn: (value: string) => api.admin.parameters(value),
    onSuccess: (parameters, value) => onUnlock(value, parameters),
  })

  const status = errorStatus(login.error)
  let fieldError: string | undefined
  if (submittedEmpty && !password) fieldError = 'Wpisz hasło administratora.'
  else if (status === 401) fieldError = 'Nieprawidłowe hasło administratora.'

  let panelError: string | null = null
  if (status === 503) panelError = 'Panel administratora jest wyłączony na serwerze (nie ustawiono hasła).'
  else if (login.isError && status !== 401) panelError = generalMessage(login.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    if (!password) {
      setSubmittedEmpty(true)
      return
    }
    setSubmittedEmpty(false)
    login.mutate(password)
  }

  return (
    <Stack gap="lg" className="mx-auto max-w-md">
      <PageHeader title="Panel administratora" description="Parametry prawne, katalog świadczeń, pola integracji i organizacje wsparcia." />
      {notice ? <Alert variant="info" title={notice} /> : null}
      <form onSubmit={submit} noValidate>
        <Card>
          <Stack gap="md">
            <Field label="Hasło administratora" error={fieldError} description="Hasło jest pamiętane tylko do zamknięcia tej karty.">
              <TextInput type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
            </Field>
            {panelError ? (
              <Alert variant="error" title="Nie można wejść do panelu">
                {panelError}
              </Alert>
            ) : null}
            <Button type="submit" loading={login.isPending} loadingLabel="Sprawdzam hasło…">
              Wejdź do panelu
            </Button>
          </Stack>
        </Card>
      </form>
    </Stack>
  )
}
