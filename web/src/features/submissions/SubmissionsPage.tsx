import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import type { SubmissionReceipt } from '@/app/api/types'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { Button, Card, ErrorState, PageHeader, Section, SimulationBadge, SimulationBanner, SkeletonText } from '@/ui'
import { fetchTemplates } from './api'
import { Certificate } from './Certificate'
import { findKind, leaveKinds, otherKinds, type SubmissionKind } from './kinds'
import { MySubmissions } from './MySubmissions'
import { NeedsLogin } from './NeedsLogin'
import { Receipt } from './Receipt'
import { TemplateForm } from './TemplateForm'
import { useFocusHeading } from './useFocusHeading'

const link = 'font-semibold text-brand-strong underline underline-offset-4'

export function SubmissionsPage() {
  const { sessionId } = useCaseState()
  const [params, setParams] = useSearchParams()
  const [receipt, setReceipt] = useState<SubmissionReceipt | null>(null)

  const widok = params.get('widok')
  const kind = widok === 'formularz' ? findKind(params.get('szablon')) : undefined
  const view = !sessionId
    ? 'login'
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
    lista: { title: 'Wnioski i zgłoszenia', description: 'Wybierz, co chcesz przygotować. Wypełnisz formularz, a my wygenerujemy plik PDF i numer zgłoszenia.' },
    moje: { title: 'Moje zgłoszenia', description: 'Zgłoszenia złożone w tej sesji (symulacja).' },
    zaswiadczenie: { title: 'Zaświadczenie o stanie składek (symulacja)' },
    potwierdzenie: { title: 'Zgłoszenie przyjęte (symulacja)', description: 'Zapisz numer zgłoszenia. Poniżej pobierzesz wniosek w PDF.' },
    login: { title: 'Wnioski i zgłoszenia' },
  }
  const header = kind ? { title: kind.title, description: `Adresat: ${kind.recipient}.` } : headers[view]

  return (
    <div ref={ref} className="mx-auto max-w-2xl space-y-6">
      <PageHeader title={header.title} description={header.description} eyebrow={view === 'lista' || view === 'login' ? 'Symulacja' : undefined} />

      {view === 'login' ? <NeedsLogin /> : null}

      {sessionId && view === 'lista' ? <KindList /> : null}

      {sessionId && kind ? (
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

      {sessionId && view !== 'lista' ? (
        <Button asChild variant="link">
          <Link to={paths.submissions}>← Wróć do listy wniosków</Link>
        </Button>
      ) : null}
    </div>
  )
}

function FormView({ kind, sessionId, onDone }: { kind: SubmissionKind; sessionId: string; onDone: (receipt: SubmissionReceipt) => void }) {
  const query = useQuery({ queryKey: ['submissions', 'templates'], queryFn: fetchTemplates, staleTime: Infinity })

  if (query.isPending) return <SkeletonText lines={6} />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} retrying={query.isFetching} />

  const template = query.data.find((item) => item.id === kind.id)
  if (!template) {
    return <ErrorState title="Nie znaleziono tego formularza" description="Serwer nie zwrócił szablonu dla wybranego wniosku. Wróć do listy i spróbuj ponownie." />
  }

  return (
    <div className="space-y-6">
      <SimulationBanner>Ten wniosek jest symulacją. Nic nie zostanie wysłane do adresata.</SimulationBanner>
      {kind.note ? <KindNote note={kind.note} /> : null}
      <TemplateForm key={template.id} template={template} sessionId={sessionId} onDone={onDone} />
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

function KindList() {
  return (
    <div className="space-y-8">
      <SimulationBanner>Każdy wniosek tutaj to symulacja. Nic nie zostanie wysłane do pracodawcy, ZUS ani urzędu.</SimulationBanner>

      <Section title="Wnioski o urlop do pracodawcy" description="Cztery wzory wniosków. Każdy kończy się plikiem PDF do wydrukowania lub przekazania.">
        <ul className="space-y-4">
          {leaveKinds.map((kind) => (
            <li key={kind.id}>
              <KindCard kind={kind} />
            </li>
          ))}
        </ul>
      </Section>

      <Section title="Zasiłek, zapytania i dokumenty">
        <ul className="space-y-4">
          {otherKinds.map((kind) => (
            <li key={kind.id}>
              <KindCard kind={kind} />
            </li>
          ))}
          <li>
            <Card as="article" className="space-y-3">
              <SimulationBadge />
              <h3 className="text-lg font-bold text-ink">Zaświadczenie o stanie składek</h3>
              <p className="text-base text-ink">Pokazuje, czy na wybrany dzień masz zaległe składki. Pobierzesz je jako PDF.</p>
              <p className="text-sm font-semibold text-ink-soft">Adresat: ZUS</p>
              <p className="text-sm text-ink-soft">Symulacja — nic nie zostanie wysłane.</p>
              <Button asChild variant="secondary">
                <Link to={`${paths.submissions}?widok=zaswiadczenie`}>Wybierz dzień i pobierz</Link>
              </Button>
            </Card>
          </li>
        </ul>
      </Section>

      <Section title="Twoje zgłoszenia">
        <Button asChild variant="secondary">
          <Link to={`${paths.submissions}?widok=moje`}>Zobacz moje zgłoszenia</Link>
        </Button>
      </Section>
    </div>
  )
}

function KindCard({ kind }: { kind: SubmissionKind }) {
  return (
    <Card as="article" className="space-y-3">
      <SimulationBadge />
      <h3 className="text-lg font-bold text-ink">{kind.title}</h3>
      <p className="text-base text-ink">{kind.description}</p>
      {kind.note ? <KindNote note={kind.note} /> : null}
      <p className="text-sm font-semibold text-ink-soft">Adresat: {kind.recipient}</p>
      <p className="text-sm text-ink-soft">Symulacja — nic nie zostanie wysłane.</p>
      <Button asChild>
        <Link to={`${paths.submissions}?widok=formularz&szablon=${kind.id}`}>
          Wypełnij<span className="sr-only">: {kind.title}</span>
        </Link>
      </Button>
    </Card>
  )
}
