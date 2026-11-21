import type { FactSet, LoginResponse, PersonaListItem } from '@/app/api/types'

export const MOCK_PERSONA_PREFIX = 'mock-'
export const MOCK_SESSION_PREFIX = 'mock-sesja-'

interface MockPersona {
  item: PersonaListItem
  login: Omit<LoginResponse, 'sessionId'>
  facts: FactSet
}

const months = (from: string, count: number, amount: number) => {
  const [y, m] = from.split('-').map(Number)
  return Array.from({ length: count }, (_, i) => {
    const index = y * 12 + (m - 1) + i
    return { miesiac: `${Math.floor(index / 12)}-${String((index % 12) + 1).padStart(2, '0')}`, kwota: amount }
  })
}

const personas: MockPersona[] = [
  {
    item: {
      id: 'mock-ewa',
      imie: 'Ewa',
      opis: 'Pracownica na umowie o pracę, w ciąży, poród przewidywany w marcu 2027.',
      caseId: null,
    },
    login: {
      identity: { imie: 'Ewa', nazwisko: 'Wymyślona', nazwiskoRodowe: 'Przykładowa', pesel: '92031400000', dataUrodzenia: '1992-03-14' },
      simulated: true,
    },
    facts: {
      sprawa: {
        'zdarzenie.rodzaj': { value: 'ciaza', source: 'user' },
        'zdarzenie.data': { value: '2027-03-15', source: 'user' },
        'zdarzenie.dlaKogo': { value: 'ja', source: 'user' },
        'dziecko.liczba': { value: 1, source: 'assumption' },
      },
      wnioskodawca: {
        'osoba.rola': { value: 'matka', source: 'assumption' },
        'osoba.dataUrodzenia': { value: '1992-03-14', source: 'mobywatel' },
        'ubezpieczenie.tytuly': {
          source: 'zus',
          value: [
            {
              kodTytulu: '0110',
              opis: 'pracownik',
              rodzajTytulu: 'pracownik',
              platnikId: 'platnik-demo-1',
              platnikNazwa: 'Przykładowa Spółka Demo',
              od: '2020-09-01',
              do: null,
              chorobowe: 'obowiazkowe',
              wymiarEtatu: 1,
            },
          ],
        },
        'ubezpieczenie.podstawySkladek': { source: 'zus', value: months('2025-11', 12, 7200) },
        'ubezpieczenie.przerwy': { source: 'zus', value: [] },
        'ubezpieczenie.pobraneSwiadczenia': { source: 'zus', value: [] },
        'platnik.liczbaUbezpieczonych': { source: 'zus', value: 85 },
        'ezla.zwolnienia': {
          source: 'ezla',
          value: [{ od: '2026-10-05', do: '2026-10-16', kod: 'B', szpital: false, dataWystawienia: '2026-10-05' }],
        },
      },
    },
  },
  {
    item: {
      id: 'mock-marta',
      imie: 'Marta',
      opis: 'Prowadzi własną działalność, jest w ciąży, ubezpieczenie chorobowe dobrowolne.',
      caseId: null,
    },
    login: {
      identity: { imie: 'Marta', nazwisko: 'Demonstracyjna', nazwiskoRodowe: null, pesel: '89112200000', dataUrodzenia: '1989-11-22' },
      simulated: true,
    },
    facts: {
      sprawa: {
        'zdarzenie.rodzaj': { value: 'ciaza', source: 'user' },
        'zdarzenie.data': { value: '2027-05-02', source: 'user' },
        'zdarzenie.dlaKogo': { value: 'ja', source: 'user' },
        'dziecko.liczba': { value: 1, source: 'assumption' },
      },
      wnioskodawca: {
        'osoba.rola': { value: 'matka', source: 'assumption' },
        'osoba.dataUrodzenia': { value: '1989-11-22', source: 'mobywatel' },
        'ubezpieczenie.tytuly': {
          source: 'zus',
          value: [{ kodTytulu: '0510', opis: 'prowadzący działalność', rodzajTytulu: 'dzialalnosc', od: '2018-04-01', do: null, chorobowe: 'dobrowolne' }],
        },
        'ubezpieczenie.podstawySkladek': { source: 'zus', value: months('2025-11', 12, 5800) },
        'ubezpieczenie.przerwy': { source: 'zus', value: [] },
        'ubezpieczenie.pobraneSwiadczenia': { source: 'zus', value: [] },
        'ezla.zwolnienia': { source: 'ezla', value: [] },
      },
    },
  },
  {
    item: {
      id: 'mock-tomasz',
      imie: 'Tomasz',
      opis: 'Ojciec dziecka urodzonego w październiku 2026, pracownik.',
      caseId: null,
    },
    login: {
      identity: { imie: 'Tomasz', nazwisko: 'Wzorcowy', nazwiskoRodowe: null, pesel: '90070800000', dataUrodzenia: '1990-07-08' },
      simulated: true,
    },
    facts: {
      sprawa: {
        'zdarzenie.rodzaj': { value: 'urodzenie', source: 'user' },
        'zdarzenie.data': { value: '2026-10-20', source: 'user' },
        'zdarzenie.dlaKogo': { value: 'ja_i_drugi_rodzic', source: 'user' },
        'dziecko.liczba': { value: 1, source: 'usc' },
        'dziecko.dataUrodzenia': { value: '2026-10-20', source: 'usc' },
      },
      wnioskodawca: {
        'osoba.rola': { value: 'ojciec', source: 'assumption' },
        'osoba.dataUrodzenia': { value: '1990-07-08', source: 'mobywatel' },
        'ubezpieczenie.tytuly': {
          source: 'zus',
          value: [
            {
              kodTytulu: '0110',
              opis: 'pracownik',
              rodzajTytulu: 'pracownik',
              platnikId: 'platnik-demo-2',
              platnikNazwa: 'Wzorcowy Zakład Demo',
              od: '2016-02-01',
              do: null,
              chorobowe: 'obowiazkowe',
              wymiarEtatu: 1,
            },
          ],
        },
        'ubezpieczenie.podstawySkladek': { source: 'zus', value: months('2025-10', 12, 9100) },
        'ubezpieczenie.przerwy': { source: 'zus', value: [] },
        'ubezpieczenie.pobraneSwiadczenia': { source: 'zus', value: [] },
        'platnik.liczbaUbezpieczonych': { source: 'zus', value: 310 },
      },
    },
  },
]

export const mockPersonas = (): PersonaListItem[] => personas.map((p) => ({ ...p.item }))

const find = (id: string) => personas.find((p) => p.item.id === id)

export function mockLogin(personaId: string): LoginResponse | undefined {
  const persona = find(personaId)
  if (!persona) return undefined
  return { ...persona.login, identity: { ...persona.login.identity }, sessionId: `${MOCK_SESSION_PREFIX}${personaId}` }
}

export function mockFacts(sessionId: string): FactSet | undefined {
  const persona = find(sessionId.slice(MOCK_SESSION_PREFIX.length))
  return persona ? (structuredClone(persona.facts) as FactSet) : undefined
}
