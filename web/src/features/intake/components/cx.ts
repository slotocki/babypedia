export const cx = (...parts: Array<string | false | null | undefined>) => parts.filter(Boolean).join(' ')

export const inputClass =
  'block min-h-11 w-full rounded-lg border border-slate-500 bg-white px-3 py-2 text-base text-slate-900 aria-[invalid=true]:border-2 aria-[invalid=true]:border-red-700'

export function describedBy(id: string, hint?: unknown, error?: string | null) {
  const ids = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean)
  return ids.length ? ids.join(' ') : undefined
}
