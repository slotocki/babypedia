import type {
  AssessmentRequest,
  AssessmentResult,
  AssistantAnswer,
  CaseSummary,
  CatalogEntry,
  CompareRequest,
  CompareResult,
  FactSet,
  Health,
  IntegrationField,
  LoginResponse,
  Organization,
  Parameter,
  ParameterChange,
  ParameterHistoryEntry,
  PersonaListItem,
  ProblemDetails,
  SubmissionReceipt,
  SubmissionRequest,
  SupportSituation,
  ZusAccount,
} from './types'

const baseUrl = import.meta.env.VITE_API_URL ?? ''

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Błąd ${status}`)
    this.status = status
    this.problem = problem
  }
}

interface RequestOptions {
  session?: string | null
  adminPassword?: string | null
}

async function request<T>(method: string, path: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (options.session) headers['X-Sim-Session'] = options.session
  if (options.adminPassword) headers['X-Admin-Password'] = options.adminPassword

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    let problem: ProblemDetails = { status: response.status, title: response.statusText }
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      // odpowiedź bez treści JSON
    }
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const api = {
  health: () => request<Health>('GET', '/health'),

  sim: {
    personas: () => request<PersonaListItem[]>('GET', '/api/sim/personas'),
    login: (personaId: string) => request<LoginResponse>('POST', '/api/sim/login', { personaId }),
    myFacts: (session: string) => request<FactSet>('GET', '/api/sim/me/facts', undefined, { session }),
    myZus: (session: string) => request<ZusAccount>('GET', '/api/sim/me/zus', undefined, { session }),
    debtCertificateUrl: (date?: string) => `${baseUrl}/api/sim/me/debt-certificate${date ? `?date=${date}` : ''}`,
  },

  assess: (body: AssessmentRequest) => request<AssessmentResult>('POST', '/api/assessment', body),
  compare: (body: CompareRequest) => request<CompareResult>('POST', '/api/assessment/compare', body),

  submissions: {
    create: (session: string, body: SubmissionRequest) =>
      request<SubmissionReceipt>('POST', '/api/submissions', body, { session }),
    list: (session: string) => request<SubmissionReceipt[]>('GET', '/api/submissions', undefined, { session }),
  },

  cases: {
    list: () => request<CaseSummary[]>('GET', '/api/cases'),
    byHash: (sha256: string, fileName?: string, pages?: number) => {
      const query = new URLSearchParams()
      if (fileName) query.set('fileName', fileName)
      if (pages) query.set('pages', String(pages))
      const qs = query.size ? `?${query}` : ''
      return request<CaseSummary>('GET', `/api/cases/by-hash/${sha256}${qs}`)
    },
  },

  organizations: (situation?: SupportSituation) =>
    request<Organization[]>('GET', `/api/organizations${situation ? `?situation=${encodeURIComponent(situation)}` : ''}`),

  admin: {
    parameters: (adminPassword: string) => request<Parameter[]>('GET', '/api/admin/parameters', undefined, { adminPassword }),
    updateParameter: (adminPassword: string, change: ParameterChange) =>
      request<Parameter>('PUT', '/api/admin/parameters', change, { adminPassword }),
    parameterHistory: (adminPassword: string) =>
      request<ParameterHistoryEntry[]>('GET', '/api/admin/parameters/history', undefined, { adminPassword }),
    catalog: (adminPassword: string) => request<CatalogEntry[]>('GET', '/api/admin/catalog', undefined, { adminPassword }),
    updateCatalog: (adminPassword: string, entries: CatalogEntry[]) =>
      request<void>('PUT', '/api/admin/catalog', entries, { adminPassword }),
    integrations: (adminPassword: string) =>
      request<IntegrationField[]>('GET', '/api/admin/integrations', undefined, { adminPassword }),
    updateIntegrations: (adminPassword: string, fields: IntegrationField[]) =>
      request<void>('PUT', '/api/admin/integrations', fields, { adminPassword }),
    organizations: (adminPassword: string) =>
      request<Organization[]>('GET', '/api/admin/organizations', undefined, { adminPassword }),
    updateOrganizations: (adminPassword: string, organizations: Organization[]) =>
      request<void>('PUT', '/api/admin/organizations', organizations, { adminPassword }),
  },

  assistant: {
    ask: (question: string) => request<AssistantAnswer>('POST', '/api/assistant/ask', { question }),
  },
}
