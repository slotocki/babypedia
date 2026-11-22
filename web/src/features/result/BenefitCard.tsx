import { useId, type ReactNode } from 'react'
import { Link } from 'react-router'
import type { BenefitPart, BenefitResult, MissingFact } from '@/app/api/types'
import { paths } from '@/app/paths'
import {
  Alert,
  Amount,
  Button,
  Card,
  Disclosure,
  HelpTip,
  InfoIcon,
  Period,
  StatusBadge,
  formatDate,
  formatMoney,
  formatPercent,
  formatWeeks,
} from '@/ui'
import { BasisForm } from './BasisForm'
import { ExtLink } from './ExtLink'
import { Glossary } from './Glossary'
import { SHARED_TOTAL_NOTE, conditionOf, explainLabel, glossary, linksFor, ownShareOf, rateOnlyNote, shownElsewhere } from './insights'
import { basisSourceLabels, benefitName, payerLabels, placeLabels } from './labels'
import { TraceList } from './TraceList'

export interface BenefitCardProps {
  benefit: BenefitResult
  missing: MissingFact[]
  application?: { href: string; label: string } | null
  headingLevel?: 3 | 4
  onSaveBasis: (benefit: BenefitResult, value: number) => void
  busy?: boolean
  /** Sytuacja straty dziecka: bez linków o urlopie rodzicielskim i rodzicielstwie. */
  difficult?: boolean
}

const PAYER_TERM = glossary.find((g) => g.term === 'Płatnik składek')!

function Row({ label, help, children }: { label: string; help?: ReactNode; children: ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="flex items-center gap-1 text-sm text-ink-soft">
        {label}
        {help}
      </dt>
      <dd className="break-words text-base text-ink">{children}</dd>
    </div>
  )
}

function LinkList({ links }: { links: BenefitResult['links'] }) {
  if (links.length === 0) return null
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

function Dates({ from, to }: { from: string; to: string }) {
  return (
    <p className="text-sm text-ink-soft">
      <time dateTime={from}>{formatDate(from)}</time>
      {' – '}
      <time dateTime={to}>{formatDate(to)}</time>
    </p>
  )
}

function Entitled({ benefit, application, onSaveBasis, busy, difficult }: Omit<BenefitCardProps, 'missing' | 'headingLevel'>) {
  const { amount, period, rate, payer, whereToApply, documents, notes, parts, trace, lumpSum } = benefit
  const links = linksFor(benefit.links, difficult === true)
  const save = (value: number) => onSaveBasis(benefit, value)
  const condition = conditionOf(benefit)
  // Bez planu urlopu rodzicielskiego karta ma okres łączny obojga rodziców, a kwotę silnik liczy tylko za część tej osoby.
  const sharedNote = notes.find((note) => note.startsWith(SHARED_TOTAL_NOTE))
  const own = amount && period ? ownShareOf(benefit)?.share : undefined
  const hidden = shownElsewhere(benefit)
  const otherNotes = notes.filter((note) => !hidden.has(note))
  const hasDetails = period || payer || whereToApply || parts.length || documents.length || otherNotes.length || links.length || trace.length
  const glossaryTexts = [...documents, payer ? payerLabels[payer] : '', whereToApply ? placeLabels[whereToApply] : '', application?.label ?? '']

  return (
    <div className="space-y-3">
      {condition ? (
        <p className="break-words text-base text-ink">
          Przysługuje, {condition}
          {rate != null ? (
            <>
              : <strong>{formatPercent(rate)} podstawy wymiaru zasiłku</strong>
            </>
          ) : null}
          . Kwotę policzymy, gdy będą znane daty zwolnienia.
        </p>
      ) : null}

      {!condition && rateOnlyNote(benefit) ? <p className="break-words text-base text-ink">{rateOnlyNote(benefit)}</p> : null}

      {lumpSum != null || amount || period ? (
        <div className="flex flex-wrap items-start gap-x-7 gap-y-3 rounded-card bg-canvas p-4">
          {lumpSum != null ? (
            <div>
              <p className="text-sm text-ink-soft">Kwota jednorazowa</p>
              <Amount value={lumpSum} showNote={benefit.id !== 'pogrzebowy'} />
            </div>
          ) : null}
          {amount ? (
            <div>
              <p className="text-sm text-ink-soft">
                {own ? 'Twoja część — ' : ''}
                {amount.basisSource === 'szacunek_zus' ? (own ? 'szacowana kwota' : 'Szacowana kwota') : period ? (own ? 'łącznie za okres' : 'Łącznie za okres') : 'Kwota'}
              </p>
              <Amount value={amount.total} showNote={benefit.id !== 'pogrzebowy'} />
            </div>
          ) : null}
          {own ? (
            <div>
              <p className="text-sm text-ink-soft">Twoja część urlopu (najwyżej)</p>
              <p className="text-xl font-bold text-ink">{formatWeeks(own.days / 7)}</p>
              <Dates from={own.from} to={own.to} />
            </div>
          ) : period ? (
            <div>
              <p className="text-sm text-ink-soft">{sharedNote ? 'Okres — łącznie dla obojga rodziców' : 'Okres'}</p>
              <p className="text-xl font-bold text-ink">{formatWeeks(period.weeks)}</p>
              <Dates from={period.from} to={period.to} />
            </div>
          ) : null}
        </div>
      ) : null}

      {own && period ? (
        <p className="break-words text-sm text-ink">
          Cały urlop rodzicielski obojga rodziców to {formatWeeks(period.weeks)} ({formatDate(period.from)} – {formatDate(period.to)}).{' '}
          {period.days > own.days ? `${formatWeeks((period.days - own.days) / 7)} z tego może wykorzystać tylko drugi rodzic, więc nie liczymy ich do Twojej kwoty.` : null}
        </p>
      ) : sharedNote ? (
        <p className="break-words text-sm text-ink">{sharedNote}</p>
      ) : null}

      {!amount && period ? (
        <div className="space-y-2">
          <p className="text-sm text-ink-soft">Podaj podstawę wymiaru zasiłku ustaloną przez płatnika składek (np. pracodawcę), żeby zobaczyć kwotę.</p>
          <BasisForm onSave={save} busy={busy} />
        </div>
      ) : null}

      {hasDetails ? (
        <Disclosure title="Szczegóły świadczenia">
          <div className="space-y-4">
            <dl className="grid gap-x-5 gap-y-3 sm:grid-cols-2">
              {period && rate != null ? <Row label="Stawka">{formatPercent(rate)} podstawy</Row> : null}
              {period && amount ? <Row label="Za jeden dzień">{formatMoney(amount.daily)}</Row> : null}
              {period && amount ? <Row label="Podstawa wymiaru zasiłku">{formatMoney(amount.basis)} brutto, {basisSourceLabels[amount.basisSource]}</Row> : null}
              {payer ? (
                <Row
                  label="Kto wypłaca"
                  help={payer === 'płatnik_składek' ? <HelpTip label="Kto to jest płatnik składek?">Płatnik składek — {PAYER_TERM.text}</HelpTip> : undefined}
                >
                  {payerLabels[payer]}
                </Row>
              ) : null}
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
            <Glossary texts={glossaryTexts} />
            <Notes notes={otherNotes} />
            <LinkList links={links} />
            <TraceList trace={trace} />
          </div>
        </Disclosure>
      ) : null}

      {application ? (
        <Button asChild variant="secondary" className="w-full sm:w-auto">
          <Link to={application.href}>{explainLabel(application.label)}</Link>
        </Button>
      ) : null}
    </div>
  )
}

function Refused({ benefit, difficult }: { benefit: BenefitResult; difficult?: boolean }) {
  const { reason, legalBasis, whatWouldChange } = benefit
  const condition = conditionOf(benefit)
  const hidden = shownElsewhere(benefit)
  const notes = benefit.notes.filter((n) => !hidden.has(n))
  const links = linksFor(benefit.links, difficult === true)
  const debt = benefit.trace.some((s) => s.rule === 'zadluzenie_skladkowe' && s.met === false)
  return (
    <div className="space-y-3">
      <p className={reason ? 'break-words text-base text-ink' : 'text-base text-ink-soft'}>
        {condition ? <span className="font-semibold">Gdyby lekarz wystawił Ci teraz zwolnienie (L4): </span> : null}
        {reason ?? 'To świadczenie nie przysługuje w Twojej sytuacji.'}
      </p>
      {whatWouldChange ? (
        <Alert variant="info" title={debt ? 'To da się naprawić' : 'Co musiałoby się zmienić'}>
          {debt ? <p className="mb-1">Zaległość nie zamyka drogi do zasiłku na stałe — wystarczy ją spłacić w terminie.</p> : null}
          {whatWouldChange}
        </Alert>
      ) : null}
      {legalBasis ? <p className="break-words text-sm text-ink-soft">Przepis: {legalBasis}</p> : null}
      <Notes notes={notes} />
      <LinkList links={links} />
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

function Undecided({ benefit, difficult }: { benefit: BenefitResult; difficult?: boolean }) {
  const links = linksFor(benefit.links, difficult === true)
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
      <LinkList links={links} />
    </div>
  )
}

/** Odznaka świadczenia warunkowego (np. „gdy dostaniesz L4”) zamiast „Przysługuje” albo „Nie przysługuje” na dziś. */
function ConditionalBadge() {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-pill border border-info bg-info-soft px-3 py-1 text-sm font-semibold text-info">
      <InfoIcon className="size-4 shrink-0" />
      Gdy dostaniesz L4
    </span>
  )
}

export function BenefitCard({ benefit, missing, application, headingLevel = 3, onSaveBasis, busy, difficult }: BenefitCardProps) {
  const headingId = useId()
  const Heading = headingLevel === 3 ? 'h3' : 'h4'
  const conditional = conditionOf(benefit) !== null && (benefit.status === 'przysługuje' || benefit.status === 'nie_przysługuje')
  return (
    <Card as="article" aria-labelledby={headingId} className="space-y-4 p-4 sm:p-5">
      <header className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-2">
        <Heading id={headingId} className="break-words text-lg font-semibold leading-snug text-ink">
          {benefitName(benefit.id)}
        </Heading>
        {conditional ? <ConditionalBadge /> : <StatusBadge status={benefit.status} />}
      </header>

      {benefit.status === 'przysługuje' ? (
        <Entitled benefit={benefit} application={application} onSaveBasis={onSaveBasis} busy={busy} difficult={difficult} />
      ) : null}
      {benefit.status === 'nie_przysługuje' ? <Refused benefit={benefit} difficult={difficult} /> : null}
      {benefit.status === 'brakuje_danych' ? <MissingData missing={missing} /> : null}
      {benefit.status === 'nie_rozstrzygam' ? <Undecided benefit={benefit} difficult={difficult} /> : null}

      {benefit.status !== 'przysługuje' ? <TraceList trace={benefit.trace} /> : null}
    </Card>
  )
}
