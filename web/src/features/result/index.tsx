import { useQuery } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import { useCaseState } from '@/app/case-context'
import { Placeholder } from '@/ui/Placeholder'

export function ResultPage() {
  const { facts } = useCaseState()
  const result = useQuery({ queryKey: ['assessment', facts], queryFn: () => api.assess({ facts }) })

  return (
    <Placeholder title="Wynik" screen={4} branch="fe/result">
      {result.isPending && <p className="mt-4">Liczę…</p>}
      {result.isError && <p className="mt-4 text-red-800" role="alert">Nie udało się pobrać wyniku: {result.error.message}</p>}
      {result.data && (
        <ul className="mt-4 list-disc pl-5">
          {result.data.benefits.map((b) => (
            <li key={`${b.id}-${b.person}`}>
              {b.id} ({b.person}): <strong>{b.status}</strong>
            </li>
          ))}
        </ul>
      )}
    </Placeholder>
  )
}
