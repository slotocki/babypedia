import { useState } from 'react'
import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Alert, Button, DateInput, Field, SimulationBanner } from '@/ui'
import { certificatePath } from './api'
import { PdfActions } from './PdfActions'

const todayIso = () => {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

export function Certificate({ session }: { session: string }) {
  const [date, setDate] = useState(todayIso)
  const year = Number(date.slice(0, 4))
  const valid = /^\d{4}-\d{2}-\d{2}$/.test(date) && year >= 2000 && year <= 2100

  return (
    <div className="space-y-6">
      <SimulationBanner>
        To zaświadczenie jest symulacją. Nie jest dokumentem urzędowym i nie zostało wydane przez ZUS.
      </SimulationBanner>
      <p className="max-w-prose text-lg text-ink-soft">
        Zaświadczenie pokazuje, czy na wybrany dzień na koncie są zaległe składki. Wybierz dzień, na który ma być wystawione, i pobierz plik PDF.
      </p>
      <div className="max-w-sm">
        <Field label="Na jaki dzień" error={valid ? undefined : 'Wybierz datę z lat 2000–2100.'}>
          <DateInput value={date} onChange={(e) => setDate(e.target.value)} />
        </Field>
      </div>
      {valid ? (
        <PdfActions
          key={date}
          path={certificatePath(date)}
          fileName={`zaswiadczenie-o-stanie-skladek-${date}.pdf`}
          session={session}
          withOpen
          downloadLabel="Pobierz zaświadczenie (PDF)"
        />
      ) : (
        <Button disabled>Pobierz zaświadczenie (PDF)</Button>
      )}
      <Alert variant="info" title="Zobacz też">
        <Link to={paths.zusData} className="font-semibold text-brand-strong underline underline-offset-4">
          Moje dane w ZUS (symulacja)
        </Link>{' '}
        — saldo składek i inne dane z konta.
      </Alert>
    </div>
  )
}
