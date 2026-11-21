import { useState } from 'react'
import { Link } from 'react-router'
import { api, ApiError } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Button } from '@/ui'
import { downloadBlob, showBlob } from './api'

type Mode = 'download' | 'open'

interface PdfActionsProps {
  path: string
  fileName: string
  session: string
  withOpen?: boolean
  downloadLabel?: string
}

function describe(error: unknown): { text: string; relogin: boolean } {
  if (error instanceof ApiError) {
    if (error.status === 410) {
      return {
        text: 'Ten PDF już wygasł. Przechowujemy go tylko około 2 godzin. Wypełnij wniosek jeszcze raz, żeby dostać nowy plik.',
        relogin: false,
      }
    }
    if (error.status === 401) return { text: 'Sesja symulacji wygasła. Zaloguj się ponownie.', relogin: true }
    if (error.status === 404) return { text: 'Nie znaleziono tego zgłoszenia w bieżącej sesji.', relogin: false }
    return { text: error.message, relogin: false }
  }
  if (error instanceof TypeError) return { text: 'Nie możemy połączyć się z serwerem. Spróbuj ponownie za chwilę.', relogin: false }
  return { text: error instanceof Error ? error.message : 'Nie udało się pobrać pliku PDF.', relogin: false }
}

export function PdfActions({ path, fileName, session, withOpen = false, downloadLabel = 'Pobierz PDF' }: PdfActionsProps) {
  const [busy, setBusy] = useState<Mode | null>(null)
  const [error, setError] = useState<unknown>(null)

  const run = async (mode: Mode) => {
    const target = mode === 'open' ? window.open('', '_blank') : null
    setBusy(mode)
    setError(null)
    try {
      const blob = await api.submissions.pdf(session, path)
      if (mode === 'open') showBlob(blob, fileName, target)
      else downloadBlob(blob, fileName)
    } catch (e) {
      target?.close()
      setError(e)
    } finally {
      setBusy(null)
    }
  }

  const problem = error ? describe(error) : null

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <Button onClick={() => void run('download')} loading={busy === 'download'} loadingLabel="Pobieram plik PDF…" disabled={busy !== null}>
          {downloadLabel}
        </Button>
        {withOpen ? (
          <Button variant="secondary" onClick={() => void run('open')} loading={busy === 'open'} loadingLabel="Otwieram plik PDF…" disabled={busy !== null}>
            Otwórz PDF
          </Button>
        ) : null}
      </div>
      {problem ? (
        <p role="alert" className="text-base font-medium text-bad">
          <span className="sr-only">Błąd: </span>
          {problem.text}{' '}
          {problem.relogin ? (
            <Link to={paths.login} className="font-semibold underline underline-offset-4">
              Zaloguj się
            </Link>
          ) : null}
        </p>
      ) : null}
    </div>
  )
}
