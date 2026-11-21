import { cva } from 'class-variance-authority'

export const buttonVariants = cva(
  'inline-flex min-h-11 min-w-11 items-center justify-center gap-2 rounded-control px-5 py-2.5 text-base font-semibold transition-colors disabled:cursor-not-allowed aria-disabled:cursor-not-allowed',
  {
    variants: {
      variant: {
        primary:
          'bg-brand text-on-brand hover:bg-brand-strong disabled:bg-neutral-soft disabled:text-ink-muted aria-disabled:bg-neutral-soft aria-disabled:text-ink-muted',
        secondary:
          'border-2 border-brand bg-surface text-brand-strong hover:bg-brand-soft disabled:border-line disabled:text-ink-muted disabled:hover:bg-surface',
        link: 'px-1 text-brand-strong underline underline-offset-4 hover:text-brand disabled:text-ink-muted disabled:no-underline',
      },
      fullWidth: { true: 'w-full', false: '' },
    },
    defaultVariants: { variant: 'primary', fullWidth: false },
  },
)
