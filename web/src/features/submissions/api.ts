// Typy szablonów i wywołania API są w @/app/api (types.ts, client.ts); tu tylko obsługa pliku w przeglądarce.
export type { TemplateField, SubmissionTemplate, TemplateFieldKind as FieldKind } from '@/app/api/types'

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
