import { cn } from './cn'

export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden="true" className={cn('h-4 animate-pulse-soft rounded-control bg-line', className)} />
}

export function SkeletonText({ lines = 3, label = 'Ładowanie treści…' }: { lines?: number; label?: string }) {
  return (
    <div role="status" className="space-y-2">
      <span className="sr-only">{label}</span>
      {Array.from({ length: lines }, (_, i) => (
        <Skeleton key={i} className={i === lines - 1 ? 'w-2/3' : 'w-full'} />
      ))}
    </div>
  )
}
