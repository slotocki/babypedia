import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { Card, FileIcon, PageHeader, Section } from '@/ui'
import { ExtLink } from './ExtLink'
import { LegalAssistant } from './LegalAssistant'
import { linkClass } from './link-class'
import { institutions } from './links'

/** Poradnik ZUS z materiałów konkursowych (publikacja ZUS, stan prawny na 1 kwietnia 2026 r.). */
const guide = {
  url: '/dokumenty/poradnik-dla-rodzicow-zus-2026.pdf',
  title: 'Poradnik dla rodziców i opiekunów dzieci',
  meta: 'ZUS, stan prawny na 1 kwietnia 2026 r. · PDF, 42 strony, 5 MB',
  // Strony PDF wg spisu treści poradnika; #page=N otwiera przeglądarkę PDF na tej stronie.
  chapters: [
    { title: 'Objaśnienie pojęć i skrótów', page: 6 },
    { title: 'Ubezpieczenia pracowników', page: 7 },
    { title: 'Ubezpieczenia zleceniobiorców', page: 10 },
    { title: 'Osoby prowadzące działalność', page: 13 },
    { title: 'Osoby na zasiłku macierzyńskim i urlopie wychowawczym', page: 20 },
    { title: 'Świadczenia dla kobiet w ciąży', page: 24 },
    { title: 'Świadczenia po narodzinach dziecka', page: 30 },
    { title: 'Kontakt z ZUS', page: 40 },
  ],
}

export function KnowledgePage() {
  const { facts } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()
  const calculatorPath = facts.sprawa['zdarzenie.rodzaj'] ? paths.result : paths.start

  return (
    <div ref={ref} className="mx-auto max-w-3xl space-y-6 py-3">
      <PageHeader title="Baza wiedzy" description="Znajdź przepisy i oficjalne informacje o świadczeniach, urlopach i wnioskach." />
      <LegalAssistant calculatorPath={calculatorPath} />

      <Section title="Poradnik ZUS" description="Oficjalny poradnik ZUS o ubezpieczeniach i świadczeniach dla rodziców. Czat powyżej przeszukuje też jego treść.">
        <Card className="space-y-4">
          <div className="flex items-start gap-3">
            <FileIcon className="mt-0.5 size-6 shrink-0 text-brand-strong" />
            <div className="min-w-0 space-y-1">
              <a href={guide.url} target="_blank" rel="noreferrer" className={`text-lg ${linkClass}`}>
                {guide.title}
                <span className="sr-only"> (PDF, otwiera się w nowej karcie)</span>
              </a>
              <p className="text-sm text-ink-soft">{guide.meta}</p>
            </div>
          </div>
          <nav aria-label="Rozdziały poradnika">
            <h3 className="mb-2 text-sm font-semibold text-ink">Przejdź do rozdziału</h3>
            <ul className="grid gap-x-4 gap-y-1 sm:grid-cols-2">
              {guide.chapters.map((chapter) => (
                <li key={chapter.page + chapter.title}>
                  <a href={`${guide.url}#page=${chapter.page}`} target="_blank" rel="noreferrer" className={`inline-flex min-h-11 items-center text-sm ${linkClass}`}>
                    <span>
                      {chapter.title}{' '}
                      <span className="whitespace-nowrap font-normal text-ink-soft">(s.{' '}{chapter.page})</span>
                      <span className="sr-only"> (PDF, otwiera się w nowej karcie)</span>
                    </span>
                  </a>
                </li>
              ))}
            </ul>
          </nav>
        </Card>
      </Section>

      <Section title="Oficjalne źródła">
        <ul className="grid gap-3 sm:grid-cols-2">
          {institutions.map((item) => (
            <li key={item.id}>
              <Card className="h-full space-y-1">
                <ExtLink url={item.url}>{item.name}</ExtLink>
                <p className="text-sm text-ink-soft">{item.why}</p>
              </Card>
            </li>
          ))}
        </ul>
      </Section>
    </div>
  )
}
