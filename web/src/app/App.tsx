import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { CaseStateProvider } from './CaseStateProvider'
import { router } from './router'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <CaseStateProvider>
        <RouterProvider router={router} />
      </CaseStateProvider>
    </QueryClientProvider>
  )
}
