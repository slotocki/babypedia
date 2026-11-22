import type { Risk } from '@/app/api/types'
import { Card, EmptyState, Section, ShieldIcon } from '@/ui'
import type { DeadlineEntry } from './DeadlinesSection'
import { ExtLink } from './ExtLink'
import { riskDuplicatesDeadline } from './insights'

export function RisksSection({ risks: all, entries = [] }: { risks: Risk[]; entries?: DeadlineEntry[] }) {
  // Ryzyko, które tylko powtarza termin z „Co zrobić i do kiedy”, pomijamy — ten sam tekst nie musi pojawiać się trzy razy.
  const risks = all.filter((risk) => !riskDuplicatesDeadline(risk.id, entries))
  return (
    <Section title="Na co uważać" description="Informacje, które mogą mieć znaczenie dla Twoich uprawnień. To nie jest ocena Twojej sytuacji.">
      {risks.length === 0 ? (
        <EmptyState title="Nic szczególnego do zasygnalizowania" description="Na podstawie podanych danych nie widzimy dodatkowych zagrożeń dla uprawnień." />
      ) : (
        <ul className="space-y-3">
          {risks.map((risk) => (
            <li key={risk.id}>
              <Card tone="muted" className="flex items-start gap-3 p-4 sm:p-4">
                <ShieldIcon className="mt-0.5 size-6 shrink-0 text-risk" />
                <div className="min-w-0 space-y-1">
                  <p className="break-words text-base text-ink">{risk.text}</p>
                  {risk.basis ? <p className="break-words text-sm text-ink-soft">Przepis: {risk.basis}</p> : null}
                  {risk.link ? (
                    <p className="text-base">
                      <ExtLink url={risk.link.url}>{risk.link.label}</ExtLink>
                    </p>
                  ) : null}
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </Section>
  )
}
