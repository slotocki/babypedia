import { expect, test } from '@playwright/test'
import { annaToResult, expectNoHorizontalScroll } from './helpers'

const widths = [320, 390, 768]

test('brak przewijania w poziomie na startowej, wyniku, wnioskach i wsparciu (320, 390, 768 px)', async ({ page }) => {
  // Wynik wymaga sprawy — przechodzimy ścieżkę Anny raz, potem sprawdzamy wszystkie szerokości.
  await annaToResult(page)

  const pages = [
    { path: '/', heading: 'Sprawdź, co Ci przysługuje' },
    { path: '/wynik', heading: 'Twój plan' },
    { path: '/wnioski', heading: null },
    { path: '/wsparcie', heading: 'Wsparcie' },
  ]

  for (const width of widths) {
    await page.setViewportSize({ width, height: 800 })
    for (const { path, heading } of pages) {
      await test.step(`${path} przy ${width} px`, async () => {
        await page.goto(path)
        if (heading) await expect(page.getByRole('heading', { name: heading, level: 1 })).toBeVisible()
        else await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
        await expectNoHorizontalScroll(page)
      })
    }
  }
})
