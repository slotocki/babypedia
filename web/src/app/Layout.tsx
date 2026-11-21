import { NavLink, Outlet } from 'react-router'
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

export function Layout() {
  return (
    <div className="flex min-h-dvh flex-col bg-white text-slate-900">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:bg-white focus:p-2">
        Przejdź do treści
      </a>
      <header className="border-b border-slate-200">
        <div className="mx-auto flex max-w-5xl flex-col gap-2 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
          <NavLink to={paths.start} className="text-xl font-bold tracking-tight text-teal-800">
            BabyRadnik
          </NavLink>
          <nav aria-label="Główna nawigacja" className="-mx-1 overflow-x-auto">
            <ul className="flex gap-1 whitespace-nowrap text-sm">
              {nav.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.to === paths.start}
                    className={({ isActive }) =>
                      `block rounded px-2 py-1 underline-offset-4 hover:underline ${isActive ? 'font-semibold text-teal-800 underline' : 'text-slate-700'}`
                    }
                  >
                    {item.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </header>
      <main id="main" className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-slate-200 text-xs text-slate-600">
        <div className="mx-auto max-w-5xl px-4 py-3">
          BabyRadnik informuje, nie orzeka. Wszystkie integracje z systemami zewnętrznymi to symulacja. Dane osób są fikcyjne.
        </div>
      </footer>
    </div>
  )
}
