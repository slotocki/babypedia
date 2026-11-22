import { expect, test } from '@playwright/test'

test('czat: pytanie z FAQ zwraca odpowiedź ze zwiniętymi źródłami', async ({ page }) => {
  await page.goto('/wsparcie')
  await page.getByRole('button', { name: 'Zapytaj o przepisy' }).click()

  const chat = page.getByRole('dialog', { name: 'Zapytaj o przepisy' })
  await chat.getByRole('textbox', { name: 'Twoje pytanie' }).fill('Ile trwa urlop ojcowski?')
  await chat.getByRole('button', { name: 'Wyślij pytanie' }).click()

  const log = chat.getByRole('log', { name: 'Przebieg rozmowy' })
  await expect(log.getByText(/Urlop ojcowski trwa do 2 tygodni/)).toBeVisible()

  // Źródła są zwinięte do etykiet; treść przepisu pokazuje się dopiero po kliknięciu.
  await expect(log.getByText('Źródła:')).toBeVisible()
  const source = log.getByRole('button', { name: 'Kodeks pracy art. 182³' }).first()
  await expect(source).toHaveAttribute('aria-expanded', 'false')
  await expect(log.getByRole('region', { name: /^Źródło:/ })).toHaveCount(0)

  await source.click()
  await expect(source).toHaveAttribute('aria-expanded', 'true')
  await expect(log.getByRole('region', { name: 'Źródło: Kodeks pracy art. 182³' })).toContainText('urlopu ojcowskiego')
})
