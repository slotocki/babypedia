import type { TraceStep } from '@/app/api/types'
import { CheckIcon, CrossIcon, Disclosure, pluralize, QuestionIcon } from '@/ui'

const metMeta = {
  true: { label: 'Warunek spełniony: tak', Icon: CheckIcon, className: 'text-ok' },
  false: { label: 'Warunek spełniony: nie', Icon: CrossIcon, className: 'text-bad' },
  null: { label: 'Warunek spełniony: brak danych', Icon: QuestionIcon, className: 'text-warn' },
} as const

export function TraceList({ trace }: { trace: TraceStep[] }) {
  if (trace.length === 0) return null
  return (
    <Disclosure title="Ścieżka decyzji" summary={`Jak do tego doszliśmy: ${trace.length} ${pluralize(trace.length, 'krok', 'kroki', 'kroków')}`}>
      <ol className="space-y-4">
        {trace.map((step, index) => {
          const { label, Icon, className } = metMeta[String(step.met) as keyof typeof metMeta]
          return (
            <li key={`${step.rule}-${index}`} className="space-y-1 break-words">
              {step.condition ? <p className="text-base font-semibold text-ink">{step.condition}</p> : null}
              <p className={`flex items-center gap-1.5 text-sm font-semibold ${className}`}>
                <Icon className="size-4 shrink-0" />
                {label}
              </p>
              <p className="text-base text-ink">{step.text}</p>
              <p className="text-sm text-ink-soft">Podstawa prawna: {step.basis}</p>
            </li>
          )
        })}
      </ol>
    </Disclosure>
  )
}
