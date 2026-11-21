import { NavLink, Outlet } from 'react-router'
import { UserIcon, cn } from '@/ui'
import { paths } from './paths'
import { useCaseState } from './case-context'

const primaryNav = [
  { to: paths.start, label: 'Start' },
  { to: paths.submissions, label: 'Wnioski' },
  { to: paths.support, label: 'Wsparcie' },
  { to: paths.knowledge, label: 'Baza wiedzy' },
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
  const { sessionId } = useCaseState()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas text-ink">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-10 focus:bg-surface focus:p-2">
        Przejdź do treści
      </a>
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-x-4 gap-y-2 px-3 py-2 sm:px-4">
          <NavLink to={paths.start} className="inline-flex min-h-11 shrink-0 items-center gap-2 whitespace-nowrap rounded-control text-base font-bold tracking-tight sm:gap-2.5 sm:text-xl">
            <img src="/kompas-rodzica.png" width={36} height={36} alt="" className="size-8 shrink-0 sm:size-9" />
            <span className="text-[#0d477d]">Kompas <span className="text-[#007a67]">Rodzica</span></span>
          </NavLink>

          <nav aria-label="Główna nawigacja" className="order-3 w-full sm:order-0 sm:w-auto">
            <ul className="grid grid-cols-4 gap-1 sm:flex">
              {primaryNav.map(({ to, label }) => (
                <li key={to}>
                  <NavLink to={to} end className={({ isActive }) => cn(linkClass(isActive), 'justify-center bg-canvas/60 px-1 text-center text-xs sm:px-3 sm:text-sm', isActive && 'bg-brand-soft')}>{label}</NavLink>
                </li>
              ))}
            </ul>
          </nav>

          {sessionId ? (
            <NavLink to={paths.account} className={({ isActive }) => cn(linkClass(isActive), 'ml-auto gap-1.5 bg-canvas/60 whitespace-nowrap', isActive && 'bg-brand-soft')}>
              <UserIcon className="size-4" /> Moje konto
            </NavLink>
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
          <p>Decyzję podejmuje ZUS lub płatnik składek. Integracje działają w trybie symulacji na fikcyjnych danych.</p>
        </div>
      </footer>
    </div>
  )
}
