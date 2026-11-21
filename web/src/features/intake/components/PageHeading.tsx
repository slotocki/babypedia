import { useEffect, useRef, type ReactNode } from 'react'

export function PageHeading({ step, children }: { step?: string; children: ReactNode }) {
  const ref = useRef<HTMLHeadingElement>(null)

  useEffect(() => {
    window.scrollTo(0, 0)
    ref.current?.focus()
  }, [])

  return (
    <div>
      {step ? <p className="text-sm font-medium text-slate-700">{step}</p> : null}
      <h1
        id="page-title"
        ref={ref}
        tabIndex={-1}
        className="mt-1 text-2xl font-semibold leading-tight text-slate-900 focus:outline-none focus-visible:outline-3 focus-visible:outline-teal-700"
      >
        {children}
      </h1>
    </div>
  )
}
