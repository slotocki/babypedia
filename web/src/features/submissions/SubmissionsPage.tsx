import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import type { SubmissionReceipt } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { api } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Alert, Button, Card, Disclosure, ErrorState, PageHeader, Section, SkeletonText } from '@/ui'
import { Certificate } from './Certificate'
import { findKind, relevantLeaveKinds, otherKinds, submissionFormPath, type SubmissionKind } from './kinds'
import { MySubmissions } from './MySubmissions'
import { NeedsLogin } from './NeedsLogin'
import { Receipt } from './Receipt'
import { TemplateForm } from './TemplateForm'
import { useFocusHeading } from './useFocusHeading'

const link = 'font-semibold text-brand-strong underline underline-offset-4'

export function SubmissionsPage() {
  const { sessionId, facts } = useCaseState()
  const [params, setParams] = useSearchParams()
  const [receipt, setReceipt] = useState<SubmissionReceipt | null>(null)

  const widok = params.get('widok')
  const kind = widok === 'formularz' ? findKind(params.get('szablon')) : undefined
  // Bez profilu, ale z wynikiem z wpisu ręcznego: wnioski do pracodawcy i zestawienie ZAM (PDF) są dostępne.
  const anonymous = !sessionId && Boolean(facts.sprawa['zdarzenie.rodzaj'])
  const anonymousKind = kind && kind.type !== 'zapytanie_ezus' && kind.type !== 'odpis_aktu' ? kind : undefined
  const view = !sessionId
    ? anonymous && (!kind || anonymousKind)
      ? kind
        ? `formularz:${kind.id}`
        : 'lista'
      : 'login'
    : kind
      ? `formularz:${kind.id}`
      : widok === 'potwierdzenie' && receipt
        ? 'potwierdzenie'
        : widok === 'moje'
          ? 'moje'
          : widok === 'zaswiadczenie'
            ? 'zaswiadczenie'
            : 'lista'

  const ref = useFocusHeading<HTMLDivElement>(view)

  const headers: Record<string, { title: string; description?: string }> = {
    lista: { title: 'Wnioski i zgłoszenia', description: 'Wybierz, co chcesz przygotować. Wypełnisz formularz, a my przygotujemy plik PDF (po zalogowaniu także numer zgłoszenia w symulacji).' },
    moje: { title: 'Moje zgłoszenia', description: 'Zgłoszenia złożone w tej sesji (symulacja).' },
    zaswiadczenie: { title: 'Zaświadczenie o stanie składek (symulacja)' },
    potwierdzenie: {
      title: receipt?.type === 'wniosek_urlop' || receipt?.type === 'dane_zam' ? 'PDF jest gotowy' : 'Zgłoszenie przyjęte (symulacja)',
      description: 'Możesz otworzyć lub pobrać dokument.',
    },
    login: { title: 'Wnioski i zgłoszenia' },
  }
  const header = kind ? { title: kind.title, description: `Adresat: ${kind.recipient}.` } : headers[view]

  return (
    <div ref={ref} className="mx-auto max-w-2xl space-y-6">
      <PageHeader title={header.title} description={header.description} />

      {view === 'login' ? <NeedsLogin templateId={kind?.id} /> : null}

      {view === 'lista' ? <KindList anonymous={anonymous} /> : null}

      {(sessionId || anonymous) && kind && view !== 'login' ? (
        <FormView
          kind={kind}
          sessionId={sessionId}
          onDone={(done) => {
            setReceipt(done)
            setParams({ widok: 'potwierdzenie' })
          }}
        />
      ) : null}

      {sessionId && view === 'potwierdzenie' && receipt ? <Receipt receipt={receipt} session={sessionId} /> : null}
      {sessionId && view === 'moje' ? <MySubmissions session={sessionId} /> : null}
      {sessionId && view === 'zaswiadczenie' ? <Certificate session={sessionId} /> : null}

      {(sessionId || anonymous) && view !== 'lista' && view !== 'login' ? (
        <Button asChild variant="link">
          <Link to={paths.submissions}>← Wróć do listy wniosków</Link>
        </Button>
      ) : null}
    </div>
  )
}

function FormView({ kind, sessionId, onDone }: { kind: SubmissionKind; sessionId: string | null; onDone: (receipt: SubmissionReceipt) => void }) {
  const query = useQuery({ queryKey: ['submissions', 'templates'], queryFn: api.submissions.templates, staleTime: Infinity })

  if (query.isPending) return <SkeletonText lines={6} />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} retrying={query.isFetching} />

  const template = query.data.find((item) => item.id === kind.id)
  if (!template) {
    return <ErrorState title="Nie znaleziono tego formularza" description="Serwer nie zwrócił szablonu dla wybranego wniosku. Wróć do listy i spróbuj ponownie." />
  }

  return (
    <div className="space-y-6">
      {kind.note ? <KindNote note={kind.note} /> : null}
      <TemplateForm key={template.id + (sessionId ?? 'anonim')} template={template} sessionId={sessionId} onDone={onDone} />
    </div>
  )
}

function KindNote({ note }: { note: NonNullable<SubmissionKind['note']> }) {
  return (
    <p className="text-base text-ink">
      {note.text}{' '}
      <a href={note.href} target="_blank" rel="noopener noreferrer" className={link}>
        {note.linkLabel}
        <span className="sr-only"> (otwiera się w nowej karcie)</span>
      </a>
      .
    </p>
  )
}

function KindList({ anonymous = false }: { anonymous?: boolean }) {
  const { facts } = useCaseState()
  const { now, later } = relevantLeaveKinds(facts)
  return (
    <div className="space-y-8">
      {anonymous ? (
        <Alert variant="info" title="Korzystasz bez logowania">
          Wnioski do pracodawcy i zestawienie danych do ZAM przygotujesz jako PDF z danych z Twojego wyniku. Nic nie jest zapisywane ani wysyłane. Zgłoszenia „do ZUS” i urzędu (symulacja) oraz lista zgłoszeń wymagają{' '}
          <Link to={paths.login + '?powrot=wnioski'} className={link}>wybrania profilu testowego</Link>.
        </Alert>
      ) : null}

      <Section
        title={later.length > 0 ? 'Wnioski o urlop dla Ciebie teraz' : 'Wnioski o urlop do pracodawcy'}
        description="Każdy wniosek kończy się plikiem PDF do wydrukowania lub przekazania. Daty z Twojego planu wpiszemy za Ciebie."
      >
        <ul className="space-y-4">
          {now.map((kind) => (
            <li key={kind.id}>
              <KindCard kind={kind} />
            </li>
          ))}
        </ul>
        {later.length > 0 ? (
          <Disclosure title={`Pozostałe wnioski o urlop (${later.length})`} summary="Zwykle nie dotyczą Twojej obecnej sytuacji" className="mt-4">
            <ul className="space-y-4">
              {later.map((kind) => (
                <li key={kind.id}>
                  <KindCard kind={kind} />
                </li>
              ))}
            </ul>
          </Disclosure>
        ) : null}
      </Section>

      <Section title="Zasiłek, zapytania i dokumenty">
        <ul className="space-y-4">
          {otherKinds.map((kind) => (
            <li key={kind.id}>
              <KindCard kind={kind} needsLogin={anonymous && (kind.type === 'zapytanie_ezus' || kind.type === 'odpis_aktu')} />
            </li>
          ))}
          {anonymous ? null : <li>
            <Card as="article" className="space-y-3">
              <h3 className="text-lg font-bold text-ink">Zaświadczenie o stanie składek</h3>
              <p className="text-base text-ink">Pokazuje, czy na wybrany dzień masz zaległe składki. Pobierzesz je jako PDF.</p>
              <p className="text-sm font-semibold text-ink-soft">Adresat: ZUS</p>
                <Button asChild variant="secondary">
                <Link to={`${paths.submissions}?widok=zaswiadczenie`}>Wybierz dzień i pobierz</Link>
              </Button>
            </Card>
          </li>}
        </ul>
      </Section>

      {anonymous ? null : (
        <Section title="Twoje zgłoszenia">
          <Button asChild variant="secondary">
            <Link to={`${paths.submissions}?widok=moje`}>Zobacz moje zgłoszenia</Link>
          </Button>
        </Section>
      )}
    </div>
  )
}

function KindCard({ kind, needsLogin = false }: { kind: SubmissionKind; needsLogin?: boolean }) {
  return (
    <Card as="article" className="space-y-3">
      <h3 className="text-lg font-bold text-ink">{kind.title}</h3>
      <p className="text-base text-ink">{kind.description}</p>
      {kind.note ? <KindNote note={kind.note} /> : null}
      <p className="text-sm font-semibold text-ink-soft">Adresat: {kind.recipient}</p>
      {needsLogin ? (
        <Button asChild variant="secondary">
          <Link to={paths.login + '?powrot=wnioski'}>
            Wybierz profil, żeby wypełnić<span className="sr-only">: {kind.title}</span>
          </Link>
        </Button>
      ) : (
        <Button asChild>
          <Link to={submissionFormPath(kind.id)}>
            Wypełnij<span className="sr-only">: {kind.title}</span>
          </Link>
        </Button>
      )}
    </Card>
  )
}
