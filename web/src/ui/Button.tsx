import type { ComponentProps } from 'react'
import type { VariantProps } from 'class-variance-authority'
import { Slot } from 'radix-ui'
import { cn } from './cn'
import { buttonVariants } from './button-variants'
import { Spinner } from './Spinner'

export type ButtonProps = ComponentProps<'button'> &
  VariantProps<typeof buttonVariants> & {
    loading?: boolean
    loadingLabel?: string
    asChild?: boolean
  }

export function Button({
  variant,
  fullWidth,
  loading = false,
  loadingLabel = 'Trwa wysyłanie…',
  asChild = false,
  disabled,
  type = 'button',
  className,
  children,
  ...props
}: ButtonProps) {
  const classes = cn(buttonVariants({ variant, fullWidth }), className)

  if (asChild) {
    return (
      <Slot.Root className={classes} {...props}>
        {children}
      </Slot.Root>
    )
  }

  return (
    <button type={type} className={classes} disabled={disabled || loading} aria-busy={loading || undefined} {...props}>
      {loading ? <Spinner label={loadingLabel} /> : null}
      {children}
    </button>
  )
}
