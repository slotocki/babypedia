import { cn } from './cn'
import { CheckIcon } from './icons'

export interface StepperProps {
  steps: string[]
  current: number
  label?: string
  className?: string
}

export function Stepper({ steps, current, label = 'Postęp', className }: StepperProps) {
  return (
    <nav aria-label={label} className={cn('space-y-2', className)}>
      <p className="text-sm font-semibold text-ink-soft">
        Krok {current + 1} z {steps.length}: <span className="text-ink">{steps[current]}</span>
      </p>
      <ol className="flex gap-1.5 sm:gap-3">
        {steps.map((step, index) => {
          const state = index < current ? 'done' : index === current ? 'current' : 'todo'
          return (
            <li key={step} aria-current={state === 'current' ? 'step' : undefined} className="min-w-0 flex-1 space-y-1.5">
              <div
                aria-hidden="true"
                className={cn('h-2 rounded-pill', state === 'todo' ? 'bg-line' : 'bg-brand', state === 'current' && 'ring-2 ring-brand/30')}
              />
              <p className={cn('hidden items-center gap-1 text-sm sm:flex', state === 'current' ? 'font-semibold text-ink' : 'text-ink-soft')}>
                {state === 'done' ? <CheckIcon className="shrink-0 text-ok" /> : null}
                <span className="truncate">{step}</span>
              </p>
              <span className="sr-only">
                {step}
                {state === 'done' ? ' — ukończony' : state === 'current' ? ' — bieżący krok' : ' — kolejny krok'}
              </span>
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
