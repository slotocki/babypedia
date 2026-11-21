import { useId, type ReactNode } from 'react'
import { Link } from 'react-router'
import type { BenefitPart, BenefitResult, MissingFact } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Alert, Amount, Button, Card, Disclosure, Period, SimulationBadge, StatusBadge, formatMoney, formatPercent } from '@/ui'
import { BasisForm } from './BasisForm'
import { ExtLink } from './ExtLink'
import { basisSourceLabels, benefitName, payerLabels, placeLabels } from './labels'
import { TraceList } from './TraceList'

export interface BenefitCardProps {
  benefit: BenefitResult
  missing: MissingFact[]
  headingLevel?: 3 | 4
  onSaveBasis: (benefit: BenefitResult, value: number) => void
  busy?: boolean
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-sm text-ink-soft">{label}</dt>
      <dd className="break-words text-base text-ink">{children}</dd>
    </div>
  )
}

function LinkList({ links }: { links: BenefitResult['links'] }) {
  return (
    <ul className="space-y-1 text-base">
      {links.map((link) => (
        <li key={link.url}>
          <ExtLink url={link.url}>{link.label}</ExtLink>
        </li>
      ))}
    </ul>
  )
}

function Notes({ notes }: { notes: string[] }) {
  if (notes.length === 0) return null
  return (
    <ul className="list-disc space-y-1 pl-5 text-sm text-ink-soft">
      {notes.map((note) => (
        <li key={note} className="break-words">
          {note}
        </li>
      ))}
    </ul>
  )
}

function Parts({ parts }: { parts: BenefitPart[] }) {
  return (
    <div className="space-y-2">
      <h4 className="text-base font-semibold text-ink">Części okresu</h4>
      <ul className="divide-y divide-line rounded-control border border-line">
        {parts.map((part, index) => (
          <li key={`${part.label}-${index}`} className="grid gap-1 p-3 sm:grid-cols-[1fr_auto] sm:gap-4">
            <div className="min-w-0 space-y-1">
              <p className="font-semibold text-ink">{part.label}</p>
              <Period period={part.period} />
            </div>
            <div className="text-base text-ink sm:text-right">
              <p>Stawka: {formatPercent(part.rate)}</p>
              {part.amount ? (
                <p className="font-semibold">Łącznie: {formatMoney(part.amount.total)}</p>
              ) : (
                <p className="text-ink-soft">Kwota: brak podstawy</p>
              )}
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}

function Entitled({ benefit, onSaveBasis, busy }: Pick<BenefitCardProps, 'benefit' | 'onSaveBasis' | 'busy'>) {
  const { amount, period, rate, payer, whereToApply, documents, links, notes, parts } = benefit
  const save = (value: number) => onSaveBasis(benefit, value)
  return (
    <div className="space-y-4">
      {period ? (
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          <Row label="Okres">
            <Period period={period} />
          </Row>
          {rate != null ? <Row label="Stawka">{formatPercent(rate)} podstawy</Row> : null}
        </dl>
      ) : null}

      {amount ? (
        <div className="space-y-3 rounded-control bg-brand-soft p-3">
          <div className="flex flex-wrap items-end gap-x-8 gap-y-3">
            {period ? (
              <div>
                <p className="text-sm text-ink-soft">Za jeden dzień</p>
                <Amount value={amount.daily} showNote={false} />
              </div>
            ) : null}
            <div>
              <p className="text-sm text-ink-soft">{period ? 'Łącznie za okres' : 'Kwota'}</p>
              <Amount value={amount.total} size="lg" />
            </div>
          </div>
          {period ? (
            <p className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-ink-soft">
              <span>
                Podstawa: {formatMoney(amount.basis)} brutto, {basisSourceLabels[amount.basisSource]}
              </span>
              {amount.basisSource === 'szacunek_zus' ? <SimulationBadge /> : null}
            </p>
          ) : null}
        </div>
      ) : period ? (
        <Alert variant="info" title="Podaj podstawę, żeby zobaczyć kwotę">
          Znamy okres i stawkę, ale bez podstawy nie wyliczymy kwoty.
          {' Wpisz podstawę ustaloną przez płatnika.'}
        </Alert>
      ) : null}

      {!amount && period ? <BasisForm onSave={save} busy={busy} /> : null}
      {amount && period && amount.basisSource !== 'ustalona_przez_płatnika' ? (
        <Disclosure title="Znasz podstawę ustaloną przez płatnika?" summary="Wpisz ją, a przeliczymy kwotę">
          <BasisForm onSave={save} busy={busy} />
        </Disclosure>
      ) : null}

      {parts.length > 0 ? <Parts parts={parts} /> : null}

      {payer || whereToApply ? (
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          {payer ? <Row label="Kto wypłaca">{payerLabels[payer]}</Row> : null}
          {whereToApply ? <Row label="Gdzie złożyć wniosek">{placeLabels[whereToApply]}</Row> : null}
        </dl>
      ) : null}

      {documents.length > 0 ? (
        <div className="space-y-1">
          <h4 className="text-base font-semibold text-ink">Dokumenty</h4>
          <ul className="list-disc space-y-1 pl-5 text-base text-ink">
            {documents.map((doc) => (
              <li key={doc} className="break-words">
                {doc}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <Notes notes={notes} />
      {links.length > 0 ? <LinkList links={links} /> : null}

      <Button asChild variant="secondary" className="w-full sm:w-auto">
        <Link to={paths.submissions}>Przygotuj wniosek</Link>
      </Button>
    </div>
  )
}

function Refused({ benefit }: { benefit: BenefitResult }) {
  const { reason, legalBasis, whatWouldChange, notes, links } = benefit
  return (
    <div className="space-y-3">
      <p className={reason ? 'break-words text-base text-ink' : 'text-base text-ink-soft'}>
        {reason ?? 'To świadczenie nie przysługuje w Twojej sytuacji.'}
      </p>
      {legalBasis ? <p className="break-words text-sm text-ink-soft">Podstawa prawna: {legalBasis}</p> : null}
      {whatWouldChange ? (
        <Alert variant="info" title="Co musiałoby się zmienić">
          {whatWouldChange}
        </Alert>
      ) : null}
      <Notes notes={notes} />
      {links.length > 0 ? <LinkList links={links} /> : null}
    </div>
  )
}

function MissingData({ missing }: { missing: MissingFact[] }) {
  return (
    <div className="space-y-3">
      <p className="text-base text-ink">
        {missing.length > 0
          ? 'Żeby ocenić, czy to świadczenie Ci przysługuje, potrzebujemy odpowiedzi na te pytania:'
          : 'Żeby ocenić, czy to świadczenie Ci przysługuje, potrzebujemy jeszcze kilku informacji.'}
      </p>
      {missing.length > 0 ? (
        <ul className="list-disc space-y-1 pl-5 text-base text-ink">
          {missing.map((m) => (
            <li key={`${m.person ?? 'sprawa'}-${m.fact}`} className="break-words">
              {m.question}
            </li>
          ))}
        </ul>
      ) : null}
      <Button asChild variant="secondary" className="w-full sm:w-auto">
        <Link to={paths.review}>Uzupełnij w „Sprawdź dane”</Link>
      </Button>
    </div>
  )
}

function Undecided({ benefit }: { benefit: BenefitResult }) {
  return (
    <div className="space-y-3">
      <p className="break-words text-base text-ink">
        {benefit.reason ?? 'Ta sytuacja wymaga indywidualnej oceny. BabyRadnik nie rozstrzyga w takich przypadkach.'}
      </p>
      <p className="text-base text-ink">Skontaktuj się z ZUS, żeby potwierdzić swoją sytuację:</p>
      <ul className="list-disc space-y-1 pl-5 text-base text-ink">
        <li>
          infolinia ZUS:{' '}
          <a href="tel:+48225601600" className="font-semibold text-brand-strong underline underline-offset-4">
            22 560 16 00
          </a>
        </li>
        <li>
          strona <ExtLink url="https://www.zus.pl">zus.pl</ExtLink> i najbliższy oddział ZUS
        </li>
      </ul>
      {benefit.links.length > 0 ? <LinkList links={benefit.links} /> : null}
    </div>
  )
}

export function BenefitCard({ benefit, missing, headingLevel = 3, onSaveBasis, busy }: BenefitCardProps) {
  const headingId = useId()
  const Heading = headingLevel === 3 ? 'h3' : 'h4'
  return (
    <Card as="article" aria-labelledby={headingId} className="space-y-4">
      <header className="space-y-2">
        <Heading id={headingId} className="break-words text-lg font-semibold leading-snug text-ink">
          {benefitName(benefit.id)}
        </Heading>
        <StatusBadge status={benefit.status} />
      </header>

      {benefit.status === 'przysługuje' ? <Entitled benefit={benefit} onSaveBasis={onSaveBasis} busy={busy} /> : null}
      {benefit.status === 'nie_przysługuje' ? <Refused benefit={benefit} /> : null}
      {benefit.status === 'brakuje_danych' ? <MissingData missing={missing} /> : null}
      {benefit.status === 'nie_rozstrzygam' ? <Undecided benefit={benefit} /> : null}

      <TraceList trace={benefit.trace} />
    </Card>
  )
}
