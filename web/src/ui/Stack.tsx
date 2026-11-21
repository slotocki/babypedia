import type { ComponentProps, ElementType } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from './cn'

const stackVariants = cva('flex min-w-0', {
  variants: {
    gap: { xs: 'gap-1', sm: 'gap-2', md: 'gap-4', lg: 'gap-6', xl: 'gap-10' },
    direction: { column: 'flex-col', row: 'flex-row flex-wrap items-center' },
  },
  defaultVariants: { gap: 'md', direction: 'column' },
})

export type StackProps = ComponentProps<'div'> & VariantProps<typeof stackVariants> & { as?: ElementType }

export function Stack({ as: Tag = 'div', gap, direction, className, ...props }: StackProps) {
  return <Tag className={cn(stackVariants({ gap, direction }), className)} {...props} />
}
