import { expect, test, type Page } from '@playwright/test'
import { benefitCard, choose, group, polishDate } from './helpers'

async function startManual(page: Page) {
  await page.goto('/')
  await page.getByRole('link', { name: /Wprowadź dane ręcznie/ }).click()
  await expect(page).toHaveURL(/\/wprowadz-dane/)
  await expect(page.getByRole('heading', { name: 'Co się u Ciebie dzieje?', level: 1 })).toBeVisible()
}

const next = (page: Page) => page.getByRole('button', { name: 'Dalej', exact: true }).click()

async function whoAndMother(page: Page) {
  await expect(page.getByRole('heading', { name: 'Dla kogo sprawdzić świadczenia?', level: 1 })).toBeVisible()
  await choose(group(page, 'Dla kogo sprawdzić świadczenia'), 'Dla mnie')
  await choose(group(page, 'Kim jesteś dla dziecka?'), 'Matką')
  await page.getByRole('textbox', { name: 'Twoja data urodzenia' }).fill('12.05.1994')
  await next(page)
}

/** Umowa o pracę na czas nieokreślony, pełny etat, 6500 zł brutto, pracodawca powyżej 20 osób. */
async function employment(page: Page) {
  await expect(page.getByRole('heading', { name: 'Praca i ubezpieczenie', level: 1 })).toBeVisible()
  await choose(group(page, 'Jak pracujesz?'), 'Umowa o pracę')
  await page.getByRole('textbox', { name: 'Od kiedy trwa ta umowa lub działalność?' }).fill('01.01.2024')
  await choose(group(page, 'Czy umowa jest na czas określony?'), 'Nie')
  await page.getByRole('textbox', { name: 'Ile zarabiasz miesięcznie brutto?' }).fill('6500')
  await choose(group(page, 'Czy pracodawca zgłasza do ubezpieczenia więcej niż 20 osób?'), 'Tak')
}

async function saveAndShowResult(page: Page) {
  await page.getByRole('button', { name: 'Zapisz i przejdź dalej' }).click()
  await expect(page).toHaveURL(/\/sprawdz-dane/)
  const show = page.getByRole('button', { name: 'Pokaż wynik' })
  await expect(show).toBeEnabled()
  await show.click()
  await expect(page).toHaveURL(/\/wynik$/)
}

test('wpis ręczny: ciąża na umowie o pracę → wynik z zasiłkiem macierzyńskim', async ({ page }) => {
  await startManual(page)
  await choose(group(page, 'Twoja sytuacja'), 'Jestem w ciąży')
  await next(page)
  await page.getByRole('textbox', { name: 'Kiedy masz termin porodu?' }).fill(polishDate(120))
  await next(page)
  await whoAndMother(page)
  await employment(page)
  // Przy ciąży można zaplanować podział urlopu rodzicielskiego.
  await expect(page.getByRole('heading', { name: 'Podział urlopu rodzicielskiego' })).toBeVisible()
  await saveAndShowResult(page)

  await expect(page.getByRole('heading', { name: 'Twój plan', level: 1 })).toBeVisible()
  const card = benefitCard(page, 'Zasiłek macierzyński za okres urlopu macierzyńskiego')
  await expect(card.getByText('Przysługuje', { exact: true })).toBeVisible()
  await expect(card.getByText(/^\d{1,3}(\s\d{3})*,\d{2}\szł$/)).toBeVisible()
})

test('wpis ręczny: martwe urodzenie — wsparcie na górze, bez podziału urlopu rodzicielskiego', async ({ page }) => {
  await startManual(page)
  await choose(group(page, 'Twoja sytuacja'), 'Dziecko się urodziło')
  await next(page)
  await page.getByRole('textbox', { name: 'Kiedy urodziło się dziecko?' }).fill(polishDate(-10))
  await choose(group(page, 'Czy dziecko żyje?'), 'Dziecko urodziło się martwe')
  await next(page)
  await expect(page.getByRole('heading', { name: 'Po porodzie', level: 1 })).toBeVisible()
  await next(page)
  await whoAndMother(page)
  await employment(page)
  // Urlopu rodzicielskiego nie ma — nie proponujemy jego podziału ani rezygnacji z urlopu macierzyńskiego.
  await expect(page.getByRole('heading', { name: 'Podział urlopu rodzicielskiego' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Podziel urlop na części' })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Rezygnacja z części urlopu macierzyńskiego' })).toHaveCount(0)
  await saveAndShowResult(page)

  await expect(page.getByRole('heading', { name: 'Co możesz teraz załatwić', level: 1 })).toBeVisible()
  const support = page.getByText('Wsparcie jest dla Ciebie')
  const benefits = page.getByRole('heading', { name: 'Co Ci przysługuje', level: 2 })
  await expect(support).toBeVisible()
  await expect(benefits).toBeVisible()
  // Wsparcie jest nad listą świadczeń.
  const supportBox = await support.boundingBox()
  const benefitsBox = await benefits.boundingBox()
  expect(supportBox && benefitsBox && supportBox.y < benefitsBox.y).toBe(true)
  await expect(page.getByRole('heading', { name: 'Twój plan', level: 1 })).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Przygotuj wniosek o urlop' })).toHaveCount(0)
})
