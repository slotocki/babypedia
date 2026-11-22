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

/** Zwykłe linki tekstowe (bez tła i ramki); aktywna strona — kolor marki i podkreślenie. */
const linkClass = (isActive: boolean) =>
  cn(
    'inline-flex min-h-11 items-center gap-1.5 whitespace-nowrap rounded-control px-1 text-sm font-semibold underline-offset-[6px] hover:text-brand-strong hover:underline sm:px-2.5',
    isActive ? 'text-brand-strong underline decoration-2' : 'text-ink-soft',
  )

export function Layout() {
  const { sessionId, caseId } = useCaseState()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas text-ink">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-10 focus:bg-surface focus:p-2">
        Przejdź do treści
      </a>
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-x-4 gap-y-2 px-3 py-2 sm:px-4">
          <NavLink to={paths.start} className="inline-flex min-h-11 shrink-0 items-center gap-2.5 whitespace-nowrap rounded-control text-xl font-bold tracking-tight sm:text-2xl">
            <img src="/kompas-rodzica.png" width={44} height={44} alt="" className="size-10 shrink-0 sm:size-11" />
            <span className="text-[#0d477d]">Kompas <span className="text-[#007a67]">Rodzica</span></span>
          </NavLink>

          {/* Na telefonie: logo w pierwszym wierszu, menu w jednej linii pod nim (Start = logo, więc bez osobnego linku). */}
          <nav aria-label="Główna nawigacja" className="w-full sm:ml-auto sm:w-auto">
            <ul className="flex flex-wrap items-center justify-between gap-x-1 sm:justify-end sm:gap-x-0.5">
              {primaryNav.map(({ to, label }) => (
                <li key={to} className={to === paths.start ? 'hidden sm:block' : undefined}>
                  <NavLink to={to} end className={({ isActive }) => linkClass(isActive)}>{label}</NavLink>
                </li>
              ))}
              {sessionId ? (
                <li>
                  <NavLink to={paths.account} className={({ isActive }) => linkClass(isActive)}>
                    <UserIcon className="size-4" /> {caseId ? 'Sprawa testowa' : 'Moje konto'}
                  </NavLink>
                </li>
              ) : null}
            </ul>
          </nav>
        </div>
      </header>
      <main id="main" className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-line bg-surface text-sm text-ink-soft">
        <div className="mx-auto max-w-5xl space-y-3 px-4 py-4">
          <p>Decyzję podejmuje ZUS lub płatnik składek. Integracje działają w trybie symulacji na fikcyjnych danych.</p>
          <nav aria-label="Dodatkowe odnośniki">
            <ul className="flex flex-wrap gap-x-5">
              <li>
                <NavLink to={paths.cases} className="inline-flex min-h-11 items-center font-semibold text-brand-strong underline underline-offset-4 hover:text-brand">
                  Sprawy ZUS (dla oceniających)
                </NavLink>
              </li>
              <li>
                <NavLink to={paths.scenarios} className="inline-flex min-h-11 items-center font-semibold text-brand-strong underline underline-offset-4 hover:text-brand">
                  Porównaj warianty
                </NavLink>
              </li>
            </ul>
          </nav>
        </div>
      </footer>
    </div>
  )
}
