import type { ReactNode } from 'react'
import { Popover as PopoverPrimitive } from 'radix-ui'
import { QuestionIcon } from './icons'

export interface HelpTipProps {
  /** Wyjaśnienie pokazywane po kliknięciu lub dotknięciu ikonki „?”. */
  children: ReactNode
  /** Dostępna nazwa przycisku, np. „Dlaczego pytamy o datę urodzenia?”. */
  label?: string
}

/**
 * Mała ikonka „?” przy etykiecie pola. Dłuższe wyjaśnienie („dlaczego o to pytamy”) chowa się w dymku,
 * żeby formularz nie zamieniał się w ścianę tekstu. Popover zamiast tooltipa — działa też na dotyk i z klawiatury.
 */
export function HelpTip({ children, label = 'Wyjaśnienie' }: HelpTipProps) {
  return (
    <PopoverPrimitive.Root>
      <PopoverPrimitive.Trigger asChild>
        <button
          type="button"
          aria-label={label}
          className="-my-1 inline-grid size-8 shrink-0 place-items-center rounded-full text-ink-soft hover:bg-brand-soft hover:text-brand-strong focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-brand data-[state=open]:bg-brand-soft data-[state=open]:text-brand-strong"
        >
          <QuestionIcon className="size-[1.15rem]" aria-hidden="true" />
        </button>
      </PopoverPrimitive.Trigger>
      <PopoverPrimitive.Portal>
        <PopoverPrimitive.Content
          side="top"
          align="start"
          sideOffset={6}
          collisionPadding={12}
          className="z-50 max-w-[min(22rem,calc(100vw-1.5rem))] animate-fade-in rounded-control border border-line bg-surface px-3.5 py-3 text-sm font-normal leading-relaxed text-ink shadow-overlay"
        >
          {children}
          <PopoverPrimitive.Arrow className="fill-surface" width={12} height={6} />
        </PopoverPrimitive.Content>
      </PopoverPrimitive.Portal>
    </PopoverPrimitive.Root>
  )
}
