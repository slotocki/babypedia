import type { Organization } from '@/app/api/types'
import { Card, EmptyState, ErrorState, Section, SkeletonText, Stack } from '@/ui'
import type { UseQueryResult } from '@tanstack/react-query'
import { ExtLink } from './ExtLink'
import { contactChannels } from './links'
import { situationLabel } from './situations'

const telHref = (phone: string) => `tel:${phone.replace(/[^\d+]/g, '')}`

function OrganizationCard({ org }: { org: Organization }) {
  return (
    <Card as="li" className="space-y-3">
      <h3 className="break-words text-lg font-semibold text-ink">{org.nazwa}</h3>
      <p className="break-words text-ink">{org.opis}</p>
      {org.sytuacje.length > 0 ? (
        <p className="break-words text-sm text-ink-soft">Pomaga w sytuacjach: {org.sytuacje.map(situationLabel).join(', ')}</p>
      ) : null}
      <ul className="space-y-1">
        {org.telefon ? (
          <li>
            Telefon:{' '}
            <a href={telHref(org.telefon)} className="font-semibold text-brand-strong underline underline-offset-4">
              {org.telefon}
            </a>
          </li>
        ) : null}
        {org.url ? (
          <li>
            Strona: <ExtLink url={org.url}>{org.url.replace(/^https?:\/\//i, '').replace(/\/$/, '')}</ExtLink>
          </li>
        ) : null}
      </ul>
    </Card>
  )
}

export function OrganizationsSection({
  query,
  filtered,
  onShowAll,
}: {
  query: UseQueryResult<Organization[]>
  filtered: boolean
  onShowAll: () => void
}) {
  return (
    <Section
      title="Organizacje wsparcia"
      description="To organizacje charytatywne. Nie są związane z ZUS i nie decydują o świadczeniach. Listę sprawdza administrator serwisu, ale zanim się skontaktujesz, możesz sama lub sam potwierdzić dane na stronie organizacji."
    >
      <div aria-live="polite">
        {query.isPending ? (
          <SkeletonText lines={4} label="Ładowanie organizacji wsparcia…" />
        ) : query.isError ? (
          <ErrorState
            error={query.error}
            title="Nie udało się pobrać listy organizacji"
            onRetry={() => void query.refetch()}
            retrying={query.isFetching}
          />
        ) : query.data.length > 0 ? (
          <Stack as="ul" gap="md" aria-label="Lista organizacji wsparcia">
            {query.data.map((org) => (
              <OrganizationCard key={org.id} org={org} />
            ))}
          </Stack>
        ) : filtered ? (
          <EmptyState
            title="Nie mamy jeszcze organizacji dla tej sytuacji"
            description="Możesz zobaczyć wszystkie organizacje z listy albo zadzwonić do ZUS i zapytać o dostępne formy pomocy."
            action={
              <button type="button" onClick={onShowAll} className="font-semibold text-brand-strong underline underline-offset-4">
                Pokaż wszystkie organizacje
              </button>
            }
          />
        ) : (
          <EmptyState
            title="Lista organizacji jest w przygotowaniu"
            description="Pokazujemy tylko organizacje sprawdzone przez administratora. Na razie żadnej nie ma na liście. Pomoc możesz uzyskać w instytucjach poniżej."
            action={
              <ul className="space-y-1 text-ink">
                {contactChannels.map((c) => (
                  <li key={c.id}>{c.node}</li>
                ))}
              </ul>
            }
          />
        )}
      </div>
    </Section>
  )
}
