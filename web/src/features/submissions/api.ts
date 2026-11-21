import { ApiError } from '@/app/api/client'
import type { ProblemDetails, SubmissionType } from '@/app/api/types'

const baseUrl = import.meta.env.VITE_API_URL ?? ''

export type FieldKind = 'text' | 'longtext' | 'date' | 'integer' | 'choice'

export interface TemplateField {
  key: string
  label: string
  kind: FieldKind
  required: boolean
  maxLength: number
  basis: string
  when: { key: string; value: string } | null
  options: { value: string; label: string }[] | null
}

export interface SubmissionTemplate {
  id: string
  type: SubmissionType
  template: string | null
  recipient: string
  title: string
  fields: TemplateField[]
}

async function failure(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = { status: response.status, title: response.statusText }
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // odpowiedź bez treści JSON
  }
  return new ApiError(response.status, problem)
}

export async function fetchTemplates(): Promise<SubmissionTemplate[]> {
  const response = await fetch(`${baseUrl}/api/submissions/templates`, { headers: { Accept: 'application/json' } })
  if (!response.ok) throw await failure(response)
  return (await response.json()) as SubmissionTemplate[]
}

export async function fetchPdf(path: string, session: string): Promise<Blob> {
  const response = await fetch(`${baseUrl}${path}`, { headers: { Accept: 'application/pdf', 'X-Sim-Session': session } })
  if (!response.ok) throw await failure(response)
  const blob = await response.blob()
  if (!blob.type.includes('pdf')) throw new Error('Serwer nie zwrócił pliku PDF.')
  return blob
}

export const submissionPdfPath = (id: string) => `/api/submissions/${id}/pdf`

export const certificatePath = (date: string) => `/api/sim/me/debt-certificate?date=${encodeURIComponent(date)}`

export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.append(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export function showBlob(blob: Blob, fileName: string, target: Window | null) {
  if (!target) {
    downloadBlob(blob, fileName)
    return
  }
  const url = URL.createObjectURL(blob)
  target.location.href = url
  setTimeout(() => URL.revokeObjectURL(url), 10 * 60_000)
}
