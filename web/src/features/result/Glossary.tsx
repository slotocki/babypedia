import { glossary } from './insights'

/** Krótkie objaśnienia skrótów i pojęć, które występują w podanych tekstach (np. ZAM, e-ZLA, płatnik składek). */
export function Glossary({ texts, title = 'Co znaczą te pojęcia' }: { texts: string[]; title?: string }) {
  const all = texts.join('\n')
  const terms = glossary.filter((g) => g.test.test(all))
  if (terms.length === 0) return null
  return (
    <div className="space-y-1 rounded-control bg-canvas p-3">
      <p className="text-sm font-semibold text-ink">{title}</p>
      <dl className="space-y-1 text-sm text-ink">
        {terms.map((g) => (
          <div key={g.term}>
            <dt className="inline font-semibold">{g.term}</dt>
            <dd className="inline"> — {g.text}</dd>
          </div>
        ))}
      </dl>
    </div>
  )
}
