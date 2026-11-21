import { Link } from 'react-router'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { Button, FileIcon, ShieldIcon } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

export function StartPage() {
  const { facts, reset } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()

  return (
    <div ref={ref} className="mx-auto max-w-4xl py-4 sm:py-12">
      <header className="mb-7 max-w-2xl space-y-3 sm:mb-10">
        <h1 tabIndex={-1} className="text-4xl font-bold tracking-tight text-ink sm:text-5xl">Sprawdź, co Ci przysługuje</h1>
        <p className="max-w-xl text-lg text-ink-soft">Sprawdź świadczenia związane z rodzicielstwem, ważne terminy i potrzebne wnioski.</p>
      </header>

      <div className="grid overflow-hidden rounded-[1.75rem] border border-line bg-surface shadow-card sm:grid-cols-2">
        <section className="flex flex-col items-start gap-4 p-6 sm:p-9">
          <span className="grid size-12 place-items-center rounded-2xl bg-canvas text-ink"><FileIcon className="size-6" /></span>
          <h2 className="text-2xl font-semibold tracking-tight">Wprowadź dane ręcznie</h2>
          <p className="max-w-xs text-ink-soft">Odpowiedz na kilka pytań o swoją sytuację. Możesz zacząć bez logowania.</p>
          <Button asChild variant="secondary" className="mt-1 w-full sm:mt-auto sm:w-auto">
            <Link to={paths.manual} onClick={reset}>Wprowadź dane ręcznie <span aria-hidden="true">→</span></Link>
          </Button>
        </section>

        <section className="flex flex-col items-start gap-4 border-t border-line bg-brand-soft p-6 sm:border-l sm:border-t-0 sm:p-9">
          <span className="grid size-12 place-items-center rounded-2xl bg-surface text-brand-strong"><ShieldIcon className="size-6" /></span>
          <h2 className="text-2xl font-semibold tracking-tight">Pobierz przykładowe dane</h2>
          <p className="max-w-xs text-ink-soft">Wypróbuj połączenie z mObywatelem i ZUS na gotowych profilach i sprawach testowych.</p>
          <Button asChild className="mt-1 w-full sm:mt-auto sm:w-auto">
            <Link to={paths.login}>Połącz z mObywatelem <span aria-hidden="true">→</span></Link>
          </Button>
          <p className="text-sm text-ink-soft">Symulacja z danymi testowymi</p>
        </section>
      </div>

      <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2">
        {facts.sprawa['zdarzenie.rodzaj'] ? (
          <Button asChild variant="link"><Link to={paths.review}>Wróć do swojej sprawy</Link></Button>
        ) : null}
        <Button asChild variant="link"><Link to={paths.cases}>Przejrzyj sprawy ZUS lub wybierz PDF</Link></Button>
      </div>
    </div>
  )
}
