import { Link } from 'react-router'
import { useCaseState } from './case-context'
import { paths } from './paths'
import { useFocusHeading } from '@/features/intake/useFocusHeading'
import { ConnectionStatus } from '@/features/zus-data/ConnectionStatus'
import { Button, Card, PageHeader, UserIcon } from '@/ui'

export function AccountPage() {
  const { sessionId, profileName, facts, connection, caseId } = useCaseState()
  const ref = useFocusHeading<HTMLDivElement>()
  const hasResult = Boolean(facts.sprawa['zdarzenie.rodzaj'])

  return (
    <div ref={ref} className="mx-auto max-w-2xl space-y-5 py-3">
      <PageHeader
        title={caseId ? 'Sprawa testowa' : 'Moje konto'}
        description={caseId ? 'Wczytano sprawę testową opartą na dokumentach ZUS. To nie jest Twoje konto, a dane są zanonimizowane.' : undefined}
      />
      {sessionId ? (
        <Card className="space-y-5">
          <div className="flex items-center gap-3">
            <span className="grid size-11 place-items-center rounded-full bg-brand-soft text-brand-strong"><UserIcon className="size-6" /></span>
            <div>
              <h2 className="text-lg font-bold">{profileName || 'Profil testowy'}</h2>
              <p className="text-sm text-ink-soft">Zalogowano przez mObywatela (symulacja) · dane fikcyjne</p>
            </div>
          </div>
          {connection ? <ConnectionStatus connection={connection} className="bg-canvas" /> : null}
          <div className="flex flex-wrap gap-2">
            <Button asChild><Link to={hasResult ? paths.result : paths.review}>{hasResult ? 'Przejdź do planu' : 'Uzupełnij dane'}</Link></Button>
            <Button asChild variant="secondary"><Link to={paths.zusData}>Moje dane w ZUS</Link></Button>
            <Button asChild variant="secondary"><Link to={paths.submissions + '?widok=moje'}>Moje wnioski</Link></Button>
          </div>
          <Button asChild variant="link"><Link to={paths.login}>Zmień osobę testową</Link></Button>
        </Card>
      ) : (
        <Card className="space-y-3">
          <p>Zaloguj się przez mObywatela (symulacja), aby zobaczyć dane konta i przygotować wniosek.</p>
          <Button asChild><Link to={paths.login}>Zaloguj przez mObywatela</Link></Button>
        </Card>
      )}
    </div>
  )
}
