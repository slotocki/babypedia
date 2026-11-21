import { cn } from './cn'

export function Spinner({ label = 'Ładowanie…', className }: { label?: string | null; className?: string }) {
  return (
    <span role={label ? 'status' : undefined} className="inline-flex items-center">
      <svg
        viewBox="0 0 24 24"
        className={cn('size-5 animate-spin-slow', className)}
        fill="none"
        stroke="currentColor"
        strokeWidth="3"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="9" opacity="0.25" />
        <path d="M21 12a9 9 0 0 0-9-9" strokeLinecap="round" />
      </svg>
      {label ? <span className="sr-only">{label}</span> : null}
    </span>
  )
}
