import { Link } from 'react-router'
import type { BenefitResult } from '@/app/api/types'
import { paths } from '@/app/paths'
import { Card, Section, formatMoney } from '@/ui'
import { ExtLink } from './ExtLink'
import { contactChannels } from './links'
import { linkClass } from './link-class'

export function FuneralBlock({
  benefit,
  hasAssessment,
  stillbirth,
  highlighted,
}: {
  benefit?: BenefitResult
  hasAssessment: boolean
  stillbirth: boolean
  highlighted: boolean
}) {
  const amount = benefit?.status === 'przysługuje' ? benefit.amount?.total : undefined
  const deadline = benefit?.deadlines.find((d) => d.id === 'wniosek_z12') ?? benefit?.deadlines[0]

  return (
    <Section
      title="Zasiłek pogrzebowy"
      description="Pomoc w pokryciu kosztów pochówku. Zasiłek wypłaca ZUS."
    >
      <Card tone={highlighted ? 'highlight' : 'muted'} className="space-y-3">
        <ul className="list-disc space-y-2 pl-5 text-ink">
          <li>
            <strong>Komu przysługuje:</strong> osobie, która zorganizowała pogrzeb i pokryła jego koszty.
            {stillbirth
              ? ' Jeśli dziecko urodziło się martwe, zasiłek przysługuje bez względu na to, jak długo trwała ciąża.'
              : ' Poradnik ZUS pisze, że zasiłek przysługuje, gdy dziecko urodzi się martwe lub po jego śmierci.'}
          </li>
          <li>
            <strong>Kwota:</strong>{' '}
            {amount !== undefined
              ? `${formatMoney(amount)} (wartość z wyniku oceny). Rodzicom przysługuje pełna kwota, nawet jeśli koszty pogrzebu były niższe.`
              : 'kwotę i szczegóły sprawdzisz na zus.pl lub w ZUS. Poradnik ZUS podaje, że kwota nie zależy od tego, ile kosztował pogrzeb.'}
          </li>
          <li>
            <strong>Wniosek:</strong> druk Z-12, razem z rachunkami za pogrzeb. Jeśli nie sporządzono aktu urodzenia z adnotacją o martwym urodzeniu, potrzebna jest karta zgonu albo zaświadczenie o martwym urodzeniu wydane przez lekarza lub położną.
          </li>
          <li>
            <strong>Termin:</strong> do 12 miesięcy od dnia pochówku.
            {deadline ? ` W wyniku oceny: ${deadline.text}` : ''}
          </li>
          <li>
            <strong>Gdzie złożyć:</strong> w ZUS. W razie pytań:
            <ul className="mt-1 list-none space-y-1 pl-0">
              {contactChannels.map((c) => (
                <li key={c.id}>{c.node}</li>
              ))}
            </ul>
          </li>
        </ul>
        <p className="text-sm text-ink-soft">Źródło: Poradnik ZUS 2026, s. 33 i 35–36. Ostateczną decyzję podejmuje ZUS.</p>
        <p className="flex flex-wrap gap-x-6 gap-y-2">
          <ExtLink url="https://www.zus.pl">Strona ZUS (zus.pl)</ExtLink>
          {hasAssessment ? (
            <Link to={paths.result} className={linkClass}>
              Zobacz pełny wynik oceny
            </Link>
          ) : null}
        </p>
      </Card>
    </Section>
  )
}
