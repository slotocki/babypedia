import type { PDFDocumentProxy } from 'pdfjs-dist'
import { useCallback, useEffect, useRef, useState } from 'react'
import { api, ApiError } from '@/app/api/client'
import type { CaseSummary } from '@/app/api/types'
import { describePdfError, openPdf, readFileBytes, sha256Hex, validatePdfFile } from './pdf-file'

export interface PdfFileInfo {
  name: string
  size: number
  sha256: string
  pages: number | null
}

export type PdfStage =
  | { kind: 'idle' }
  | { kind: 'reading'; fraction: number; name: string }
  | { kind: 'hashing'; name: string }
  | { kind: 'opening'; name: string }
  | { kind: 'rejected'; message: string }
  | { kind: 'ready' }

export type Lookup =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'found'; summary: CaseSummary }
  | { kind: 'unknown' }
  | { kind: 'error'; error: unknown }

export function useCasePdf() {
  const [stage, setStage] = useState<PdfStage>({ kind: 'idle' })
  const [doc, setDoc] = useState<PDFDocumentProxy | null>(null)
  const [docError, setDocError] = useState<string | null>(null)
  const [file, setFile] = useState<PdfFileInfo | null>(null)
  const [lookup, setLookup] = useState<Lookup>({ kind: 'idle' })
  const run = useRef(0)
  const docRef = useRef<PDFDocumentProxy | null>(null)

  const releaseDoc = useCallback(() => {
    const current = docRef.current
    docRef.current = null
    if (current) void current.destroy()
    setDoc(null)
  }, [])

  useEffect(
    () => () => {
      run.current += 1
      const current = docRef.current
      docRef.current = null
      if (current) void current.destroy()
    },
    [],
  )

  const recognize = useCallback(async (info: PdfFileInfo, token: number) => {
    setLookup({ kind: 'loading' })
    try {
      const summary = await api.cases.byHash(info.sha256, info.name, info.pages ?? undefined)
      if (token === run.current) setLookup({ kind: 'found', summary })
    } catch (error) {
      if (token !== run.current) return
      setLookup(error instanceof ApiError && error.status === 404 ? { kind: 'unknown' } : { kind: 'error', error })
    }
  }, [])

  const pick = useCallback(
    async (picked: File) => {
      const token = ++run.current
      const cancelled = () => token !== run.current
      releaseDoc()
      setFile(null)
      setDocError(null)
      setLookup({ kind: 'idle' })

      const rejection = validatePdfFile(picked)
      if (rejection) {
        setStage({ kind: 'rejected', message: rejection })
        return
      }

      try {
        setStage({ kind: 'reading', fraction: 0, name: picked.name })
        const bytes = await readFileBytes(picked, (fraction) => !cancelled() && setStage({ kind: 'reading', fraction, name: picked.name }), cancelled)
        if (!bytes || cancelled()) return
        setStage({ kind: 'hashing', name: picked.name })
        const sha256 = await sha256Hex(bytes)
        if (cancelled()) return

        setStage({ kind: 'opening', name: picked.name })
        let pages: number | null = null
        try {
          const opened = await openPdf(bytes)
          if (cancelled()) {
            void opened.destroy()
            return
          }
          docRef.current = opened
          setDoc(opened)
          pages = opened.numPages
        } catch (error) {
          if (cancelled()) return
          setDocError(describePdfError(error))
        }

        const info: PdfFileInfo = { name: picked.name, size: picked.size, sha256, pages }
        setFile(info)
        setStage({ kind: 'ready' })
        await recognize(info, token)
      } catch {
        if (!cancelled()) setStage({ kind: 'rejected', message: 'Nie udało się odczytać pliku. Spróbuj wybrać go ponownie.' })
      }
    },
    [recognize, releaseDoc],
  )

  const retryLookup = useCallback(() => {
    if (file) void recognize(file, run.current)
  }, [file, recognize])

  const clear = useCallback(() => {
    run.current += 1
    releaseDoc()
    setFile(null)
    setDocError(null)
    setLookup({ kind: 'idle' })
    setStage({ kind: 'idle' })
  }, [releaseDoc])

  return { stage, doc, docError, file, lookup, pick, retryLookup, clear }
}
