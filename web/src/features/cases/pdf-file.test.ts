import { describe, expect, it } from 'vitest'
import { describePdfError, formatSize, MAX_PDF_BYTES, readFileBytes, sha256Hex, validatePdfFile } from './pdf-file'

const file = (bytes: BlobPart[], name: string, type = 'application/pdf') => new File(bytes, name, { type })

describe('pdf-file', () => {
  it('sha256Hex daje znaną sumę kontrolną', async () => {
    expect(await sha256Hex(new TextEncoder().encode('abc'))).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')
    expect(await sha256Hex(new Uint8Array())).toBe('e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855')
  })

  it('readFileBytes wczytuje cały plik i zgłasza postęp do 100%', async () => {
    const content = new Uint8Array(200_000).map((_, i) => i % 251)
    const progress: number[] = []
    const bytes = await readFileBytes(file([content], 'a.pdf'), (f) => progress.push(f), () => false)
    expect(bytes && Buffer.from(bytes).equals(Buffer.from(content))).toBe(true)
    expect(progress.at(-1)).toBe(1)
  })

  it('readFileBytes przerwane zwraca null', async () => {
    expect(await readFileBytes(file(['%PDF-1.7'], 'a.pdf'), () => undefined, () => true)).toBeNull()
  })

  it('validatePdfFile: rozszerzenie, pusty plik, limit rozmiaru', () => {
    expect(validatePdfFile(file(['%PDF'], 'sprawa.PDF', ''))).toBeNull()
    expect(validatePdfFile(file(['x'], 'zdjecie.png', 'image/png'))).toMatch(/To nie jest plik PDF/)
    expect(validatePdfFile(file([], 'pusty.pdf'))).toBe('Plik jest pusty.')
    const big = { name: 'duzy.pdf', type: 'application/pdf', size: MAX_PDF_BYTES + 1 } as File
    expect(validatePdfFile(big)?.replace(/\s/g, ' ')).toBe('Plik jest za duży (50 MB). Największy dozwolony rozmiar to 50 MB.')
  })

  it('formatSize i komunikaty błędów pdf.js', () => {
    expect(formatSize(10)).toBe('1 KB')
    expect(formatSize(1536 * 1024).replace(/\s/g, ' ')).toBe('1,5 MB')
    expect(describePdfError({ name: 'PasswordException' })).toMatch(/hasłem/)
    expect(describePdfError(new Error('x'))).toMatch(/podglądu/)
  })
})
