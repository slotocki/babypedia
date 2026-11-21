import { Link } from 'react-router'
import type { SubmissionReceipt } from '@/app/api/types'
import { api } from '@/app/api/client'
import { paths } from '@/app/paths'
import { Button, Card, SimulationBadge } from '@/ui'
import { dateTimeLabel, recipientLabels, statusLabel, typeLabels } from './kinds'
import { PdfActions } from './PdfActions'

export function Receipt({ receipt, session }: { receipt: SubmissionReceipt; session: string }) {
  return (
    <div className="space-y-6">
      <Card tone="highlight" className="space-y-4">
        <SimulationBadge />
        <div>
          <p className="text-sm font-semibold text-ink-soft">Numer zgłoszenia</p>
          <p className="break-words text-2xl font-bold text-ink">{receipt.number}</p>
        </div>
        <dl className="grid gap-3 sm:grid-cols-2">
          <Item label="Rodzaj" value={typeLabels[receipt.type]} />
          <Item label="Adresat" value={recipientLabels[receipt.recipient]} />
          <Item label="Data złożenia" value={dateTimeLabel(receipt.submittedAt)} />
          <Item label="Status" value={statusLabel(receipt.status)} />
        </dl>
        {receipt.summary ? <Item label="Streszczenie" value={receipt.summary} /> : null}
        <p className="text-base text-ink">Symulacja: nic nie zostało wysłane do adresata. Zgłoszenie jest zapisane tylko w tej sesji demonstracyjnej.</p>
      </Card>

      <PdfActions path={receipt.pdfUrl ?? api.submissions.pdfPath(receipt.id)} fileName={`${receipt.number}.pdf`} session={session} withOpen />

      <Button asChild variant="secondary">
        <Link to={`${paths.submissions}?widok=moje`}>Zobacz moje zgłoszenia</Link>
      </Button>
    </div>
  )
}

function Item({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-sm font-semibold text-ink-soft">{label}</dt>
      <dd className="text-base text-ink">{value}</dd>
    </div>
  )
}
