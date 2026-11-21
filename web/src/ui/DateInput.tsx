import { useRef, useState } from 'react'
import { Popover } from 'radix-ui'
import { TextInput, type TextInputProps } from './TextInput'
import { CalendarIcon } from './icons'
import { cn } from './cn'

export type DateInputProps = Omit<TextInputProps, 'type' | 'suffix'>

const monthNames = Array.from({ length: 12 }, (_, month) =>
  new Intl.DateTimeFormat('pl-PL', { month: 'long', timeZone: 'UTC' }).format(new Date(Date.UTC(2026, month))),
)
const years = Array.from({ length: 301 }, (_, index) => 1900 + index)
const weekdays = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd']
const pad = (value: number) => String(value).padStart(2, '0')
const isoDate = (year: number, month: number, day: number) => `${year}-${pad(month + 1)}-${pad(day)}`
const monthOf = (value?: string) => {
  const match = /^(\d{4})-(0[1-9]|1[0-2])-\d{2}$/.exec(value ?? '')
  const year = match ? Number(match[1]) : 0
  if (match && year >= 1900 && year <= 2200) return { year, month: Number(match[2]) - 1 }
  const today = new Date()
  return { year: today.getFullYear(), month: today.getMonth() }
}

export function DateInput({ className, ref, disabled, readOnly, min, max, ...props }: DateInputProps) {
  const input = useRef<HTMLInputElement>(null)
  const [open, setOpen] = useState(false)
  const [view, setView] = useState(() => monthOf())
  const [selected, setSelected] = useState('')
  const { year, month } = view
  const first = (new Date(Date.UTC(year, month, 1)).getUTCDay() + 6) % 7
  const count = new Date(Date.UTC(year, month + 1, 0)).getUTCDate()

  const show = (next: boolean) => {
    if (next) {
      const value = input.current?.value ?? ''
      setSelected(value)
      setView(monthOf(value))
    }
    setOpen(next)
  }

  const move = (offset: number) => {
    const target = new Date(Date.UTC(year, month + offset, 1))
    if (target.getUTCFullYear() >= 1900 && target.getUTCFullYear() <= 2200) {
      setView({ year: target.getUTCFullYear(), month: target.getUTCMonth() })
    }
  }

  const choose = (value: string) => {
    const field = input.current
    if (!field) return
    // Emit the same input event as typing, including for uncontrolled React Hook Form fields.
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set?.call(field, value)
    field.dispatchEvent(new Event('input', { bubbles: true }))
    setOpen(false)
  }

  return (
    <div className={cn('relative min-w-0', className)}>
      <TextInput
        placeholder="RRRR-MM-DD"
        autoComplete="off"
        {...props}
        disabled={disabled}
        readOnly={readOnly}
        className="pr-12"
        ref={(node) => {
          input.current = node
          if (typeof ref === 'function') {
            const cleanup = ref(node)
            if (cleanup) return () => { input.current = null; cleanup() }
          } else if (ref) ref.current = node
        }}
      />
      <Popover.Root open={open} onOpenChange={show}>
        <Popover.Trigger asChild>
          <button type="button" aria-label="Otwórz kalendarz" disabled={disabled || readOnly} className="absolute inset-y-0 right-1 grid min-w-10 place-items-center rounded-control text-brand-strong hover:bg-brand-soft disabled:opacity-50">
            <CalendarIcon className="size-5" />
          </button>
        </Popover.Trigger>
        <Popover.Portal>
          <Popover.Content aria-label="Wybierz datę" sideOffset={6} align="end" collisionPadding={8} className="z-50 w-72 max-w-[calc(100vw-16px)] space-y-3 rounded-card border border-line bg-surface p-3 text-ink shadow-overlay">
            <div className="grid grid-cols-[2rem_minmax(0,1fr)_4.25rem_2rem] gap-1">
              <button type="button" aria-label="Poprzedni miesiąc" onClick={() => move(-1)} disabled={year === 1900 && month === 0} className="rounded-control hover:bg-brand-soft disabled:opacity-40">‹</button>
              <select aria-label="Miesiąc" value={month} onChange={(event) => setView({ year, month: Number(event.target.value) })} className="min-h-9 min-w-0 rounded-control border border-line bg-surface px-1 text-sm capitalize">
                {monthNames.map((name, index) => <option key={name} value={index}>{name}</option>)}
              </select>
              <select aria-label="Rok" value={year} onChange={(event) => setView({ year: Number(event.target.value), month })} className="min-h-9 min-w-0 rounded-control border border-line bg-surface px-1 text-sm">
                {years.map((option) => <option key={option}>{option}</option>)}
              </select>
              <button type="button" aria-label="Następny miesiąc" onClick={() => move(1)} disabled={year === 2200 && month === 11} className="rounded-control hover:bg-brand-soft disabled:opacity-40">›</button>
            </div>
            <div className="grid grid-cols-7 gap-1 text-center text-sm">
              {weekdays.map((name) => <span key={name} className="text-xs font-semibold text-ink-soft" aria-hidden="true">{name}</span>)}
              {Array.from({ length: first }, (_, index) => <span key={`empty-${index}`} />)}
              {Array.from({ length: count }, (_, index) => {
                const date = isoDate(year, month, index + 1)
                return (
                  <button key={date} type="button" aria-label={date} aria-pressed={selected === date} disabled={(min != null && date < String(min)) || (max != null && date > String(max))}
                    onClick={() => choose(date)}
                    className="aspect-square rounded-control hover:bg-brand-soft aria-pressed:bg-brand aria-pressed:text-on-brand disabled:opacity-35">
                    {index + 1}
                  </button>
                )
              })}
            </div>
          </Popover.Content>
        </Popover.Portal>
      </Popover.Root>
    </div>
  )
}
