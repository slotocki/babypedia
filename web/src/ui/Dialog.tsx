import type { ReactNode } from 'react'
import { Dialog as DialogPrimitive } from 'radix-ui'
import { CrossIcon } from './icons'

export interface DialogProps {
  title: ReactNode
  description?: ReactNode
  trigger?: ReactNode
  open?: boolean
  onOpenChange?: (open: boolean) => void
  footer?: ReactNode
  children?: ReactNode
}

export function Dialog({ title, description, trigger, open, onOpenChange, footer, children }: DialogProps) {
  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      {trigger ? <DialogPrimitive.Trigger asChild>{trigger}</DialogPrimitive.Trigger> : null}
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-40 animate-fade-in bg-ink/60" />
        <DialogPrimitive.Content
          {...(description ? {} : { 'aria-describedby': undefined })}
          className="fixed inset-x-0 bottom-0 z-50 max-h-[90dvh] animate-fade-in overflow-y-auto rounded-t-card bg-surface p-5 shadow-overlay sm:inset-auto sm:left-1/2 sm:top-1/2 sm:w-full sm:max-w-lg sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-card"
        >
          <div className="flex items-start justify-between gap-3">
            <DialogPrimitive.Title className="text-xl font-semibold text-ink">{title}</DialogPrimitive.Title>
            <DialogPrimitive.Close
              aria-label="Zamknij okno"
              className="-mr-2 -mt-2 grid size-11 shrink-0 place-items-center rounded-control text-ink-soft hover:bg-neutral-soft"
            >
              <CrossIcon className="size-6" />
            </DialogPrimitive.Close>
          </div>
          {description ? <DialogPrimitive.Description className="mt-1 text-ink-soft">{description}</DialogPrimitive.Description> : null}
          {children ? <div className="mt-4">{children}</div> : null}
          {footer ? <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">{footer}</div> : null}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  )
}
