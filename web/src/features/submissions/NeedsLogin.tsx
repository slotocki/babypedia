import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Alert, Button } from '@/ui'

export function NeedsLogin({ expired = false, templateId }: { expired?: boolean; templateId?: string }) {
  const params = new URLSearchParams({ powrot: 'wnioski' })
  if (templateId) params.set('szablon', templateId)
  return (
    <Alert variant="info" title={expired ? 'Sesja symulacji wygasła' : 'Wybierz profil testowy do wniosku'}>
      <div className="space-y-3">
        <p>
          {expired
            ? 'Zaloguj się ponownie, żeby dalej korzystać z wniosków. Zgłoszenia z poprzedniej sesji nie są już dostępne.'
            : 'Przypiszemy dokument do wybranego profilu testowego. Zachowamy dane wprowadzone podczas sprawdzania świadczeń.'}
        </p>
        <Button asChild>
          <Link to={paths.login + '?' + params.toString()}>Wybierz profil testowy</Link>
        </Button>
      </div>
    </Alert>
  )
}
