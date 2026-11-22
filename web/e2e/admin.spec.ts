import { expect, test } from '@playwright/test'

test('/admin z hasłem: lista parametrów z polskimi nazwami', async ({ page }) => {
  await page.goto('/admin')
  await expect(page.getByRole('heading', { name: 'Panel administratora', level: 1 })).toBeVisible()

  // Błędne hasło nie wpuszcza do panelu.
  const password = page.getByLabel('Hasło administratora')
  await password.fill('zle-haslo')
  await password.press('Enter')
  await expect(page.getByRole('alert')).toContainText('Nieprawidłowe hasło administratora.')

  // Hasło ustawione w playwright.config.ts (ADMIN_PASSWORD=e2e).
  await password.fill('e2e')
  await password.press('Enter')
  await expect(page.getByRole('button', { name: 'Wyloguj z panelu' })).toBeVisible()
  await expect(page.getByRole('tab', { name: 'Parametry' })).toHaveAttribute('aria-selected', 'true')

  // Parametr może mieć kilka kart (warianty wg wiersza tabeli) — wystarczy pierwsza.
  const parameter = page.getByRole('article', { name: 'Parametr macierzynski_tyg' }).first()
  await expect(parameter).toBeVisible()
  await expect(parameter.getByText('Długość urlopu macierzyńskiego (tygodnie, wg liczby dzieci)')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Urlopy i ich długość' })).toBeVisible()
  expect(await page.getByRole('article', { name: /^Parametr / }).count()).toBeGreaterThan(10)
})
