import type { ReactNode } from 'react'
import { linkClass } from './link-class'

/** Źródło: Poradnik ZUS 2026, s. 6 (infolinia, cot@zus.pl, placówki) i s. 39 (infolinia). */
export const contactChannels: { id: string; node: ReactNode }[] = [
  {
    id: 'infolinia',
    node: (
      <>
        Infolinia ZUS (Centrum Kontaktu Klientów):{' '}
        <a href="tel:225601600" className={linkClass}>
          22 560 16 00
        </a>
      </>
    ),
  },
  {
    id: 'email',
    node: (
      <>
        E-mail do ZUS:{' '}
        <a href="mailto:cot@zus.pl" className={linkClass}>
          cot@zus.pl
        </a>
      </>
    ),
  },
  { id: 'placowka', node: <>Osobiście w każdej placówce ZUS</> },
]

export const institutions = [
  {
    id: 'zus',
    name: 'ZUS',
    url: 'https://www.zus.pl',
    why: 'Tu złożysz wnioski o zasiłki i sprawdzisz, jakie dokumenty są potrzebne.',
  },
  {
    id: 'mrpips',
    name: 'Ministerstwo Rodziny, Pracy i Polityki Społecznej',
    url: 'https://www.gov.pl/web/rodzina/urlop-rodzicielski',
    why: 'Tu przeczytasz o urlopie rodzicielskim.',
  },
  {
    id: 'pip',
    name: 'Państwowa Inspekcja Pracy',
    url: 'https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo',
    why: 'Tu znajdziesz porady o prawach pracownika związanych z rodzicielstwem.',
  },
  {
    id: 'usc',
    name: 'Rejestr stanu cywilnego',
    url: 'https://www.gov.pl/web/cyfryzacja/rejestr-stanu-cywilnego',
    why: 'Tu dowiesz się, jak uzyskać odpis aktu urodzenia.',
  },
] as const
