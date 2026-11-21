import { createBrowserRouter } from 'react-router'
import { Layout } from './Layout'
import { paths } from './paths'
import { IntakePage, LoginPage } from '@/features/intake'
import { ReviewPage } from '@/features/review'
import { ResultPage } from '@/features/result'
import { ScenariosPage } from '@/features/scenarios'
import { SubmissionsPage } from '@/features/submissions'
import { ZusDataPage } from '@/features/zus-data'
import { CasesPage } from '@/features/cases'
import { SupportPage } from '@/features/support'
import { AdminPage } from '@/features/admin'
import { NotFoundPage } from './NotFoundPage'
import { UiGallery } from '@/ui/gallery/UiGallery'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { path: paths.start, element: <IntakePage /> },
      { path: paths.login, element: <LoginPage /> },
      { path: paths.review, element: <ReviewPage /> },
      { path: paths.result, element: <ResultPage /> },
      { path: paths.scenarios, element: <ScenariosPage /> },
      { path: paths.submissions, element: <SubmissionsPage /> },
      { path: paths.zusData, element: <ZusDataPage /> },
      { path: paths.cases, element: <CasesPage /> },
      { path: paths.support, element: <SupportPage /> },
      { path: paths.admin, element: <AdminPage /> },
      { path: '/ui', element: <UiGallery /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
