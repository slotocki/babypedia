import { useQuery } from '@tanstack/react-query'
import { lazy, Suspense } from 'react'
import { api } from '@/app/api/client'
import { Card, ErrorState, PageHeader, SkeletonText, Spinner } from '@/ui'
import { AddCasePdf } from './AddCasePdf'
import { CaseList } from './CaseList'
import { useCasePdf } from './useCasePdf'
import { useFocusHeading } from './useFocusHeading'
import { useLoadCase } from './useLoadCase'

const PdfPreview = lazy(() => import('./PdfPreview').then((m) => ({ default: m.PdfPreview })))

export function CasesPage() {
  const pageRef = useFocusHeading<HTMLDivElement>()
  const cases = useQuery({ queryKey: ['cases'], queryFn: api.cases.list })
  const pdf = useCasePdf()
  const loader = useLoadCase()
  const hasPreview = pdf.doc !== null

  return (
    <div ref={pageRef} className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Sprawy ZUS"
        description="Wybierz sprawę. Wczytamy dane osoby odczytane z dokumentów ZUS, a wynik kalkulatora porównamy z decyzją ZUS."
      />

      {cases.isPending ? <SkeletonText lines={4} label="Wczytuję listę spraw…" /> : null}
      {cases.isError ? <ErrorState error={cases.error} title="Nie udało się wczytać listy spraw" onRetry={() => void cases.refetch()} retrying={cases.isFetching} /> : null}
      {cases.isSuccess ? <CaseList cases={cases.data} loader={loader} /> : null}

      <details className="group rounded-card border border-line bg-surface" open={hasPreview || undefined}>
        <summary className="flex min-h-11 cursor-pointer items-center px-4 py-3 font-semibold text-brand-strong sm:px-5">
          Masz plik PDF sprawy ZUS? Rozpoznaj go
        </summary>
        <div className={hasPreview ? 'grid grid-cols-1 gap-6 px-4 pb-4 sm:px-5 lg:grid-cols-2 lg:items-start' : 'max-w-3xl px-4 pb-4 sm:px-5'}>
          <div className="min-w-0">
            <AddCasePdf pdf={pdf} loader={loader} />
          </div>
          {pdf.doc && pdf.file ? (
            <Card className="min-w-0 space-y-3 lg:sticky lg:top-4">
              <h2 className="text-xl font-semibold text-ink">Podgląd dokumentu</h2>
              <Suspense
                fallback={
                  <p role="status" className="flex items-center gap-2 text-ink-soft">
                    <Spinner label={null} /> Wczytuję podgląd…
                  </p>
                }
              >
                <PdfPreview key={pdf.file.sha256} doc={pdf.doc} fileName={pdf.file.name} />
              </Suspense>
            </Card>
          ) : null}
        </div>
      </details>
    </div>
  )
}
