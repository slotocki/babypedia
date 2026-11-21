import type { FactKey, FactSet } from '@/app/api/types'
import { formatDatePl } from './dates'

export type FactKind = 'date' | 'integer' | 'number' | 'money' | 'boolean' | 'enum' | 'dateRanges' | 'sytuacjaMatki' | 'readonly'

export interface FactMeta {
  label: string
  kind: FactKind
  options?: Record<string, string>
  unit?: string
  min?: number
  max?: number
  format: (value: unknown) => string
}

const nf = new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 2 })
const money = new Intl.NumberFormat('pl-PL', { style: 'currency', currency: 'PLN' })

const fmtDate = (v: unknown) => formatDatePl(String(v))
const fmtBool = (v: unknown) => (v ? 'Tak' : 'Nie')
const fmtNumber = (unit?: string) => (v: unknown) => `${nf.format(Number(v))}${unit ? ` ${unit}` : ''}`
const fmtMoney = (v: unknown) => money.format(Number(v))
const fmtEnum = (options: Record<string, string>) => (v: unknown) => options[String(v)] ?? String(v)

const range = (od: string, do_?: string | null) => (do_ ? `${fmtDate(od)} – ${fmtDate(do_)}` : `od ${fmtDate(od)}, trwa`)

function fmtList<T>(items: unknown, line: (item: T) => string): string {
  if (!Array.isArray(items) || items.length === 0) return 'Brak'
  return (items as T[]).map(line).join('\n')
}

const date = (label: string): FactMeta => ({ label, kind: 'date', format: fmtDate })
const bool = (label: string): FactMeta => ({ label, kind: 'boolean', format: fmtBool })
const int = (label: string, extra: Partial<FactMeta> = {}): FactMeta => ({
  label,
  kind: 'integer',
  format: fmtNumber(extra.unit),
  ...extra,
})
const enumOf = (label: string, options: Record<string, string>): FactMeta => ({
  label,
  kind: 'enum',
  options,
  format: fmtEnum(options),
})
const readonly = (label: string, format: (v: unknown) => string): FactMeta => ({ label, kind: 'readonly', format })

export const eventKindLabels = {
  planuje: 'Planuję dziecko',
  ciaza: 'Jestem w ciąży',
  urodzenie: 'Dziecko się urodziło',
  przyjecie: 'Przyjmuję dziecko na wychowanie',
}

export const calculationScopeLabels = {
  ja: 'Dla mnie',
  ja_i_drugi_rodzic: 'Dla mnie i drugiego rodzica',
}

const titleKinds: Record<string, string> = {
  pracownik: 'Pracownik',
  zleceniobiorca: 'Zleceniobiorca',
  dzialalnosc: 'Działalność gospodarcza',
  osoba_wspolpracujaca: 'Osoba współpracująca',
  pobierajacy_zasilek: 'Pobiera zasiłek',
  inny: 'Inny tytuł',
}

const sicknessInsurance: Record<string, string> = {
  obowiazkowe: 'obowiązkowe',
  dobrowolne: 'dobrowolne',
  brak: 'brak',
}

const benefitKinds: Record<string, string> = {
  wynagrodzenie_chorobowe: 'Wynagrodzenie chorobowe',
  chorobowy: 'Zasiłek chorobowy',
  rehabilitacyjne: 'Świadczenie rehabilitacyjne',
  macierzynski: 'Zasiłek macierzyński',
  macierzynski_uzupelniajacy: 'Uzupełniający zasiłek macierzyński',
  rodzicielski: 'Świadczenie rodzicielskie',
  ojcowski: 'Zasiłek ojcowski',
  opiekunczy: 'Zasiłek opiekuńczy',
  inne: 'Inne świadczenie',
}

const endReasons: Record<string, string> = {
  wypowiedzenie_przez_pracownika: 'Wypowiedzenie przez pracownika',
  wypowiedzenie_przez_pracodawce: 'Wypowiedzenie przez pracodawcę',
  porozumienie_stron: 'Porozumienie stron',
  koniec_umowy_na_czas_okreslony: 'Koniec umowy na czas określony',
  likwidacja_pracodawcy: 'Likwidacja pracodawcy',
  upadlosc_pracodawcy: 'Upadłość pracodawcy',
  smierc_pracodawcy: 'Śmierć pracodawcy',
  naruszenie_prawa_orzeczone_przez_sad: 'Naruszenie prawa przez pracodawcę, orzeczone przez sąd',
  zamkniecie_dzialalnosci: 'Zamknięcie działalności',
  inna: 'Inna przyczyna',
}

const motherSituations: Record<string, string> = {
  niezdolnosc_do_samodzielnej_egzystencji: 'Niezdolność do samodzielnej egzystencji',
  pobyt_w_szpitalu: 'Pobyt w szpitalu',
  zgon: 'Zgon matki',
  porzucenie_dziecka: 'Porzucenie dziecka przez matkę',
}

const roles = { matka: 'Matka', ojciec: 'Ojciec' }

const leaveParts = (v: unknown) =>
  fmtList<{ tygodnie: number; od?: string }>(v, (p) => `${p.tygodnie} tyg.${p.od ? `, od ${fmtDate(p.od)}` : ''}`)

export const factMeta: Record<FactKey, FactMeta> = {
  'zdarzenie.rodzaj': enumOf('Co się dzieje', eventKindLabels),
  'zdarzenie.data': date('Data zdarzenia'),
  'zdarzenie.dlaKogo': enumOf('Dla kogo liczymy', calculationScopeLabels),
  'dziecko.liczba': int('Liczba dzieci', { min: 1, max: 10 }),
  'dziecko.dataUrodzenia': date('Data urodzenia dziecka'),
  'dziecko.tydzienCiazyPrzyPorodzie': int('Tydzień ciąży w dniu porodu', { min: 1, max: 45 }),
  'dziecko.masaUrodzeniowaG': int('Masa urodzeniowa dziecka (przy kilku dzieciach: najniższa)', { unit: 'g', min: 100, max: 8000 }),
  'dziecko.pobytyWSzpitalu': {
    label: 'Pobyty dziecka w szpitalu',
    kind: 'dateRanges',
    format: (v) => fmtList<{ od: string; do: string }>(v, (r) => range(r.od, r.do)),
  },
  'dziecko.zaZyciem': bool('Zaświadczenie „Za życiem”'),
  'dziecko.martweUrodzenie': bool('Dziecko urodziło się martwe'),
  'dziecko.liczbaZmarlych': int('Liczba dzieci, które zmarły', { min: 0, max: 10 }),
  'dziecko.dataZgonu': date('Data zgonu dziecka'),
  'dziecko.dataPochowku': date('Data pogrzebu'),
  'przyjecie.sciezka': enumOf('Sposób przyjęcia dziecka', {
    przysposobienie: 'Przysposobienie',
    rodzina_zastepcza_niezawodowa: 'Rodzina zastępcza niezawodowa',
    rodzina_zastepcza_zawodowa: 'Rodzina zastępcza zawodowa',
  }),
  'przyjecie.wniosekDoSadu': bool('Wniosek o przysposobienie złożony do sądu opiekuńczego'),
  'przyjecie.dataUprawomocnieniaPostanowienia': date('Data uprawomocnienia się postanowienia sądu'),
  'przyjecie.odroczenieObowiazkuSzkolnego': bool('Odroczony obowiązek szkolny dziecka'),
  'matka.sytuacjaSzczegolna': {
    label: 'Szczególna sytuacja matki dziecka',
    kind: 'sytuacjaMatki',
    options: motherSituations,
    format: (v) => {
      const s = v as { rodzaj: string; od: string; do?: string | null }
      return `${motherSituations[s.rodzaj] ?? s.rodzaj}, ${range(s.od, s.do)}`
    },
  },

  'osoba.rola': enumOf('Rola wobec dziecka', roles),
  'osoba.dataUrodzenia': date('Data urodzenia'),
  'osoba.dataUkonczeniaSzkoly': date('Data ukończenia szkoły'),
  'ubezpieczenie.tytuly': readonly('Tytuły ubezpieczenia', (v) =>
    fmtList<{ rodzajTytulu: string; platnikNazwa?: string; od: string; do?: string | null; chorobowe: string }>(v, (t) =>
      [
        titleKinds[t.rodzajTytulu] ?? t.rodzajTytulu,
        t.platnikNazwa,
        range(t.od, t.do),
        `ubezpieczenie chorobowe: ${sicknessInsurance[t.chorobowe] ?? t.chorobowe}`,
      ]
        .filter(Boolean)
        .join(', '),
    ),
  ),
  'ubezpieczenie.podstawySkladek': readonly('Podstawy składek z ostatnich miesięcy', (v) => {
    if (!Array.isArray(v) || v.length === 0) return 'Brak'
    const rows = [...(v as { miesiac: string; kwota: number }[])].sort((a, b) => a.miesiac.localeCompare(b.miesiac))
    const average = rows.reduce((sum, r) => sum + r.kwota, 0) / rows.length
    return `Miesięcy: ${rows.length} (od ${rows[0].miesiac} do ${rows[rows.length - 1].miesiac}), średnio ${money.format(average)}`
  }),
  'ubezpieczenie.przerwy': readonly('Przerwy w ubezpieczeniu', (v) =>
    fmtList<{ od: string; do: string; opis?: string }>(v, (r) => `${range(r.od, r.do)}${r.opis ? ` (${r.opis})` : ''}`),
  ),
  'ubezpieczenie.pobraneSwiadczenia': readonly('Pobrane świadczenia', (v) =>
    fmtList<{ rodzaj: string; od: string; do: string }>(v, (b) => `${benefitKinds[b.rodzaj] ?? b.rodzaj}: ${range(b.od, b.do)}`),
  ),
  'ubezpieczenie.latWczesniejszegoObowiazkowego': int('Lata wcześniejszego ubezpieczenia obowiązkowego', { unit: 'lat', min: 0, max: 60 }),
  'ubezpieczenie.podlegaChorobowemu': bool('Podlega ubezpieczeniu chorobowemu'),
  'ubezpieczenie.ustaniePrzyczyna': enumOf('Przyczyna ustania ubezpieczenia', endReasons),
  'umowa.naCzasOkreslony': bool('Umowa na czas określony'),
  'umowa.dataKonca': date('Data końca umowy'),
  'umowa.planowaneOdejscieData': date('Planowana data odejścia z pracy'),
  'platnik.liczbaUbezpieczonych': int('Liczba ubezpieczonych u płatnika składek (30 listopada poprzedniego roku)', { min: 0, max: 10000000 }),
  'skladki.zadluzenie': readonly('Zadłużenie z tytułu składek', (v) => {
    const d = v as { naDzien: string; kwota: number }
    return `${money.format(d.kwota)} na dzień ${fmtDate(d.naDzien)}`
  }),
  'skladki.dataSplaty': date('Data spłaty zadłużenia'),
  'ezla.zwolnienia': readonly('Zwolnienia lekarskie (e-ZLA)', (v) =>
    fmtList<{ od: string; do: string; kod?: string | null; szpital: boolean }>(v, (z) =>
      [range(z.od, z.do), z.kod ? `kod ${z.kod}` : null, z.szpital ? 'pobyt w szpitalu' : null].filter(Boolean).join(', '),
    ),
  ),
  'podstawa.ustalonaPrzezPlatnika': { label: 'Podstawa zasiłku ustalona przez płatnika', kind: 'money', format: fmtMoney },
  'plan.wariantWyplaty': enumOf('Wariant wypłaty', { standardowy: 'Standardowy', usredniony: 'Uśredniony' }),
  'plan.dataWnioskuUsrednionego': date('Data wniosku o wariant uśredniony'),
  'plan.macierzynskiPrzedPorodemTyg': int('Tygodnie zasiłku macierzyńskiego przed porodem', { unit: 'tyg.', min: 0, max: 52 }),
  'plan.rezygnacjaZMacierzynskiegoPoTyg': int('Rezygnacja z zasiłku macierzyńskiego po tygodniach', { unit: 'tyg.', min: 0, max: 52 }),
  'plan.rodzicielskiCzesci': readonly('Podział urlopu rodzicielskiego na części', leaveParts),
  'plan.pracaNaCzescEtatu': readonly('Praca na część etatu w trakcie urlopu rodzicielskiego', (v) =>
    fmtList<{ wymiar: number; tygodnie: number; od?: string }>(v, (p) => `wymiar ${p.wymiar}, ${p.tygodnie} tyg.${p.od ? `, od ${fmtDate(p.od)}` : ''}`),
  ),
  'plan.ojcowskiCzesci': readonly('Podział urlopu ojcowskiego na części', leaveParts),
}

export const factKeyOrder = Object.keys(factMeta) as FactKey[]

export function isFactKey(key: string): key is FactKey {
  return Object.prototype.hasOwnProperty.call(factMeta, key)
}

export function labelFor(key: string, facts?: FactSet): string {
  if (key === 'zdarzenie.data') {
    switch (facts?.sprawa['zdarzenie.rodzaj']?.value) {
      case 'planuje':
        return 'Planowana data urodzenia dziecka'
      case 'ciaza':
        return 'Przewidywana data porodu'
      case 'urodzenie':
        return 'Data urodzenia dziecka'
      case 'przyjecie':
        return 'Data przyjęcia dziecka'
    }
  }
  return isFactKey(key) ? factMeta[key].label : key
}

export function optionsOf(key: string, override?: string[] | null): { value: string; label: string }[] {
  const meta = isFactKey(key) ? factMeta[key] : undefined
  const values = override?.length ? override : Object.keys(meta?.options ?? {})
  return values.map((value) => ({ value, label: meta?.options?.[value] ?? value }))
}

export function formatFactValue(key: string, value: unknown): string {
  if (!isFactKey(key)) return String(value)
  try {
    return factMeta[key].format(value)
  } catch {
    return 'Nie udało się wyświetlić tej wartości'
  }
}
