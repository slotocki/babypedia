import { readFile } from 'node:fs/promises'
import { expect, test } from '@playwright/test'
import { annaToResult, choose, group } from './helpers'

test('wniosek: dane do ZAM → przygotowanie i pobranie PDF', async ({ page }) => {
  await annaToResult(page)
  await page.getByRole('link', { name: 'Przygotuj dane do wniosku o zasiłek macierzyński (ZAM)' }).click()
  await expect(page).toHaveURL(/\/wnioski\?.*szablon=dane_zam/)
  await expect(page.getByRole('heading', { name: 'Dane do wniosku o zasiłek macierzyński (ZAM)', level: 1 })).toBeVisible()

  // Imię i nazwisko oraz termin porodu są wstępnie wypełnione z danych Anny; uzupełniamy pola wymagane.
  await expect(page.getByRole('textbox', { name: 'Pierwsze imię i nazwisko ubezpieczonego' })).toHaveValue('Anna Przykładowa')
  await page.getByRole('combobox', { name: 'Rodzaj świadczenia, o które ubiega się wnioskujący' }).selectOption({ label: 'Zasiłek macierzyński za okres urlopu macierzyńskiego' })
  await page.getByRole('textbox', { name: 'Okres świadczenia — od' }).fill('15.03.2027')
  await page.getByRole('textbox', { name: 'Okres świadczenia — do' }).fill('01.08.2027')
  await choose(group(page, 'Czego dotyczy wniosek'), 'Urodzenia dziecka')
  await page.getByRole('button', { name: 'Przygotuj PDF' }).click()

  const downloadButton = page.getByRole('button', { name: 'Pobierz PDF' }).first()
  await expect(downloadButton).toBeVisible()

  const pdfResponse = page.waitForResponse((r) => r.request().method() === 'GET' && /\/api\/.*pdf/i.test(r.url()))
  const download = page.waitForEvent('download')
  await downloadButton.click()

  const response = await pdfResponse
  expect(response.status()).toBe(200)
  expect(response.headers()['content-type']).toContain('application/pdf')
  expect(Number(response.headers()['content-length'] ?? 0)).toBeGreaterThan(0)

  // Treść sprawdzamy na pobranym pliku (Playwright nie zawsze oddaje treść odpowiedzi przeczytanej przez aplikację jako Blob).
  const file = await download
  expect(file.suggestedFilename()).toMatch(/\.pdf$/)
  const bytes = await readFile(await file.path())
  expect(bytes.length).toBeGreaterThan(0)
  expect(bytes.subarray(0, 5).toString('latin1')).toBe('%PDF-')
})
