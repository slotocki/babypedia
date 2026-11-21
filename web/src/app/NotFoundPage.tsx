import { Link } from 'react-router'
import { paths } from './paths'

export function NotFoundPage() {
  return (
    <section>
      <h1 className="text-2xl font-semibold">Nie ma takiej strony</h1>
      <p className="mt-2">
        <Link to={paths.start} className="text-teal-800 underline">Wróć na start</Link>
      </p>
    </section>
  )
}
