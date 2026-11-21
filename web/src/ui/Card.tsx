import type { ComponentProps, ElementType } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from './cn'

const cardVariants = cva('rounded-card border p-4 sm:p-6', {
  variants: {
    tone: {
      default: 'border-line bg-surface shadow-card',
      muted: 'border-line bg-neutral-soft',
      highlight: 'border-2 border-brand bg-brand-soft',
    },
  },
  defaultVariants: { tone: 'default' },
})

export type CardProps = ComponentProps<'div'> & VariantProps<typeof cardVariants> & { as?: ElementType }

export function Card({ as: Tag = 'div', tone, className, ...props }: CardProps) {
  return <Tag className={cn(cardVariants({ tone }), className)} {...props} />
}
