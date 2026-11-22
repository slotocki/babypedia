import { useId, useLayoutEffect, useRef, useState, type ChangeEvent, type KeyboardEvent } from 'react'
import { Popover } from 'radix-ui'
import { TextInput, type TextInputProps } from './TextInput'
import { useFieldControl } from './field-context'
import { CalendarIcon } from './icons'
import { cn } from './cn'
import { displayDate, isoDate, maskDate, outwardDate, parseDate } from './date-text'

/**
 * Pole daty. Na zewnątrz (value / onChange / register z React Hook Form) zawsze RRRR-MM-DD, a w polu
 * użytkownik widzi i wpisuje datę po polsku: „15.03.2027”, „15032027” (kropki dostawiamy sami) albo „2027-03-15”.
 * Wartość nieprawidłową (np. „31.02.2027”) przekazujemy dopiero po opuszczeniu pola — walidacja formularza
 * pokaże wtedy komunikat; w trakcie pisania nie zgłaszamy błędów.
 */
export type DateInputProps = Omit<TextInputProps, 'type' | 'suffix'>

const monthNames = Array.from({ length: 12 }, (_, month) =>
  new Intl.DateTimeFormat('pl-PL', { month: 'long', timeZone: 'UTC' }).format(new Date(Date.UTC(2026, month))),
)
const dayLabel = new Intl.DateTimeFormat('pl-PL', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' })
const years = Array.from({ length: 301 }, (_, index) => 1900 + index)
const weekdays = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd']
const display = displayDate
const mask = maskDate
const outward = outwardDate

const monthOf = (value?: string) => {
  const match = /^(\d{4})-(0[1-9]|1[0-2])-\d{2}$/.exec(value ?? '')
  const year = match ? Number(match[1]) : 0
  if (match && year >= 1900 && year <= 2200) return { year, month: Number(match[2]) - 1 }
  const today = new Date()
  return { year: today.getFullYear(), month: today.getMonth() }
}

export function DateInput({
  className,
  ref,
  disabled,
  readOnly,
  min,
  max,
  value,
  defaultValue,
  onChange,
  onBlur,
  onKeyDown,
  name,
  ...props
}: DateInputProps) {
  const carrier = useRef<HTMLInputElement>(null)
  const field = useFieldControl()
  const hintId = useId()
  const controlled = value !== undefined
  const [text, setText] = useState(() => display(String(value ?? defaultValue ?? '')))
  const [seen, setSeen] = useState(value)
  // Kropki dostawiamy, dopóki użytkownik sam nie wpisze separatora albo nie wklei daty.
  const autoMask = useRef(true)
  const [open, setOpen] = useState(false)
  const [view, setView] = useState(() => monthOf())
  const [selected, setSelected] = useState('')
  const { year, month } = view
  const first = (new Date(Date.UTC(year, month, 1)).getUTCDay() + 6) % 7
  const count = new Date(Date.UTC(year, month + 1, 0)).getUTCDate()

  // Pole sterowane: zmiana wartości z zewnątrz (np. podpowiedź, reset) przepisuje tekst.
  if (controlled && value !== seen) {
    setSeen(value)
    if (outward(text) !== String(value ?? '')) setText(display(String(value ?? '')))
  }

  // Pole niesterowane (React Hook Form): wartość początkową wpisuje formularz w ukryte pole.
  useLayoutEffect(() => {
    if (controlled) return
    const initial = carrier.current?.value ?? ''
    if (initial) setText(display(initial))
    // tylko przy montowaniu
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const emit = (next: string) => {
    const node = carrier.current
    if (!node || node.value === next) return
    // To samo zdarzenie co przy pisaniu — działa też dla pól niesterowanych React Hook Form.
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set?.call(node, next)
    node.dispatchEvent(new Event('input', { bubbles: true }))
  }

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const raw = event.target.value
    const native = event.nativeEvent as InputEvent
    const kind = native.inputType ?? ''
    let next = raw
    if (raw.trim() === '') {
      autoMask.current = true
      next = ''
    } else if (kind.startsWith('delete')) {
      next = raw
    } else if (kind !== 'insertText' || (native.data ?? '').length > 1) {
      // Wklejenie, autouzupełnianie albo wpisanie całej daty naraz.
      autoMask.current = false
      const iso = parseDate(raw)
      next = iso ? display(iso) : raw
    } else if (autoMask.current && /[^\d]/.test(native.data ?? '')) {
      // Użytkownik sam wpisuje separator: przy „-” (zapis RRRR-MM-DD) odtwarzamy tekst bez dostawionych kropek.
      autoMask.current = false
      const separator = native.data ?? ''
      next = separator === '.' ? raw : text.replace(/\D/g, '') + separator
    } else if (autoMask.current) {
      next = mask(raw.replace(/\D/g, ''))
    }
    setText(next)
    const iso = parseDate(next)
    if (iso) emit(iso)
    else if (next === '') emit('')
  }

  const flush = () => {
    const iso = parseDate(text)
    if (iso) setText(display(iso))
    emit(outward(text))
  }

  const choose = (iso: string) => {
    autoMask.current = true
    setText(display(iso))
    emit(iso)
    setOpen(false)
  }

  const show = (next: boolean) => {
    if (next) {
      const current = parseDate(text) ?? ''
      setSelected(current)
      setView(monthOf(current))
    }
    setOpen(next)
  }

  const move = (offset: number) => {
    const target = new Date(Date.UTC(year, month + offset, 1))
    if (target.getUTCFullYear() >= 1900 && target.getUTCFullYear() <= 2200) {
      setView({ year: target.getUTCFullYear(), month: target.getUTCMonth() })
    }
  }

  const describedBy = [props['aria-describedby'] ?? field['aria-describedby'], hintId].filter(Boolean).join(' ')

  return (
    <div className={cn('relative min-w-0', className)}>
      <input
        type="text"
        hidden
        tabIndex={-1}
        aria-hidden="true"
        name={name}
        value={value}
        defaultValue={defaultValue}
        onChange={onChange}
        onBlur={onBlur}
        disabled={disabled}
        ref={(node) => {
          carrier.current = node
          if (typeof ref === 'function') {
            const cleanup = ref(node)
            if (cleanup) return () => { carrier.current = null; cleanup() }
          } else if (ref) ref.current = node
        }}
      />
      <TextInput
        placeholder="DD.MM.RRRR"
        autoComplete="off"
        inputMode="numeric"
        enterKeyHint="next"
        {...props}
        aria-describedby={describedBy}
        value={text}
        disabled={disabled}
        readOnly={readOnly}
        className="pr-12"
        onFocus={(event) => {
          // Wartość zmieniona przez formularz (np. reset) — pokaż ją przed edycją.
          const outside = carrier.current?.value ?? ''
          if (!controlled && outside !== outward(text)) setText(display(outside))
          props.onFocus?.(event)
        }}
        onChange={handleChange}
        onBlur={() => {
          flush()
          carrier.current?.dispatchEvent(new FocusEvent('focusout', { bubbles: true }))
        }}
        onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
          if (event.key === 'Enter') flush()
          onKeyDown?.(event)
        }}
      />
      <span id={hintId} className="sr-only">Format: dzień, miesiąc, rok, np. 15.03.2027.</span>
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
                {monthNames.map((monthName, index) => <option key={monthName} value={index}>{monthName}</option>)}
              </select>
              <select aria-label="Rok" value={year} onChange={(event) => setView({ year: Number(event.target.value), month })} className="min-h-9 min-w-0 rounded-control border border-line bg-surface px-1 text-sm">
                {years.map((option) => <option key={option}>{option}</option>)}
              </select>
              <button type="button" aria-label="Następny miesiąc" onClick={() => move(1)} disabled={year === 2200 && month === 11} className="rounded-control hover:bg-brand-soft disabled:opacity-40">›</button>
            </div>
            <div className="grid grid-cols-7 gap-1 text-center text-sm">
              {weekdays.map((weekday) => <span key={weekday} className="text-xs font-semibold text-ink-soft" aria-hidden="true">{weekday}</span>)}
              {Array.from({ length: first }, (_, index) => <span key={`empty-${index}`} />)}
              {Array.from({ length: count }, (_, index) => {
                const date = isoDate(year, month, index + 1)
                return (
                  <button key={date} type="button" aria-label={dayLabel.format(new Date(Date.UTC(year, month, index + 1)))} aria-pressed={selected === date} disabled={(min != null && date < String(min)) || (max != null && date > String(max))}
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
