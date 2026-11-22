import { Link } from 'react-router'
import { Button, PageHeader } from '@/ui'
import { paths } from './paths'

export function NotFoundPage() {
  return (
    <section className="mx-auto max-w-2xl space-y-5 py-6">
      <PageHeader
        title="Nie ma takiej strony"
        description="Adres mógł się zmienić albo został wpisany z błędem. Nic się nie stało, Twoje dane są bezpieczne."
      />
      <div className="flex flex-wrap gap-3">
        <Button asChild>
          <Link to={paths.start}>Wróć na start</Link>
        </Button>
        <Button asChild variant="secondary">
          <Link to={paths.support}>Wsparcie</Link>
        </Button>
      </div>
    </section>
  )
}
