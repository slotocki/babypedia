/** Czytelne nazwy parametrów (po wierszu tabeli parametrów). Brak wpisu = pokazujemy sam klucz. */
type Entry = { title: string; group: string }

const G = {
  leave: 'Urlopy i ich długość',
  rates: 'Stawki zasiłków',
  deadlines: 'Terminy składania wniosków',
  waiting: 'Choroba, ciąża i okres wyczekiwania',
  base: 'Podstawa wymiaru i kwoty',
  loss: 'Strata dziecka i zasiłek pogrzebowy',
  adoption: 'Przyjęcie dziecka na wychowanie',
  premature: 'Wcześniactwo (urlop uzupełniający)',
  debt: 'Zadłużenie składkowe i płatnik',
  other: 'Pozostałe',
} as const

const names: Record<string, Entry> = {
  macierzynski_tyg: { title: 'Długość urlopu macierzyńskiego (tygodnie, wg liczby dzieci)', group: G.leave },
  macierzynski_przed_porodem_max_tyg: { title: 'Ile urlopu macierzyńskiego można wziąć przed porodem (tygodnie)', group: G.leave },
  matka_min_po_porodzie_tyg: { title: 'Minimum urlopu matki po porodzie (tygodnie)', group: G.leave },
  matka_min_po_porodzie_szczegolne_tyg: { title: 'Minimum urlopu matki po porodzie w szczególnej sytuacji (tygodnie)', group: G.leave },
  rodzicielski_tyg: { title: 'Długość urlopu rodzicielskiego (tygodnie)', group: G.leave },
  rodzicielski_za_zyciem_tyg: { title: 'Długość urlopu rodzicielskiego z zaświadczeniem „Za życiem” (tygodnie)', group: G.leave },
  rodzicielski_nieprzenoszalne_tyg: { title: 'Tygodnie urlopu rodzicielskiego tylko dla każdego rodzica', group: G.leave },
  rodzicielski_max_czesci: { title: 'Na ile części można podzielić urlop rodzicielski', group: G.leave },
  rodzicielski_do_konca_roku_wieku: { title: 'Do jakiego wieku dziecka można wykorzystać urlop rodzicielski (lata)', group: G.leave },
  praca_na_rodzicielskim_max_etat: { title: 'Najwyższy wymiar etatu przy pracy na urlopie rodzicielskim', group: G.leave },
  rodzicielski_z_praca_max_tyg: { title: 'Długość urlopu rodzicielskiego przy łączeniu z pracą (tygodnie)', group: G.leave },
  ojcowski_tyg: { title: 'Urlop ojcowski (tygodnie i części)', group: G.leave },
  ojcowski_do_miesiaca_zycia: { title: 'Do którego miesiąca życia dziecka można wziąć urlop ojcowski', group: G.leave },
  pierwszy_rok_zycia_miesiecy: { title: 'Długość „pierwszego roku życia” dziecka (miesiące)', group: G.leave },
  ciaza_trwanie_tyg: { title: 'Zakładany czas trwania ciąży (tygodnie)', group: G.leave },
  stawka_macierzynski: { title: 'Stawka zasiłku macierzyńskiego', group: G.rates },
  stawka_uzupelniajacy: { title: 'Stawka zasiłku za uzupełniający urlop macierzyński', group: G.rates },
  stawka_ojcowski: { title: 'Stawka zasiłku za urlop ojcowski', group: G.rates },
  stawka_rodzicielski: { title: 'Stawka zasiłku za urlop rodzicielski', group: G.rates },
  stawka_usredniona: { title: 'Stawka przy wypłacie uśrednionej', group: G.rates },
  stawka_9_tyg_drugiego_rodzica: { title: 'Stawka za 9 tygodni drugiego rodzica', group: G.rates },
  chorobowy_ciaza_stawka: { title: 'Stawka zasiłku chorobowego w ciąży', group: G.rates },
  rehabilitacyjne_stawki: { title: 'Stawki świadczenia rehabilitacyjnego', group: G.rates },
  kwota_swiadczenia_rodzicielskiego: { title: 'Kwota świadczenia rodzicielskiego', group: G.rates },
  termin_wniosku_usredniona_dni: { title: 'Termin wniosku o wypłatę uśrednioną (dni po porodzie)', group: G.deadlines },
  termin_wniosku_macierzynski_przed_porodem_dni: { title: 'Termin wniosku o część urlopu przed porodem (dni)', group: G.deadlines },
  termin_rezygnacji_matki_dni: { title: 'Termin rezygnacji matki z reszty urlopu (dni)', group: G.deadlines },
  termin_wniosku_ojca_czesc_macierzynskiego_dni: { title: 'Termin wniosku ojca o część urlopu macierzyńskiego (dni)', group: G.deadlines },
  termin_wniosku_uzupelniajacy_dni: { title: 'Termin wniosku o urlop uzupełniający (dni)', group: G.deadlines },
  termin_wniosku_rodzicielski_dni: { title: 'Termin wniosku o urlop rodzicielski (dni przed)', group: G.deadlines },
  termin_wniosku_praca_czesc_etatu_dni: { title: 'Termin wniosku o pracę w części etatu (dni przed)', group: G.deadlines },
  termin_wniosku_ojcowski_dni: { title: 'Termin wniosku o urlop ojcowski (dni przed)', group: G.deadlines },
  termin_wniosku_przyjecie_dni: { title: 'Termin wniosku przy przyjęciu dziecka (dni)', group: G.deadlines },
  termin_wyplaty_dni: { title: 'Termin wypłaty zasiłku (dni)', group: G.deadlines },
  termin_wyplaty_po_splacie_dni: { title: 'Termin wypłaty po spłacie zadłużenia (dni)', group: G.deadlines },
  przedawnienie_mies: { title: 'Przedawnienie roszczenia o świadczenie (miesiące)', group: G.deadlines },
  chorobowy_ciaza_max_dni: { title: 'Najdłuższy okres zasiłku chorobowego w ciąży (dni)', group: G.waiting },
  wyczekiwanie_obowiazkowe_dni: { title: 'Okres wyczekiwania, ubezpieczenie obowiązkowe (dni)', group: G.waiting },
  wyczekiwanie_dobrowolne_dni: { title: 'Okres wyczekiwania, ubezpieczenie dobrowolne (dni)', group: G.waiting },
  wyczekiwanie_max_przerwa_dni: { title: 'Najdłuższa przerwa, która nie przerywa wyczekiwania (dni)', group: G.waiting },
  wyczekiwanie_wyjatek_lat_obowiazkowego: { title: 'Lata ubezpieczenia obowiązkowego znoszące wyczekiwanie', group: G.waiting },
  wyczekiwanie_absolwent_max_dni: { title: 'Najdłuższa przerwa po szkole przy wyczekiwaniu absolwenta (dni)', group: G.waiting },
  wynagrodzenie_chorobowe_dni: { title: 'Wynagrodzenie chorobowe od pracodawcy (dni, wg wieku)', group: G.waiting },
  wynagrodzenie_chorobowe_wiek_lat: { title: 'Wiek, od którego krótsze wynagrodzenie chorobowe (lata)', group: G.waiting },
  rehabilitacyjne_max_miesiecy: { title: 'Najdłuższy okres świadczenia rehabilitacyjnego (miesiące)', group: G.waiting },
  ciaza_umowa_przedluzenie_miesiace: { title: 'Przedłużenie umowy do dnia porodu: ile miesięcy ciąży', group: G.waiting },
  chorobowy_po_ustaniu_start_dni: { title: 'Zasiłek po ustaniu ubezpieczenia: w ciągu ilu dni od ustania (dni)', group: G.waiting },
  chorobowy_po_ustaniu_min_dni: { title: 'Zasiłek po ustaniu ubezpieczenia: minimalny okres (dni)', group: G.waiting },
  chorobowy_po_ustaniu_zakazna_miesiace: { title: 'Zasiłek po ustaniu ubezpieczenia: choroba zakaźna (miesiące)', group: G.waiting },
  odliczenie_skladek: { title: 'Odliczenie składek od podstawy zasiłku', group: G.base },
  placa_minimalna: { title: 'Płaca minimalna', group: G.base },
  podstawa_minimalna_pelny_etat: { title: 'Minimalna podstawa wymiaru (pełny etat)', group: G.base },
  max_podstawa_dobrowolnej_skladki: { title: 'Najwyższa podstawa dobrowolnej składki chorobowej', group: G.base },
  podstawa_dzielnik_dzienny: { title: 'Dzielnik do stawki dziennej', group: G.base },
  miesiac_dni: { title: 'Liczba dni w miesiącu do wyliczeń', group: G.base },
  zaokraglanie: { title: 'Zasady zaokrąglania kwot', group: G.base },
  kwoty: { title: 'Ustawienia kwot', group: G.base },
  martwe_urodzenie_tyg_po_porodzie: { title: 'Martwe urodzenie: urlop po porodzie (tygodnie)', group: G.loss },
  zgon_po_8_tyg_dni: { title: 'Zgon dziecka po 8. tygodniu: dni zasiłku', group: G.loss },
  zasilek_pogrzebowy: { title: 'Zasiłek pogrzebowy (kwota i termin wniosku)', group: G.loss },
  przyjecie_min_tyg: { title: 'Przyjęcie dziecka: minimalny urlop (tygodnie)', group: G.adoption },
  przyjecie_rodzicielski_przy_minimum_tyg: { title: 'Przyjęcie dziecka: urlop rodzicielski przy minimum (tygodnie)', group: G.adoption },
  przyjecie_start_urlopu_max_dni: { title: 'Przyjęcie dziecka: najpóźniejszy start urlopu (dni)', group: G.adoption },
  wiek_przysposobienie_max: { title: 'Najwyższy wiek dziecka przy przysposobieniu (lata)', group: G.adoption },
  wiek_rodzina_zastepcza_max: { title: 'Najwyższy wiek dziecka w rodzinie zastępczej (lata)', group: G.adoption },
  uzupelniajacy_max_tyg: { title: 'Urlop uzupełniający: maksymalna długość (tygodnie)', group: G.premature },
  uzupelniajacy_prog_tydzien_ciazy: { title: 'Urlop uzupełniający: progi tygodnia ciąży', group: G.premature },
  uzupelniajacy_prog_masa_g: { title: 'Urlop uzupełniający: próg masy urodzeniowej (g)', group: G.premature },
  uzupelniajacy_donoszony_min_kolejne_dni: { title: 'Urlop uzupełniający (donoszone): minimalna liczba kolejnych dni pobytu', group: G.premature },
  uzupelniajacy_donoszony_pierwszy_dzien_od: { title: 'Urlop uzupełniający (donoszone): pierwszy dzień pobytu, od', group: G.premature },
  uzupelniajacy_donoszony_pierwszy_dzien_do: { title: 'Urlop uzupełniający (donoszone): pierwszy dzień pobytu, do', group: G.premature },
  uzupelniajacy_donoszony_liczenie_od_dnia: { title: 'Urlop uzupełniający (donoszone): liczenie od dnia', group: G.premature },
  prog_zadluzenia: { title: 'Próg zadłużenia składkowego blokujący zasiłek', group: G.debt },
  termin_splaty_zadluzenia_mies: { title: 'Termin spłaty zadłużenia (miesiące)', group: G.debt },
  prog_platnika_ubezpieczonych: { title: 'Próg liczby ubezpieczonych u płatnika (kto wypłaca zasiłek)', group: G.debt },
}

const variants: Record<string, string> = {
  dzieci_1: '1 dziecko',
  dzieci_2: '2 dzieci',
  dzieci_3: '3 dzieci',
  dzieci_4: '4 dzieci',
  dzieci_5: '5 i więcej dzieci',
  jedno_dziecko: 'jedno dziecko',
  wiecej_dzieci: 'więcej dzieci',
  za_zyciem_jedno_dziecko: '„Za życiem”, jedno dziecko',
  za_zyciem_wiecej_dzieci: '„Za życiem”, więcej dzieci',
  do_50_lat: 'do 50. roku życia',
  od_50_lat: 'od 50. roku życia',
}

export interface ParameterName {
  title: string
  group: string
  variant?: string
}

export function parameterName(key: string, wiersz: string | undefined): ParameterName | null {
  const base = key.split('.')[0]
  const entry = names[base] ?? (wiersz ? names[wiersz] : undefined)
  if (!entry) return null
  const suffix = key.includes('.') ? key.slice(key.indexOf('.') + 1) : undefined
  return { ...entry, variant: suffix ? (variants[suffix] ?? suffix.replace(/_/g, ' ')) : undefined }
}

export const otherGroup = G.other

/** Kolejność grup w panelu. */
export const groupOrder: string[] = [G.leave, G.rates, G.deadlines, G.waiting, G.base, G.loss, G.adoption, G.premature, G.debt, G.other]
