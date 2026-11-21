import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { Card, PageHeader, Section } from '@/ui'
import { ExtLink } from './ExtLink'
import { LegalAssistant } from './LegalAssistant'
import { institutions } from './links'

export function KnowledgePage() {
  const { facts } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()
  const calculatorPath = facts.sprawa['zdarzenie.rodzaj'] ? paths.result : paths.start

  return (
    <div ref={ref} className="mx-auto max-w-3xl space-y-6 py-3">
      <PageHeader title="Baza wiedzy" description="Znajdź przepisy i oficjalne informacje o świadczeniach, urlopach i wnioskach." />
      <LegalAssistant calculatorPath={calculatorPath} />
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
