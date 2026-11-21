import { useEffect, useId, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { paths } from './paths'

const nav = [
  { to: paths.start, label: 'Start' },
  { to: paths.result, label: 'Wynik' },
  { to: paths.scenarios, label: 'Warianty' },
  { to: paths.submissions, label: 'Wnioski' },
  { to: paths.zusData, label: 'Moje dane w ZUS' },
  { to: paths.cases, label: 'Sprawy ZUS' },
  { to: paths.support, label: 'Wsparcie' },
]

/** Przekierowania S9 (instrukcja, sekcja 4). */
const externalLinks = [
  { href: 'https://www.zus.pl', label: 'ZUS — zus.pl' },
  { href: 'https://www.gov.pl/web/rodzina/urlop-rodzicielski', label: 'Ministerstwo Rodziny, Pracy i Polityki Społecznej — urlop rodzicielski' },
  { href: 'https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo', label: 'Państwowa Inspekcja Pracy — rodzicielstwo' },
]

const linkClass = (isActive: boolean) =>
  `block rounded-control px-3 py-2 underline-offset-4 hover:underline focus-visible:outline-3 focus-visible:outline-offset-2 focus-visible:outline-brand ${
    isActive ? 'font-semibold text-brand-strong underline' : 'text-ink-soft'
  }`

function NavItems({ onNavigate }: { onNavigate?: () => void }) {
  return nav.map((item) => (
    <li key={item.to}>
      <NavLink to={item.to} end={item.to === paths.start} onClick={onNavigate} className={({ isActive }) => linkClass(isActive)}>
        {item.label}
      </NavLink>
    </li>
  ))
}

export function Layout() {
  const [open, setOpen] = useState(false)
  const menuId = useId()
  const location = useLocation()

  useEffect(() => {
    setOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open])

  return (
    <div className="flex min-h-dvh flex-col bg-canvas text-ink">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-10 focus:bg-surface focus:p-2">
        Przejdź do treści
      </a>
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-2 px-4 py-3">
          <NavLink to={paths.start} className="text-xl font-bold tracking-tight text-brand-strong">
            Kompas Rodzica
          </NavLink>

          <button
            type="button"
            className="inline-flex min-h-11 items-center gap-2 rounded-control border-2 border-brand bg-surface px-4 py-2 text-base font-semibold text-brand-strong hover:bg-brand-soft focus-visible:outline-3 focus-visible:outline-offset-2 focus-visible:outline-brand md:hidden"
            aria-expanded={open}
            aria-controls={menuId}
            onClick={() => setOpen((v) => !v)}
          >
            <span aria-hidden="true" className="text-lg leading-none">
              {open ? '×' : '≡'}
            </span>
            Menu
          </button>

          <nav aria-label="Główna nawigacja" className="hidden md:block">
            <ul className="flex flex-wrap gap-1 text-sm">
              <NavItems />
            </ul>
          </nav>

          {open ? (
            <nav id={menuId} aria-label="Główna nawigacja (menu)" className="w-full md:hidden">
              <ul className="space-y-1 border-t border-line pt-2 text-base">
                <NavItems onNavigate={() => setOpen(false)} />
              </ul>
            </nav>
          ) : null}
        </div>
      </header>
      <main id="main" className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-line bg-surface text-sm text-ink-soft">
        <div className="mx-auto max-w-5xl space-y-3 px-4 py-4">
          <nav aria-label="Przydatne strony">
            <ul className="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:gap-x-6">
              {externalLinks.map((link) => (
                <li key={link.href}>
                  <a href={link.href} target="_blank" rel="noreferrer" className="font-semibold text-brand-strong underline underline-offset-4 hover:text-brand">
                    {link.label}
                    <span className="sr-only"> (otwiera się w nowej karcie)</span>
                  </a>
                </li>
              ))}
            </ul>
          </nav>
          <p>Kompas Rodzica informuje, nie orzeka. Wszystkie integracje z systemami zewnętrznymi to symulacja. Dane osób są fikcyjne.</p>
        </div>
      </footer>
    </div>
  )
}
