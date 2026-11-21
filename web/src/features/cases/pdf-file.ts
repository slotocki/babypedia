import type { PDFDocumentProxy } from 'pdfjs-dist'

export const MAX_PDF_BYTES = 50 * 1024 * 1024

export function formatSize(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toLocaleString('pl-PL', { maximumFractionDigits: 1 })} MB`
}

export function validatePdfFile(file: File): string | null {
  const looksLikePdf = file.type === 'application/pdf' || /\.pdf$/i.test(file.name)
  if (!looksLikePdf) return 'To nie jest plik PDF. Wybierz plik z rozszerzeniem .pdf.'
  if (file.size === 0) return 'Plik jest pusty.'
  if (file.size > MAX_PDF_BYTES) {
    return `Plik jest za duży (${formatSize(file.size)}). Największy dozwolony rozmiar to ${formatSize(MAX_PDF_BYTES)}.`
  }
  if (!globalThis.crypto?.subtle) {
    return 'Ta przeglądarka nie pozwala policzyć sumy kontrolnej pliku. Otwórz stronę przez połączenie szyfrowane (https) albo użyj nowszej przeglądarki.'
  }
  return null
}

export async function readFileBytes(file: File, onProgress: (fraction: number) => void, isCancelled: () => boolean): Promise<Uint8Array | null> {
  const bytes = new Uint8Array(file.size)
  const reader = file.stream().getReader()
  let offset = 0
  for (;;) {
    const { done, value } = await reader.read()
    if (isCancelled()) {
      void reader.cancel()
      return null
    }
    if (done) break
    if (offset + value.length > bytes.length) throw new Error('Plik zmienił się w trakcie wczytywania.')
    bytes.set(value, offset)
    offset += value.length
    onProgress(offset / bytes.length)
  }
  return offset === bytes.length ? bytes : bytes.subarray(0, offset)
}

export async function sha256Hex(bytes: Uint8Array): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', bytes as BufferSource)
  return Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, '0')).join('')
}

export async function openPdf(data: Uint8Array): Promise<PDFDocumentProxy> {
  const [pdfjs, worker] = await Promise.all([import('pdfjs-dist'), import('pdfjs-dist/build/pdf.worker.min.mjs?url')])
  pdfjs.GlobalWorkerOptions.workerSrc = worker.default
  return pdfjs.getDocument({ data, isEvalSupported: false, enableXfa: false }).promise
}

export function describePdfError(error: unknown): string {
  const name = typeof error === 'object' && error !== null && 'name' in error ? String((error as { name: unknown }).name) : ''
  if (name === 'PasswordException') return 'Plik jest zabezpieczony hasłem, więc nie możemy go pokazać. Rozpoznanie sprawy nadal działa.'
  if (name === 'InvalidPDFException' || name === 'FormatError') return 'Nie udało się otworzyć pliku — wygląda na uszkodzony. Rozpoznanie sprawy nadal działa.'
  return 'Nie udało się wyświetlić podglądu tego pliku. Rozpoznanie sprawy nadal działa.'
}
