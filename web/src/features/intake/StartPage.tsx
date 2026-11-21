import { Link } from 'react-router'
import { useCaseState } from '@/app/case-context'
import { paths } from '@/app/paths'
import { FileIcon, ShieldIcon } from '@/ui'
import { useFocusHeading } from './useFocusHeading'

const card = 'group flex min-h-48 flex-col items-start gap-3 rounded-card border p-5 transition-colors focus-visible:outline-offset-4 sm:p-6'

export function StartPage() {
  const { facts, startManual } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()

  return (
    <div ref={ref} className="mx-auto max-w-5xl space-y-6 py-3 sm:space-y-8 sm:py-8">
      <header className="grid grid-cols-[minmax(0,1fr)_4.5rem] items-center gap-3 rounded-[1.75rem] bg-brand-soft px-5 py-6 min-[440px]:grid-cols-[minmax(0,1fr)_7.5rem] sm:grid-cols-[minmax(0,1fr)_11rem] sm:px-9 sm:py-8 lg:grid-cols-[minmax(0,1fr)_13rem]">
        <div className="space-y-3">
          <h1 tabIndex={-1} className="max-w-xl text-[clamp(1.8rem,4.6vw,3.35rem)] leading-[1.08] font-bold tracking-tight text-ink">Sprawdź, co Ci przysługuje</h1>
          <p className="max-w-md text-sm text-ink-soft sm:text-lg">Świadczenia dla rodziców, ważne terminy i potrzebne wnioski w jednym miejscu.</p>
        </div>
        <img alt="" width={480} height={480} src="/kompas-start.png" className="h-auto w-full" />
      </header>

      <div className="grid gap-3 md:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]">
        <div className="grid gap-3 sm:grid-cols-2">
          <Link to={paths.manual} onClick={startManual} className={card + ' border-line bg-surface hover:border-brand hover:bg-brand-soft/40'}>
            <FileIcon className="size-6 text-brand-strong" />
            <h2 className="text-lg font-bold leading-tight sm:text-xl">Wprowadź dane ręcznie</h2>
            <p className="text-sm text-ink-soft">Odpowiedz na kilka pytań o swoją sytuację. Nie wymaga logowania</p>
            <span className="mt-auto text-lg font-bold text-brand-strong" aria-hidden="true">→</span>
          </Link>
          <Link to={paths.login} className={card + ' border-brand/25 bg-surface hover:border-brand hover:bg-brand-soft/40'}>
            <ShieldIcon className="size-6 text-brand-strong" />
            <h2 className="text-lg font-bold leading-tight sm:text-xl">Połącz się przez mObywatela i ZUS</h2>
            <p className="text-sm text-ink-soft">Nie musisz wprowadzać większości danych</p>
            <span className="mt-auto text-lg font-bold text-brand-strong" aria-hidden="true">→</span>
          </Link>
        </div>

        <Link to={paths.cases} className={card + ' border-transparent bg-[#e6f4ef] text-[#134f46] hover:border-[#147664]'}>
          <h2 className="text-lg font-bold leading-tight sm:text-xl">Sprawdź na rzeczywistych przykładach</h2>
          <p className="text-sm text-[#285e55]">Wybierz zanonimizowaną sprawę ZUS i porównaj wynik kalkulatora z odnotowaną decyzją.</p>
          <span className="mt-auto text-sm font-bold">Zobacz sprawy ZUS <span aria-hidden="true">→</span></span>
        </Link>
      </div>

      {facts.sprawa['zdarzenie.rodzaj'] ? (
        <Link to={paths.result} className="inline-flex min-h-11 items-center font-semibold text-brand-strong underline underline-offset-4">Wróć do swojego wyniku</Link>
      ) : null}
    </div>
  )
}
