import { expect, test } from '@playwright/test'
import { annaToResult, benefitCard, confirmAndShowResult, loginAs } from './helpers'

test('Anna: logowanie symulowane → zgoda → dane → wynik z kartą zasiłku macierzyńskiego i kwotą', async ({ page }) => {
  await annaToResult(page)

  const card = benefitCard(page, 'Zasiłek macierzyński za okres urlopu macierzyńskiego')
  await expect(card).toBeVisible()
  await expect(card.getByText('Przysługuje', { exact: true })).toBeVisible()
  await expect(card.getByText('Szacowana kwota')).toBeVisible()
  // Kwota po polsku: cyfry z separatorem tysięcy, przecinek i „zł”.
  await expect(card.getByText(/^\d{1,3}(\s\d{3})*,\d{2}\szł$/)).toBeVisible()
  await expect(card.getByText('20 tygodni')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Co zrobić i do kiedy' })).toBeVisible()
})

test('Edyta: odmowa z powodem i następnym krokiem', async ({ page }) => {
  await loginAs(page, 'Edyta')
  await confirmAndShowResult(page)

  const card = benefitCard(page, 'Zasiłek macierzyński za okres urlopu macierzyńskiego')
  await expect(card.getByText('Nie przysługuje', { exact: true })).toBeVisible()
  // Powód: zaległość w składkach, i co zrobić, żeby to zmienić.
  await expect(card.getByText(/Masz zaległość w składkach na ubezpieczenia społeczne/)).toBeVisible()
  await expect(card.getByText('To da się naprawić')).toBeVisible()
  await expect(card.getByText(/Jeśli spłacisz całą zaległość .* zasiłek może zostać przyznany/)).toBeVisible()
  await expect(page.getByRole('complementary', { name: 'Dalsze kroki' }).getByRole('heading', { name: 'Co dalej?' })).toBeVisible()
})
