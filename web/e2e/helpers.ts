import { expect, type Locator, type Page } from '@playwright/test'

/** Data przesunięta o `days` dni od dziś, zapisana tak, jak wpisuje ją użytkownik (DD.MM.RRRR). */
export function polishDate(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`
}

/**
 * Wybór odpowiedzi w polu radio. Same przyciski są ukryte wizualnie (sr-only), więc klikamy etykietę —
 * tak jak użytkownik. `scope` zawęża wybór do grupy pytania, gdy kilka pytań ma „Tak/Nie”.
 */
export async function choose(scope: Page | Locator, option: string) {
  const escaped = option.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  await scope.locator('label').filter({ hasText: new RegExp(`^\\s*${escaped}`) }).first().click()
}

export const group = (page: Page, name: string) => page.getByRole('group', { name, exact: false })

/** Symulowane logowanie przez mObywatela i zgoda na pobranie danych; kończy na stronie „Sprawdź dane”. */
export async function loginAs(page: Page, person: string) {
  await page.goto('/logowanie')
  await page.getByText(person, { exact: true }).click()
  await page.getByRole('button', { name: 'Zaloguj przez mObywatela' }).click()
  await expect(page.getByRole('heading', { name: 'Zgoda na pobranie danych' })).toBeVisible()
  await page.getByRole('button', { name: 'Połącz i pobierz dane (symulacja)' }).click()
  await expect(page).toHaveURL(/\/sprawdz-dane/)
  await expect(page.getByRole('heading', { name: 'Sprawdź swoje dane', level: 1 })).toBeVisible()
}

/** „Dane się zgadzają” → (opcjonalne pytania) → „Pokaż wynik”; kończy na stronie wyniku. */
export async function confirmAndShowResult(page: Page, answer?: (page: Page) => Promise<void>) {
  await page.getByRole('button', { name: 'Dane się zgadzają — dalej' }).click()
  // Krok „Pytania”: nagłówek zależy od tego, czy silnik ma pytania („Kilka pytań…” albo „Sprawdź i pokaż wynik”).
  await expect(page).toHaveURL(/\/sprawdz-dane\?potwierdzone=1/)
  await expect(page.getByText('Krok 2 z 3:')).toBeVisible()
  // Przycisk jest nieaktywny, dopóki nie wrócą pytania z silnika.
  const show = page.getByRole('button', { name: 'Pokaż wynik' })
  await expect(show).toBeEnabled()
  if (answer) await answer(page)
  await show.click()
  await expect(page).toHaveURL(/\/wynik$/)
}

/** Anna (umowa o pracę, w ciąży) od logowania do wyniku. */
export async function annaToResult(page: Page) {
  await loginAs(page, 'Anna')
  await confirmAndShowResult(page, async (p) => {
    const contract = group(p, 'Czy Twoja umowa jest zawarta na czas określony?')
    // Pytanie pojawia się, gdy dane z ZUS nie mówią, jaka jest umowa — odpowiadamy, jeśli jest.
    if (await contract.isVisible()) {
      await choose(contract, 'Nie')
      await expect(p.getByRole('button', { name: 'Pokaż wynik' })).toBeEnabled()
    }
  })
  await expect(page.getByRole('heading', { name: 'Twój plan', level: 1 })).toBeVisible()
}

export const benefitCard = (page: Page, name: string) => page.getByRole('article', { name })

/** Czy strona przewija się w poziomie (szerokość dokumentu większa niż okno). */
export async function expectNoHorizontalScroll(page: Page) {
  await expect
    .poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth), {
      message: `przewijanie w poziomie na ${page.url()}`,
    })
    .toBeLessThanOrEqual(0)
}
