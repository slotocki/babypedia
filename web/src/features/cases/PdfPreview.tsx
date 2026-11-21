import type { PDFDocumentProxy, RenderTask } from 'pdfjs-dist'
import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { Button, Spinner, TextInput } from '@/ui'

const zoomSteps = [1, 1.25, 1.5, 2, 3]

export function PdfPreview({ doc, fileName }: { doc: PDFDocumentProxy; fileName: string }) {
  const total = doc.numPages
  const [page, setPage] = useState(1)
  const [pageText, setPageText] = useState('1')
  const [zoomIndex, setZoomIndex] = useState(0)
  const [width, setWidth] = useState(0)
  const [rendering, setRendering] = useState(true)
  const [renderError, setRenderError] = useState(false)
  const viewportRef = useRef<HTMLDivElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const pageInputId = useId()
  const zoom = zoomSteps[zoomIndex]

  useEffect(() => {
    const node = viewportRef.current
    if (!node) return
    const update = () => setWidth(Math.floor(node.clientWidth))
    update()
    const observer = new ResizeObserver(update)
    observer.observe(node)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    if (width <= 0) return
    let cancelled = false
    let task: RenderTask | null = null
    setRendering(true)
    setRenderError(false)

    void (async () => {
      try {
        const pdfPage = await doc.getPage(page)
        if (cancelled) return
        const base = pdfPage.getViewport({ scale: 1 })
        const cssScale = (width / base.width) * zoom
        const pixelRatio = Math.min(window.devicePixelRatio || 1, 2)
        const viewport = pdfPage.getViewport({ scale: cssScale * pixelRatio })
        const canvas = canvasRef.current
        const context = canvas?.getContext('2d')
        if (!canvas || !context) return
        canvas.width = Math.floor(viewport.width)
        canvas.height = Math.floor(viewport.height)
        canvas.style.width = `${Math.floor(viewport.width / pixelRatio)}px`
        canvas.style.height = `${Math.floor(viewport.height / pixelRatio)}px`
        task = pdfPage.render({ canvasContext: context, viewport })
        await task.promise
        if (!cancelled) setRendering(false)
      } catch (error) {
        const name = typeof error === 'object' && error !== null && 'name' in error ? (error as { name: string }).name : ''
        if (!cancelled && name !== 'RenderingCancelledException') {
          setRenderError(true)
          setRendering(false)
        }
      }
    })()

    return () => {
      cancelled = true
      task?.cancel()
    }
  }, [doc, page, zoom, width])

  const goTo = (next: number) => {
    const clamped = Math.min(Math.max(next, 1), total)
    setPage(clamped)
    setPageText(String(clamped))
  }

  const onPageText = (value: string) => {
    setPageText(value)
    const parsed = Number.parseInt(value, 10)
    if (Number.isInteger(parsed) && parsed >= 1 && parsed <= total) setPage(parsed)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const actions: Record<string, () => void> = {
      PageUp: () => goTo(page - 1),
      PageDown: () => goTo(page + 1),
      Home: () => goTo(1),
      End: () => goTo(total),
      '+': () => setZoomIndex((i) => Math.min(i + 1, zoomSteps.length - 1)),
      '=': () => setZoomIndex((i) => Math.min(i + 1, zoomSteps.length - 1)),
      '-': () => setZoomIndex((i) => Math.max(i - 1, 0)),
      '0': () => setZoomIndex(0),
    }
    const action = actions[event.key]
    if (!action || event.ctrlKey || event.metaKey || event.altKey) return
    event.preventDefault()
    action()
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-end gap-2" role="group" aria-label="Sterowanie podglądem dokumentu">
        <Button variant="secondary" onClick={() => goTo(page - 1)} disabled={page <= 1} aria-label="Poprzednia strona">
          Poprzednia
        </Button>
        <div className="flex items-center gap-2">
          <label htmlFor={pageInputId} className="text-base font-semibold text-ink">
            Strona
          </label>
          <TextInput
            id={pageInputId}
            type="number"
            inputMode="numeric"
            min={1}
            max={total}
            value={pageText}
            onChange={(event) => onPageText(event.target.value)}
            onBlur={() => setPageText(String(page))}
            className="w-20"
          />
          <span className="text-base text-ink-soft">z {total}</span>
        </div>
        <Button variant="secondary" onClick={() => goTo(page + 1)} disabled={page >= total} aria-label="Następna strona">
          Następna
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2" role="group" aria-label="Powiększenie">
        <Button variant="secondary" onClick={() => setZoomIndex((i) => Math.max(i - 1, 0))} disabled={zoomIndex === 0} aria-label="Pomniejsz">
          −
        </Button>
        <span className="min-w-24 text-center text-base text-ink" role="status">
          {zoomIndex === 0 ? 'Do szerokości' : `${Math.round(zoom * 100)}%`}
        </span>
        <Button
          variant="secondary"
          onClick={() => setZoomIndex((i) => Math.min(i + 1, zoomSteps.length - 1))}
          disabled={zoomIndex === zoomSteps.length - 1}
          aria-label="Powiększ"
        >
          +
        </Button>
      </div>

      <div
        ref={viewportRef}
        tabIndex={0}
        onKeyDown={onKeyDown}
        aria-label={`Podgląd dokumentu ${fileName}. PageUp i PageDown zmieniają stronę, plus i minus powiększają.`}
        role="group"
        className="relative max-h-[70vh] overflow-auto rounded-card border border-line bg-neutral-soft"
      >
        <canvas
          ref={canvasRef}
          role="img"
          aria-label={`Strona ${page} z ${total} dokumentu ${fileName}. To obraz skanu — treść dokumentu nie jest odczytywana ani opisywana.`}
          className="block bg-white"
        />
        {rendering ? (
          <p role="status" className="absolute inset-x-0 top-2 mx-auto flex w-fit items-center gap-2 rounded-pill bg-surface px-3 py-1 text-sm text-ink-soft shadow-card">
            <Spinner label={null} /> Rysuję stronę {page}…
          </p>
        ) : null}
      </div>
      {renderError ? <p role="alert" className="text-base text-bad">Nie udało się narysować tej strony.</p> : null}
      <p className="text-sm text-ink-soft">Podgląd służy tylko do obejrzenia dokumentu. Nie odczytujemy jego treści.</p>
    </div>
  )
}
