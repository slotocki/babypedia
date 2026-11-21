import { Link } from 'react-router'
import { paths } from '@/app/paths'
import { Placeholder } from '@/ui/Placeholder'

export function IntakePage() {
  return (
    <Placeholder title="Start — trzy pytania" screen={1} branch="fe/intake">
      <p className="mt-4">
        <Link to={paths.login} className="text-teal-800 underline">Dalej: logowanie (symulacja)</Link>
      </p>
    </Placeholder>
  )
}

export function LoginPage() {
  return <Placeholder title="Logowanie przez mObywatel (symulacja)" screen={2} branch="fe/intake" />
}
