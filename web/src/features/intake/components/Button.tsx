import type { ButtonHTMLAttributes } from 'react'
import { cx } from './cx'

type Variant = 'primary' | 'secondary' | 'link'

const variants: Record<Variant, string> = {
  primary: 'bg-teal-800 text-white hover:bg-teal-900 border border-teal-800 px-5',
  secondary: 'bg-white text-teal-900 hover:bg-teal-50 border border-teal-800 px-5',
  link: 'bg-transparent text-teal-900 underline underline-offset-4 hover:bg-teal-50 border border-transparent px-2',
}

export function Button({
  variant = 'primary',
  className,
  type = 'button',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant }) {
  return (
    <button
      type={type}
      className={cx(
        'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg py-2 text-base font-semibold disabled:cursor-not-allowed disabled:opacity-60',
        variants[variant],
        className,
      )}
      {...props}
    />
  )
}
