import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api } from '@/app/api/client'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { Button, Card, Field, PageHeader, Section, Select, Stack, ToneScope } from '@/ui'
import { FuneralBlock } from './FuneralBlock'
import { LegalAssistant } from './LegalAssistant'
import { OrganizationsSection } from './OrganizationsSection'
import { ExtLink } from './ExtLink'
import { linkClass } from './link-class'
import { contactChannels, institutions } from './links'
import { ALL, detectSituation, isDifficultSituation, isSituationId, situations, type Filter } from './situations'


function useFocusHeading() {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    window.scrollTo(0, 0)
    const heading = ref.current?.querySelector('h1')
    if (heading) {
      heading.tabIndex = -1
      heading.classList.add('focus:outline-none')
      heading.focus({ preventScroll: true })
    }
  }, [])
  return ref
}

export function SupportPage() {
  const { facts } = useCaseState()
  const pageRef = useFocusHeading()
  const hasEvent = Boolean(facts.sprawa['zdarzenie.rodzaj'])
  const detected = detectSituation(facts)
  const [chosen, setChosen] = useState<Filter | null>(null)
  const filter: Filter = chosen ?? detected ?? ALL

  const assessment = useQuery({
    queryKey: ['assessment', facts],
    queryFn: () => api.assess({ facts }),
    enabled: hasEvent,
  })
  const organizations = useQuery({
    queryKey: ['organizations', filter],
    queryFn: () => api.organizations(filter === ALL ? undefined : filter),
  })

  const funeralSituations = ['martwe_urodzenie', 'zgon_dziecka']
  const funeralOnTop = funeralSituations.includes(filter) || (detected !== null && funeralSituations.includes(detected))
  const difficult =
    assessment.data?.difficultSituation === true ||
    isDifficultSituation(filter) ||
    (detected !== null && isDifficultSituation(detected))
  const stillbirth = filter === 'martwe_urodzenie' || detected === 'martwe_urodzenie'
  const funeral = (
    <FuneralBlock
      benefit={assessment.data?.benefits.find((b) => b.id === 'pogrzebowy')}
      hasAssessment={Boolean(assessment.data)}
      stillbirth={stillbirth}
      highlighted={funeralOnTop}
    />
  )

  const orgs = (
    <div id="organizacje" tabIndex={-1} className="scroll-mt-4 focus:outline-none">
      <OrganizationsSection query={organizations} filtered={filter !== ALL} onShowAll={() => setChosen(ALL)} />
    </div>
  )

  function goToOrganizations() {
    const target = document.getElementById('organizacje')
    target?.scrollIntoView({ block: 'start' })
    target?.focus({ preventScroll: true })
  }

  return (
    <ToneScope tone={difficult ? 'difficult' : 'default'} className="-mx-4 -my-6 px-4 py-6">
      <div ref={pageRef} className="mx-auto max-w-3xl space-y-8">
        <PageHeader
          title="Wsparcie"
          description={
            difficult
              ? 'Zebraliśmy miejsca, w których możesz uzyskać pomoc i informacje. Nie musisz robić wszystkiego naraz.'
              : 'Organizacje wsparcia, informacja o zasiłku pogrzebowym i instytucje, które pomagają w sprawach formalnych.'
          }
        />

        <Card className="space-y-3">
          <Field
            label="Twoja sytuacja"
            description={
              detected
                ? 'Wybraliśmy ją na podstawie podanych danych. Możesz ją zmienić.'
                : 'Wybierz sytuację, aby zobaczyć pasujące organizacje.'
            }
          >
            <Select value={filter} onChange={(e) => setChosen(e.target.value === ALL || isSituationId(e.target.value) ? (e.target.value as Filter) : ALL)}>
              <option value={ALL}>Wszystkie sytuacje</option>
              {situations.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.label}
                </option>
              ))}
            </Select>
          </Field>
          {filter !== ALL ? (
            <Button variant="secondary" onClick={() => setChosen(ALL)}>
              Pokaż wszystkie
            </Button>
          ) : null}
        </Card>

        {difficult ? (
          <Section title="Co możesz zrobić teraz">
            <ol className="list-decimal space-y-2 pl-5 text-ink">
              {funeralOnTop ? (
                <li>
                  Jeśli organizujesz pogrzeb, możesz złożyć w ZUS wniosek o zasiłek pogrzebowy (druk Z-12) do 12 miesięcy od pochówku. Szczegóły są niżej.
                </li>
              ) : null}
              {funeralOnTop ? (
                <li>
                  Po martwym urodzeniu lub śmierci dziecka matce może przysługiwać zasiłek macierzyński.{' '}
                  {hasEvent ? (
                    <Link to={paths.result} className={linkClass}>
                      Zobacz wynik oceny
                    </Link>
                  ) : (
                    <Link to={paths.start} className={linkClass}>
                      Sprawdź to na start
                    </Link>
                  )}
                  .
                </li>
              ) : (
                <li>
                  Sprawdź, jakie świadczenia mogą Ci przysługiwać.{' '}
                  <Link to={hasEvent ? paths.result : paths.start} className={linkClass}>
                    {hasEvent ? 'Zobacz wynik oceny' : 'Sprawdź to na start'}
                  </Link>
                  .
                </li>
              )}
              <li>
                Jeśli masz pytania, zadzwoń do ZUS: {contactChannels[0].node}.
              </li>
              <li>
                Skorzystaj z pomocy organizacji wsparcia.{' '}
                <button type="button" onClick={goToOrganizations} className={linkClass}>
                  Przejdź do listy
                </button>
                .
              </li>
            </ol>
          </Section>
        ) : null}

        {orgs}

        {funeralOnTop ? funeral : null}

        <Section title="Instytucje" description="Strony, na których znajdziesz oficjalne informacje.">
          <Stack as="ul" gap="sm">
            {institutions.map((item) => (
              <li key={item.id}>
                <ExtLink url={item.url}>{item.name}</ExtLink>
                <span className="block text-ink-soft">{item.why}</span>
              </li>
            ))}
          </Stack>
        </Section>

        {funeralOnTop ? null : funeral}
        <LegalAssistant calculatorPath={hasEvent ? paths.result : paths.start} />
      </div>
    </ToneScope>
  )
}
