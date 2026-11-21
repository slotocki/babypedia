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
        description="Wybierz sprawę testową albo dodaj plik PDF sprawy. Wczytamy dane fikcyjnej osoby z tej sprawy, a Ty je sprawdzisz."
      />

      <div className={hasPreview ? 'grid grid-cols-1 gap-6 lg:grid-cols-2 lg:items-start' : 'grid max-w-3xl grid-cols-1 gap-6'}>
        <div className={hasPreview ? 'min-w-0 lg:col-start-1 lg:row-start-1' : 'min-w-0'}>
          <AddCasePdf pdf={pdf} loader={loader} />
        </div>

        {pdf.doc && pdf.file ? (
          <Card className="min-w-0 space-y-3 lg:sticky lg:top-4 lg:col-start-2 lg:row-span-2 lg:row-start-1">
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

        <div className={hasPreview ? 'min-w-0 lg:col-start-1 lg:row-start-2' : 'min-w-0'}>
          {cases.isPending ? (
            <SkeletonText lines={4} label="Wczytuję listę spraw…" />
          ) : null}
          {cases.isError ? <ErrorState error={cases.error} title="Nie udało się wczytać listy spraw" onRetry={() => void cases.refetch()} retrying={cases.isFetching} /> : null}
          {cases.isSuccess ? <CaseList cases={cases.data} loader={loader} /> : null}
        </div>
      </div>
    </div>
  )
}
