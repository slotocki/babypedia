import { useRef, useState, type DragEvent } from 'react'
import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Alert, Button, ErrorState, Section, Spinner, cn } from '@/ui'
import { decisionLabel } from './decision'
import { formatSize, MAX_PDF_BYTES } from './pdf-file'
import type { useCasePdf } from './useCasePdf'
import type { useLoadCase } from './useLoadCase'

type PdfState = ReturnType<typeof useCasePdf>
type LoadState = ReturnType<typeof useLoadCase>

const linkClass = 'font-semibold text-brand-strong underline underline-offset-4'

function StageStatus({ stage }: { stage: PdfState['stage'] }) {
  if (stage.kind === 'reading') {
    const percent = Math.round(stage.fraction * 100)
    return (
      <div role="status" className="space-y-1">
        <p className="text-base text-ink">Wczytuję plik w przeglądarce: {percent}%</p>
        <progress value={percent} max={100} aria-label="Postęp wczytywania pliku" className="h-2 w-full" />
      </div>
    )
  }
  if (stage.kind === 'hashing') return <p role="status" className="flex items-center gap-2 text-ink"><Spinner label={null} /> Liczę sumę kontrolną pliku…</p>
  if (stage.kind === 'opening') return <p role="status" className="flex items-center gap-2 text-ink"><Spinner label={null} /> Otwieram podgląd…</p>
  return null
}

function Recognition({ pdf, loader }: { pdf: PdfState; loader: LoadState }) {
  const { lookup, file } = pdf
  if (!file) return null

  if (lookup.kind === 'loading') {
    return <p role="status" className="flex items-center gap-2 text-ink"><Spinner label={null} /> Sprawdzam, czy znam tę sprawę…</p>
  }
  if (lookup.kind === 'error') {
    return <ErrorState error={lookup.error} title="Nie udało się rozpoznać sprawy" onRetry={pdf.retryLookup} description="Plik został tylko wczytany w przeglądarce. Spróbuj ponownie albo wybierz sprawę z listy." />
  }
  if (lookup.kind === 'unknown') {
    return (
      <Alert variant="warning" title="Nie znam tej sprawy, wprowadź dane">
        <p>Plik nie pasuje do żadnej sprawy testowej. Możesz obejrzeć go obok i wprowadzić dane samodzielnie.</p>
        <p className="mt-2">
          <Link to={paths.manual} className={linkClass}>
            Przejdź do ręcznego wprowadzania danych
          </Link>
        </p>
      </Alert>
    )
  }
  if (lookup.kind !== 'found') return null

  const { summary } = lookup
  if (!summary.dostepna) {
    return (
      <Alert variant="info" title={`To sprawa: ${summary.tytul}`}>
        <p>Oczekiwana decyzja ZUS: {decisionLabel[summary.oczekiwanaDecyzja]}.</p>
        <p className="mt-1">Dane tej sprawy nie zostały jeszcze dostarczone, więc nie możemy ich załadować.</p>
        <p className="mt-2">
          <Link to={paths.login} className={linkClass}>
            Wypróbuj na przykładowej osobie (symulacja)
          </Link>
        </p>
      </Alert>
    )
  }
  const loading = loader.state.status === 'loading'
  return (
    <div className="space-y-3">
      <Alert variant="info" title={`To sprawa: ${summary.tytul}`}>
        <p>Oczekiwana decyzja ZUS: {decisionLabel[summary.oczekiwanaDecyzja]}.</p>
      </Alert>
      <Button fullWidth className="sm:w-auto" loading={loading} loadingLabel="Ładuję dane sprawy (symulacja)…" disabled={loader.busy} onClick={() => void loader.load(summary)}>
        Załaduj dane sprawy
      </Button>
      <p className="flex items-center gap-2 text-sm text-ink-soft">
        Zalogujemy Cię jako fikcyjną osobę z tej sprawy.
      </p>
      {loader.state.status === 'error' && loader.state.caseId === summary.id ? (
        <ErrorState error={loader.state.error} title="Nie udało się załadować danych sprawy" onRetry={() => void loader.load(summary)} />
      ) : null}
    </div>
  )
}

export function AddCasePdf({ pdf, loader }: { pdf: PdfState; loader: LoadState }) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const { stage, file, docError } = pdf
  const working = stage.kind === 'reading' || stage.kind === 'hashing' || stage.kind === 'opening'

  const onFiles = (files: FileList | null) => {
    const first = files?.[0]
    if (first && !working) void pdf.pick(first)
  }

  const onDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setDragging(false)
    onFiles(event.dataTransfer.files)
  }

  return (
    <Section title="Dodaj PDF sprawy" description="Dodaj dokument sprawy, a rozpoznamy, której sprawy dotyczy, i pokażemy go obok.">
      <Alert variant="info" title="Plik nie opuszcza Twojego urządzenia">
        Do serwera trafia tylko suma kontrolna pliku (a w zapasie jego nazwa i liczba stron). Treści dokumentu nie wysyłamy, nie zapisujemy i nie odczytujemy.
      </Alert>

      <div
        onDragOver={(event) => {
          event.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className={cn(
          'flex flex-col items-center gap-3 rounded-card border-2 border-dashed px-4 py-6 text-center transition-colors',
          dragging ? 'border-brand bg-brand-soft' : 'border-line bg-surface',
        )}
      >
        <p className="text-base text-ink">Przeciągnij plik PDF tutaj albo wybierz go z urządzenia.</p>
        <input
          ref={inputRef}
          type="file"
          accept="application/pdf"
          hidden
          onChange={(event) => {
            onFiles(event.target.files)
            event.target.value = ''
          }}
        />
        <Button variant="secondary" disabled={working} onClick={() => inputRef.current?.click()}>
          {file ? 'Wybierz inny plik PDF' : 'Wybierz plik PDF'}
        </Button>
        <p className="text-sm text-ink-soft">Tylko PDF, do {formatSize(MAX_PDF_BYTES)}.</p>
      </div>

      <StageStatus stage={stage} />
      {stage.kind === 'rejected' ? (
        <Alert variant="error" title="Nie możemy użyć tego pliku">
          {stage.message}
        </Alert>
      ) : null}

      {file ? (
        <div className="space-y-3">
          <dl className="space-y-1 text-sm text-ink-soft">
            <div className="flex flex-wrap gap-x-2">
              <dt className="font-semibold">Plik:</dt>
              <dd className="min-w-0 break-words">{file.name} ({formatSize(file.size)}{file.pages ? `, stron: ${file.pages}` : ''})</dd>
            </div>
            <div className="flex flex-wrap gap-x-2">
              <dt className="font-semibold">Suma kontrolna SHA-256:</dt>
              <dd className="min-w-0 break-all font-mono text-xs">{file.sha256}</dd>
            </div>
          </dl>
          {docError ? <Alert variant="warning" title="Podglądu nie pokażemy">{docError}</Alert> : null}
          <Recognition pdf={pdf} loader={loader} />
          <Button variant="link" onClick={pdf.clear}>
            Usuń plik z ekranu
          </Button>
        </div>
      ) : null}
    </Section>
  )
}
