import { useEffect, useId, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { ChevronDownIcon, cn } from '@/ui'
import { paths } from './paths'

const primaryNav = [
  { to: paths.start, label: 'Start' },
  { to: paths.result, label: 'Wynik' },
]

const additionalNav = [
  { to: paths.submissions, label: 'Wnioski' },
  { to: paths.scenarios, label: 'Porównaj warianty' },
  { to: paths.zusData, label: 'Moje dane w ZUS' },
  { to: paths.cases, label: 'Sprawy ZUS' },
]

/** Przekierowania S9 (instrukcja, sekcja 4). */
const externalLinks = [
  { href: 'https://www.zus.pl', label: 'ZUS — zus.pl' },
  { href: 'https://www.gov.pl/web/rodzina/urlop-rodzicielski', label: 'Ministerstwo Rodziny, Pracy i Polityki Społecznej — urlop rodzicielski' },
  { href: 'https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo', label: 'Państwowa Inspekcja Pracy — rodzicielstwo' },
]

const linkClass = (isActive: boolean) =>
  cn(
    'flex min-h-11 items-center rounded-control px-2.5 py-2 text-sm font-semibold hover:bg-brand-soft hover:text-brand-strong sm:px-3',
    isActive ? 'bg-brand-soft text-brand-strong' : 'text-ink-soft',
  )

export function Layout() {
  const [open, setOpen] = useState(false)
  const menuId = useId()
  const toggleRef = useRef<HTMLButtonElement>(null)
  const location = useLocation()

  useEffect(() => {
    setOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        toggleRef.current?.focus()
      }
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
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-x-2 px-4 py-2">
          <NavLink to={paths.start} onClick={() => setOpen(false)} className="inline-flex min-h-11 items-center text-lg font-bold tracking-tight text-brand-strong sm:text-xl">
            Kompas Rodzica
          </NavLink>

          <nav aria-label="Główna nawigacja">
            <ul className="flex items-center gap-0.5 sm:gap-1">
              {primaryNav.map(({ to, label }) => (
                <li key={to} className="hidden sm:block">
                  <NavLink to={to} end onClick={() => setOpen(false)} className={({ isActive }) => linkClass(isActive)}>{label}</NavLink>
                </li>
              ))}
              <li><NavLink to={paths.support} onClick={() => setOpen(false)} className={({ isActive }) => linkClass(isActive)}>Wsparcie</NavLink></li>
              <li>
                <button
                  ref={toggleRef}
                  type="button"
                  className={cn(linkClass(open || additionalNav.some(({ to }) => to === location.pathname)), 'gap-1')}
                  aria-expanded={open}
                  aria-controls={menuId}
                  onClick={() => setOpen((value) => !value)}
                >
                  Więcej <ChevronDownIcon className={cn('size-4', open && 'rotate-180')} />
                </button>
              </li>
            </ul>
          </nav>

          <nav id={menuId} aria-label="Pozostałe funkcje" hidden={!open} className="w-full border-t border-line pt-2">
            <ul className="grid gap-1 sm:flex sm:flex-wrap sm:justify-end">
              {[...primaryNav, ...additionalNav].map(({ to, label }) => (
                <li key={to} className={primaryNav.some((item) => item.to === to) ? 'sm:hidden' : undefined}>
                  <NavLink to={to} end onClick={() => setOpen(false)} className={({ isActive }) => linkClass(isActive)}>{label}</NavLink>
                </li>
              ))}
            </ul>
          </nav>
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
          <p>Decyzję podejmuje ZUS lub płatnik składek. Integracje działają w trybie symulacji na fikcyjnych danych.</p>
        </div>
      </footer>
    </div>
  )
}
