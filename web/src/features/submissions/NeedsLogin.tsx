import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Alert, Button } from '@/ui'

export function NeedsLogin({ expired = false }: { expired?: boolean }) {
  return (
    <Alert variant="info" title={expired ? 'Sesja symulacji wygasła' : 'Wnioski wymagają zalogowania (symulacja)'}>
      <div className="space-y-3">
        <p>
          {expired
            ? 'Zaloguj się ponownie, żeby dalej korzystać z wniosków. Zgłoszenia z poprzedniej sesji nie są już dostępne.'
            : 'Zgłoszenia zapisujemy przy zalogowanej osobie. Wybrano wpisywanie danych samodzielnie, więc nie wiemy, do kogo przypisać zgłoszenie. Zaloguj się przez symulowany mObywatel i wybierz fikcyjną osobę.'}
        </p>
        <Button asChild>
          <Link to={paths.login}>Przejdź do logowania (symulacja)</Link>
        </Button>
      </div>
    </Alert>
  )
}
