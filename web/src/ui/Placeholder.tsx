import type { ReactNode } from 'react'

export function Placeholder({ title, screen, branch, children }: { title: string; screen: number; branch: string; children?: ReactNode }) {
  return (
    <section aria-labelledby="page-title">
      <h1 id="page-title" className="text-2xl font-semibold">{title}</h1>
      <p className="mt-2 text-slate-700">
        Ekran {screen} — zaślepka. Powstaje na gałęzi <code>{branch}</code>.
      </p>
      {children}
    </section>
  )
}
