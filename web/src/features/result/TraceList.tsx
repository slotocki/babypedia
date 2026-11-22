import { useState } from 'react'
import type { TraceStep } from '@/app/api/types'
import { Button, CheckIcon, CrossIcon, Disclosure, pluralize, QuestionIcon } from '@/ui'
import { keySteps } from './insights'

const metMeta = {
  true: { label: 'Warunek spełniony', Icon: CheckIcon, className: 'text-ok' },
  false: { label: 'Warunek niespełniony', Icon: CrossIcon, className: 'text-bad' },
  null: { label: 'Brakuje danych, żeby to sprawdzić', Icon: QuestionIcon, className: 'text-warn' },
} as const

function Step({ step }: { step: TraceStep }) {
  const { label, Icon, className } = metMeta[String(step.met) as keyof typeof metMeta]
  return (
    <li className="space-y-1 break-words">
      {step.condition ? <p className="text-base font-semibold text-ink">{step.condition}</p> : null}
      <p className={`flex items-center gap-1.5 text-sm font-semibold ${className}`}>
        <Icon className="size-4 shrink-0" />
        {label}
      </p>
      <p className="text-base text-ink">{step.text}</p>
      <p className="text-sm text-ink-soft">Przepis: {step.basis}</p>
    </li>
  )
}

export function TraceList({ trace }: { trace: TraceStep[] }) {
  const [all, setAll] = useState(false)
  if (trace.length === 0) return null
  const key = keySteps(trace)
  const collapsible = key.length > 0 && key.length < trace.length
  const shown = all || !collapsible ? trace : key
  const hidden = trace.length - key.length
  return (
    <Disclosure title="Ścieżka decyzji" summary={`Jak do tego doszliśmy: ${trace.length} ${pluralize(trace.length, 'krok', 'kroki', 'kroków')}`}>
      <div className="space-y-4">
        {collapsible && !all ? <p className="text-sm text-ink-soft">Najważniejsze kroki. Pozostałe ({hidden}) pokażesz niżej.</p> : null}
        <ol className="space-y-4">
          {shown.map((step, index) => (
            <Step key={`${step.rule}-${index}`} step={step} />
          ))}
        </ol>
        {collapsible ? (
          <Button type="button" variant="link" onClick={() => setAll((v) => !v)} aria-expanded={all}>
            {all ? 'Pokaż tylko najważniejsze kroki' : `Pokaż wszystkie kroki (${trace.length})`}
          </Button>
        ) : null}
      </div>
    </Disclosure>
  )
}
