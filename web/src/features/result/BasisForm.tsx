import { useState, type FormEvent } from 'react'
import { Button, Field, MoneyInput, parseMoney } from '@/ui'

export interface BasisFormProps {
  onSave: (value: number) => void
  busy?: boolean
  label?: string
}

export function BasisForm({ onSave, busy = false, label = 'Podstawa ustalona przez płatnika (brutto, miesięcznie)' }: BasisFormProps) {
  const [text, setText] = useState('')
  const [error, setError] = useState<string>()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const value = parseMoney(text)
    if (value === null || value <= 0) {
      setError('Wpisz kwotę w złotych większą od zera, np. 6 903,20.')
      return
    }
    setError(undefined)
    onSave(value)
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-3">
      <Field label={label} description="Znajdziesz ją w informacji od płatnika składek. Jeśli jej nie znasz, zostaw puste." error={error} fix="wpisz samą kwotę, np. 6903,20">
        <MoneyInput value={text} onChange={(e) => setText(e.target.value)} />
      </Field>
      <Button type="submit" variant="secondary" loading={busy} loadingLabel="Liczę kwotę…">
        Policz kwotę
      </Button>
    </form>
  )
}
