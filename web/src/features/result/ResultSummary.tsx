import type { BenefitResult, PersonRef } from '@/app/api/types'
import { Card, formatMoney } from '@/ui'
import { conditionOf, highlightedSteps } from './insights'
import { benefitName } from './labels'

const who: Record<PersonRef, { entitled: string; none: string }> = {
  wnioskodawca: { entitled: 'Tobie przysługuje', none: 'Na podstawie podanych danych Tobie nie przysługuje teraz żadne z tych świadczeń.' },
  drugiRodzic: { entitled: 'Drugiemu rodzicowi przysługuje', none: 'Drugiemu rodzicowi nie przysługuje żadne z tych świadczeń.' },
}

function lower(name: string) {
  return name.charAt(0).toLocaleLowerCase('pl') + name.slice(1)
}

function nameWithAmount(b: BenefitResult) {
  if (b.lumpSum != null) return `${lower(benefitName(b.id))} (${formatMoney(b.lumpSum)})`
  return b.amount ? `${lower(benefitName(b.id))} (ok. ${formatMoney(b.amount.total)})` : lower(benefitName(b.id))
}

/**
 * Jedno-dwa zdania na górze wyniku: co komu przysługuje, co zależy od warunku i czego brakuje.
 * Kwoty i statusy pochodzą wprost z kart silnika — tu ich nie liczymy, tylko zbieramy w zdanie.
 */
export function ResultSummary({ benefits, grouped, difficult }: { benefits: BenefitResult[]; grouped: boolean; difficult: boolean }) {
  const persons: PersonRef[] = grouped ? ['wnioskodawca', 'drugiRodzic'] : ['wnioskodawca']
  const lines: string[] = []
  const refused = benefits.filter((b) => b.status === 'nie_przysługuje' && !conditionOf(b))
  const debt = refused.some((b) => b.trace.some((s) => s.rule === 'zadluzenie_skladkowe' && s.met === false))
  for (const person of persons) {
    const mine = benefits.filter((b) => b.person === person)
    if (mine.length === 0) continue
    const entitled = mine.filter((b) => b.status === 'przysługuje' && !conditionOf(b))
    const conditional = mine.filter((b) => conditionOf(b) && (b.status === 'przysługuje' || b.status === 'nie_przysługuje'))
    const missing = mine.filter((b) => b.status === 'brakuje_danych')
    const undecided = mine.filter((b) => b.status === 'nie_rozstrzygam')
    const label = grouped ? who[person].entitled : 'Przysługuje Ci'
    if (entitled.length > 0) lines.push(`${label}: ${entitled.map(nameWithAmount).join('; ')}.`)
    else if (missing.length === 0 && undecided.length === 0 && !difficult && !debt) lines.push(grouped ? who[person].none : who.wnioskodawca.none)
    for (const b of conditional) {
      const condition = conditionOf(b)!
      lines.push(
        b.status === 'przysługuje'
          ? `${benefitName(b.id)}: przysługuje, ${condition}.`
          : `${benefitName(b.id)}: ${condition} — zobacz w karcie, co trzeba najpierw załatwić.`,
      )
    }
    if (missing.length > 0) lines.push(`Do oceny brakuje danych: ${missing.map((b) => lower(benefitName(b.id))).join(', ')}.`)
    if (undecided.length > 0) lines.push(`Tego nie rozstrzygamy, zapytaj ZUS: ${undecided.map((b) => lower(benefitName(b.id))).join(', ')}.`)
  }

  // Ważne wyjaśnienia ze ścieżki decyzji (np. brak ubezpieczenia matki nie blokuje prawa ojca) — raz, na górze.
  const highlights = new Set(benefits.flatMap((b) => highlightedSteps(b).map((s) => s.condition ?? s.text)))
  for (const text of highlights) lines.push(text.endsWith('.') ? text : `${text}.`)
  if (debt) lines.push('Część zasiłków wstrzymuje zaległość w składkach. To da się naprawić — w kartach poniżej piszemy, do kiedy ją spłacić.')

  if (lines.length === 0) return null
  return (
    <Card tone="muted" className="space-y-1 p-4 sm:p-5">
      <h2 className="text-lg font-semibold text-ink">W skrócie</h2>
      {lines.map((line) => (
        <p key={line} className="break-words text-base text-ink">
          {line}
        </p>
      ))}
      <p className="text-sm text-ink-soft">Kwoty są szacunkowe, brutto. Decyzję podejmuje ZUS albo płatnik składek (zwykle pracodawca).</p>
    </Card>
  )
}
