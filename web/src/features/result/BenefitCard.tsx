import { useId, type ReactNode } from 'react'
import { Link } from 'react-router'
import type { BenefitPart, BenefitResult, MissingFact } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Alert, Amount, Button, Card, Disclosure, Period, StatusBadge, formatDate, formatMoney, formatPercent, formatWeeks } from '@/ui'
import { BasisForm } from './BasisForm'
import { ExtLink } from './ExtLink'
import { basisSourceLabels, benefitName, payerLabels, placeLabels } from './labels'
import { TraceList } from './TraceList'

/** Początek uwagi silnika (RodzicielskiRules.DefaultBlock) o pokazaniu wymiaru łącznego obojga rodziców. */
const SHARED_TOTAL_NOTE = 'Pokazany jest łączny wymiar'

export interface BenefitCardProps {
  benefit: BenefitResult
  missing: MissingFact[]
  application?: { href: string; label: string } | null
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

function Entitled({ benefit, application, onSaveBasis, busy }: Pick<BenefitCardProps, 'benefit' | 'application' | 'onSaveBasis' | 'busy'>) {
  const { amount, period, rate, payer, whereToApply, documents, links, notes, parts, trace, lumpSum } = benefit
  const save = (value: number) => onSaveBasis(benefit, value)
  // Bez planu urlopu rodzicielskiego silnik pokazuje wymiar łączny obojga rodziców (uwaga z modułu okresów).
  // Uwagi są schowane w szczegółach, więc tę jedną pokazujemy od razu przy okresie i kwocie.
  const sharedNote = notes.find((note) => note.startsWith(SHARED_TOTAL_NOTE))
  const hasDetails = period || payer || whereToApply || parts.length || documents.length || notes.length || links.length || trace.length

  return (
    <div className="space-y-3">
      {lumpSum != null || amount || period ? (
        <div className="flex flex-wrap items-start gap-x-7 gap-y-3 rounded-card bg-canvas p-4">
          {lumpSum != null ? (
            <div>
              <p className="text-sm text-ink-soft">Kwota jednorazowa</p>
              <Amount value={lumpSum} />
            </div>
          ) : null}
          {amount ? (
            <div>
              <p className="text-sm text-ink-soft">
                {amount.basisSource === 'szacunek_zus' ? 'Szacowana kwota' : period ? 'Łącznie za okres' : 'Kwota'}
                {sharedNote ? ' — dla obojga rodziców' : ''}
              </p>
              <Amount value={amount.total} />
            </div>
          ) : null}
          {period ? (
            <div>
              <p className="text-sm text-ink-soft">{sharedNote ? 'Okres — łącznie dla obojga rodziców' : 'Okres'}</p>
              <p className="text-xl font-bold text-ink">{formatWeeks(period.weeks)}</p>
              <p className="text-sm text-ink-soft">
                <time dateTime={period.from}>{formatDate(period.from)}</time>{' – '}
                <time dateTime={period.to}>{formatDate(period.to)}</time>
              </p>
            </div>
          ) : null}
        </div>
      ) : null}

      {sharedNote ? <p className="break-words text-sm text-ink">{sharedNote}</p> : null}

      {!amount && period ? (
        <div className="space-y-2">
          <p className="text-sm text-ink-soft">Podaj podstawę ustaloną przez płatnika, żeby zobaczyć kwotę.</p>
          <BasisForm onSave={save} busy={busy} />
        </div>
      ) : null}

      {hasDetails ? (
        <Disclosure title="Szczegóły świadczenia">
          <div className="space-y-4">
            <dl className="grid gap-x-5 gap-y-3 sm:grid-cols-2">
              {period && rate != null ? <Row label="Stawka">{formatPercent(rate)} podstawy</Row> : null}
              {period && amount ? <Row label="Za jeden dzień">{formatMoney(amount.daily)}</Row> : null}
              {period && amount ? <Row label="Podstawa">{formatMoney(amount.basis)} brutto, {basisSourceLabels[amount.basisSource]}</Row> : null}
              {payer ? <Row label="Kto wypłaca">{payerLabels[payer]}</Row> : null}
              {whereToApply ? <Row label="Gdzie złożyć wniosek">{placeLabels[whereToApply]}</Row> : null}
            </dl>
            {amount && period && amount.basisSource !== 'ustalona_przez_płatnika' ? (
              <Disclosure title="Podaj podstawę ustaloną przez płatnika">
                <BasisForm onSave={save} busy={busy} />
              </Disclosure>
            ) : null}
            {parts.length > 0 ? <Parts parts={parts} /> : null}
            {documents.length > 0 ? (
              <div className="space-y-1">
                <h4 className="text-base font-semibold text-ink">Dokumenty</h4>
                <ul className="list-disc space-y-1 pl-5">
                  {documents.map((doc) => <li key={doc} className="break-words">{doc}</li>)}
                </ul>
              </div>
            ) : null}
            <Notes notes={notes.filter((note) => note !== sharedNote)} />
            {links.length > 0 ? <LinkList links={links} /> : null}
            <TraceList trace={trace} />
          </div>
        </Disclosure>
      ) : null}

      {application ? (
        <Button asChild variant="secondary" className="w-full sm:w-auto">
          <Link to={application.href}>{application.label}</Link>
        </Button>
      ) : null}
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
        {benefit.reason ?? 'Ta sytuacja wymaga indywidualnej oceny. Kompas Rodzica nie rozstrzyga w takich przypadkach.'}
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

export function BenefitCard({ benefit, missing, application, headingLevel = 3, onSaveBasis, busy }: BenefitCardProps) {
  const headingId = useId()
  const Heading = headingLevel === 3 ? 'h3' : 'h4'
  return (
    <Card as="article" aria-labelledby={headingId} className="space-y-4 p-4 sm:p-5">
      <header className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-2">
        <Heading id={headingId} className="break-words text-lg font-semibold leading-snug text-ink">
          {benefitName(benefit.id)}
        </Heading>
        <StatusBadge status={benefit.status} />
      </header>

      {benefit.status === 'przysługuje' ? <Entitled benefit={benefit} application={application} onSaveBasis={onSaveBasis} busy={busy} /> : null}
      {benefit.status === 'nie_przysługuje' ? <Refused benefit={benefit} /> : null}
      {benefit.status === 'brakuje_danych' ? <MissingData missing={missing} /> : null}
      {benefit.status === 'nie_rozstrzygam' ? <Undecided benefit={benefit} /> : null}

      {benefit.status !== 'przysługuje' ? <TraceList trace={benefit.trace} /> : null}
    </Card>
  )
}
