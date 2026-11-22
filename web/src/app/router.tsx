import { createBrowserRouter, Navigate } from 'react-router'
import { Layout } from './Layout'
import { paths } from './paths'
import { IntakePage, LoginPage } from '@/features/intake'
import { StartPage } from '@/features/intake/StartPage'
import { ReviewPage } from '@/features/review'
import { ResultPage } from '@/features/result'
import { ScenariosPage } from '@/features/scenarios'
import { SubmissionsPage } from '@/features/submissions'
import { ZusDataPage } from '@/features/zus-data'
import { CasesPage } from '@/features/cases'
import { SupportPage } from '@/features/support'
import { KnowledgePage } from '@/features/support/KnowledgePage'
import { AccountPage } from './AccountPage'
import { AdminPage } from '@/features/admin'
import { NotFoundPage } from './NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { path: paths.start, element: <StartPage /> },
      { path: paths.manual, element: <IntakePage /> },
      { path: paths.login, element: <LoginPage /> },
      { path: paths.review, element: <ReviewPage /> },
      { path: paths.result, element: <ResultPage /> },
      { path: paths.scenarios, element: <ScenariosPage /> },
      { path: paths.submissions, element: <SubmissionsPage /> },
      { path: paths.zusData, element: <ZusDataPage /> },
      { path: paths.cases, element: <CasesPage /> },
      { path: paths.support, element: <SupportPage /> },
      { path: paths.knowledge, element: <KnowledgePage /> },
      { path: paths.account, element: <AccountPage /> },
      { path: paths.admin, element: <AdminPage /> },
      { path: '/konto', element: <Navigate to={paths.account} replace /> },
      // Galeria komponentów tylko w trybie deweloperskim; w buildzie produkcyjnym trafia na stronę 404.
      ...(import.meta.env.DEV
        ? [{ path: '/ui', lazy: async () => ({ Component: (await import('@/ui/gallery/UiGallery')).UiGallery }) }]
        : []),
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
