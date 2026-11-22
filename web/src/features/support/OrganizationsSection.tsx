import type { Organization } from '@/app/api/types'
import { Card, Disclosure, EmptyState, ErrorState, Section, SkeletonText, Stack } from '@/ui'
import type { UseQueryResult } from '@tanstack/react-query'
import { ExtLink } from './ExtLink'
import { linkClass } from './link-class'
import { contactChannels } from './links'
import { situationLabel } from './situations'

/** Organizacje, które pomagają wyłącznie po stracie dziecka albo w ciężkiej chorobie matki. */
const lossSituations = ['poronienie', 'martwe_urodzenie', 'zgon_dziecka', 'sytuacja_matki']
const isLossOnly = (org: Organization) => org.sytuacje.length > 0 && org.sytuacje.every((s) => lossSituations.includes(s))

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
            <a href={telHref(org.telefon)} className={linkClass}>
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
  collapseLoss = false,
}: {
  query: UseQueryResult<Organization[]>
  filtered: boolean
  onShowAll: () => void
  /** Bez wybranej sytuacji: organizacje dla żałoby nie otwierają listy, tylko czekają pod rozwinięciem. */
  collapseLoss?: boolean
}) {
  return (
    <Section
      title="Organizacje wsparcia"
      description="To organizacje charytatywne. Nie są związane z ZUS i nie decydują o świadczeniach."
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
          <OrganizationList organizations={query.data} collapseLoss={collapseLoss} />
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

function OrganizationList({ organizations, collapseLoss }: { organizations: Organization[]; collapseLoss: boolean }) {
  const loss = collapseLoss ? organizations.filter(isLossOnly) : []
  const main = collapseLoss ? organizations.filter((org) => !isLossOnly(org)) : organizations
  const list = (items: Organization[], label: string) => (
    <Stack as="ul" gap="md" aria-label={label}>
      {items.map((org) => (
        <OrganizationCard key={org.id} org={org} />
      ))}
    </Stack>
  )
  return (
    <div className="space-y-4">
      {main.length > 0 ? list(main, 'Lista organizacji wsparcia') : null}
      {loss.length > 0 ? (
        <Disclosure
          title={`Wsparcie po stracie dziecka lub przy ciężkiej chorobie matki (${loss.length})`}
          summary="Rozwiń tylko wtedy, gdy Cię to dotyczy. Możesz też wybrać swoją sytuację powyżej."
        >
          {list(loss, 'Organizacje wsparcia po stracie')}
        </Disclosure>
      ) : null}
    </div>
  )
}
