"""Czyszczenie surowych wyciagow tekstu (docs/prawo/raw/*.txt) do Markdown.

Skrypt NIE zmienia tresci przepisow. Robi wylacznie:
  - usuwa stopki "Legalis", numery stron i naglowki biegnace,
  - przenosi przypisy dolne ze stron na koniec dokumentu (sekcja "Przypisy"),
  - skleja przeniesienia wyrazow ("ubezpie-" + "czeniem" -> "ubezpieczeniem"),
  - skleja zawiniete wiersze w akapity (granice: ust., pkt, lit., myslniki, naglowki),
  - oznacza artykuly / paragrafy / zalaczniki / rozdzialy naglowkami Markdown,
  - w numerach artykulow KP zamienia sklejone indeksy gorne na znaki indeksu
    (np. Art. 1821a -> Art. 182¹a). Tresc ustepow NIE jest ruszana.

Uzycie (z katalogu docs/prawo):
    py -I tools/clean_prawo.py [ustawa|kp|rozp2026|rozp2025|poradnik|all]
"""
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "raw"
SUP = str.maketrans("0123456789", "⁰¹²³⁴⁵⁶⁷⁸⁹")

LETTERS = "a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ"
LOWER = "a-ząćęłńóśźż"

MARKER_RES = [
    re.compile(r"^\d{1,3}[a-z]{0,2}\.\d{0,2}\s"),  # ust. 1.  1a.  oraz z cyfra przypisu: 1d.4
    re.compile(r"^\d{1,3}[a-z]?\)\s"),           # pkt 1)  2a)
    re.compile(r"^[a-z]\)\s"),                    # lit. a)
    re.compile(r"^§\s?\d{1,2}[a-z]?\.\s"),       # KP: § 1.
    re.compile(r"^[–—\-]\s"),                     # myslnik
    re.compile(r"^[▪•*]\s?"),                     # punktor
    re.compile(r"^\(…\)"),
    re.compile(r"^\(uchylon"),
]


def is_marker(t):
    return any(r.match(t) for r in MARKER_RES)


def read_pages(name):
    txt = (RAW / name).read_text(encoding="utf-8")
    return txt.split("\f")


def join_hyphens(lines, log):
    """Skleja koniec wiersza 'xx-' z poczatkiem nastepnego 'yy'."""
    out = []
    for ln in lines:
        if out and out[-1]["kind"] == "t" and ln["kind"] == "t" and out[-1]["text"]:
            a = out[-1]["text"]
            b = ln["text"]
            m = re.search(rf"([{LETTERS}]+)-$", a)
            if m and re.match(rf"[{LETTERS}]", b):
                frag = m.group(1)
                bw = re.match(rf"[{LETTERS}]+", b).group(0)
                ok = bw[0].islower() or (frag.isupper() and bw.isupper())
                if ok and not is_marker(b):
                    out[-1]["text"] = a[:-1] + b
                    log.append((frag + "-" + bw, frag + bw))
                    continue
        out.append(dict(ln))
    return out


# ---------------------------------------------------------------- legal acts
FOOT_RE = re.compile(r"^\d{1,2} (?=[A-ZĄĆĘŁŃÓŚŹŻ])")


def split_pages_legal(pages):
    """Zwraca (stream, footnotes). stream: dict(kind 't'|'b', text, indent, page)."""
    stream, foots = [], []
    for pi, page in enumerate(pages, start=1):
        lines = [l.rstrip() for l in page.replace("\f", "").split("\n")]
        lines = [l for l in lines if l.strip() != "Legalis"]
        while lines and not lines[-1].strip():
            lines.pop()
        # przypisy dolne: od pierwszego wiersza "N Tekst" (bez wciecia) do konca strony
        fi = None
        for i, l in enumerate(lines):
            if FOOT_RE.match(l):
                fi = i
                break
        if fi is not None:
            foot = [l.strip() for l in lines[fi:] if l.strip()]
            lines = lines[:fi]
            foots.append((pi, foot))
        while lines and not lines[-1].strip():
            lines.pop()
        for l in lines:
            if l.strip():
                txt = re.sub(r"\s+", " ", l.strip())
                # ustepy z cyfra przypisu (np. "1d.4 Jezeli") sklejone w jednym wierszu - podziel na granicy ustepu
                parts = re.split(r"(?<=[.;:]) (?=\d{1,2}[a-z]?\.\d{1,2} [A-ZĄĆĘŁŃÓŚŹŻ])", txt)
                for part in parts:
                    stream.append(dict(kind="t", text=part, indent=len(l) - len(l.lstrip(" ")), page=pi))
            else:
                stream.append(dict(kind="b", text="", indent=0, page=pi))
    return stream, foots


def kp_num(n):
    m = re.fullmatch(r"(\d{3})(\d)([a-z]*)", n)
    if m:
        return m.group(1) + m.group(2).translate(SUP) + m.group(3)
    return n


def clean_legal(src, dst, kind):
    pages = read_pages(src)
    stream, foots = split_pages_legal(pages)
    log = []

    # front matter: wszystko przed pierwszym samotnym "1" (znacznik przypisu tytulu)
    front, i = [], 0
    while i < len(stream) and not (stream[i]["kind"] == "t" and re.fullmatch(r"\d{1,2}", stream[i]["text"])):
        if stream[i]["kind"] == "t":
            front.append(stream[i]["text"])
        i += 1
    if i >= len(stream):
        i, front = 0, []
    else:
        i += 1
    title = " ".join(front)
    rest = stream[i:]
    lone = [s["text"] for s in rest if s["kind"] == "t" and re.fullmatch(r"\d{1,3}", s["text"])]
    rest = [s for s in rest if not (s["kind"] == "t" and re.fullmatch(r"\d{1,3}", s["text"]))]

    rest = join_hyphens(rest, log)

    out = [f"# {title}", ""]
    para = []
    nextpar = 1
    in_annex = False

    def flush():
        nonlocal para
        if para:
            out.append(" ".join(para))
            out.append("")
            para = []

    k = 0
    n = len(rest)
    after_blank = False
    while k < n:
        s = rest[k]
        if s["kind"] == "b":
            after_blank = True
            k += 1
            continue
        t = s["text"]
        blank_before = after_blank
        after_blank = False

        # --- rozdzial / dzial
        if re.match(r"^(DZIAŁ|ROZDZIAŁ|Oddział)\b", t) and s["indent"] >= 4:
            flush()
            head = t
            k += 1
            while (k < n and rest[k]["kind"] == "t" and rest[k]["indent"] >= 8
                   and not re.search(f"[{LOWER}]", rest[k]["text"])
                   and not re.match(r"^(DZIAŁ|ROZDZIAŁ|Oddział)", rest[k]["text"])):
                head += " " + rest[k]["text"]
                k += 1
            out.append(f"# {head}")
            out.append("")
            continue

        # --- artykul (ustawa, KP)
        if kind in ("ustawa", "kp"):
            m = re.match(r"^Art\.\s*(\d+[a-z]*)\b\s*(.*)$", t)
            if m:
                rest_t = m.group(2)
                if rest_t == "" or rest_t.startswith("[") or rest_t.startswith("(uchylon"):
                    flush()
                    num = m.group(1)
                    if kind == "kp":
                        num = kp_num(num)
                    out.append(f"## Art. {num}")
                    out.append("")
                    k += 1
                    if rest_t.startswith("["):
                        ttl = rest_t
                        while "]" not in ttl and k < n and rest[k]["kind"] == "t":
                            ttl += " " + rest[k]["text"]
                            k += 1
                        close = ttl.index("]")
                        body = ttl[close + 1:].strip()
                        ttl = ttl[:close + 1]
                        out.append(ttl)
                        out.append("")
                        if body:
                            para.append(body)
                    elif rest_t:
                        out.append(rest_t)
                        out.append("")
                    continue

        # --- paragraf rozporzadzenia
        if kind.startswith("rozp") and not in_annex:
            m = re.match(r"^§\s?(\d+)\b\s*(.*)$", t)
            if m and int(m.group(1)) == nextpar:
                flush()
                out.append(f"## § {m.group(1)}")
                out.append("")
                nextpar += 1
                if m.group(2):
                    para.append(m.group(2))
                k += 1
                continue
        if kind.startswith("rozp") and re.match(r"^Załącznik\s+\d+\.", t):
            flush()
            in_annex = True
            head = t
            k += 1
            while (k < n and rest[k]["kind"] == "t" and not is_marker(rest[k]["text"])
                   and not re.match(r"^[IVX]+\s", rest[k]["text"])):
                head += " " + rest[k]["text"]
                k += 1
            out.append(f"## {head}")
            out.append("")
            continue

        # --- zwykly wiersz
        brk = is_marker(t) and not (para and re.search(r"(?:§|art\.|ust\.|pkt|lit\.|poz\.|nr)$", para[-1]))
        if brk and para and re.match(r"^\d{1,3}[a-z]{0,2}\.\d{0,2}\s", t) and not re.search(r"[.;:,)\-–—\"”]$", para[-1]):
            brk = False   # "8. tygodnia" po zawinieciu wiersza to nie nowy ustep
        if (not brk and blank_before and para and re.search(r"[.:;)]$", para[-1])
                and re.match(r"^[A-ZĄĆĘŁŃÓŚŹŻ„\"(]", t)):
            brk = True
        if brk or not any(l.startswith("#") for l in out[2:]):
            flush()   # wiersze przed pierwszym naglowkiem (front matter) zostaja osobnymi liniami
        para.append(t)
        k += 1
    flush()

    if foots:
        out.append("## Przypisy")
        out.append("")
        out.append("*(przypisy dolne przeniesione ze stron surowego tekstu; numer przypisu = pierwszy znak wiersza)*")
        out.append("")
        for pi, foot in foots:
            foot = [d["text"] for d in join_hyphens([dict(kind="t", text=x, indent=0) for x in foot], log)]
            cur = []
            for l in foot:
                if FOOT_RE.match(l) and cur:
                    out.append(" ".join(cur))
                    out.append("")
                    cur = []
                cur.append(l)
            if cur:
                out.append(" ".join(cur))
                out.append("")
    note = [f"> Uwaga redakcyjna (nie jest częścią aktu). Plik wygenerowany skryptem `tools/clean_prawo.py` z `raw/{src}`; "
            "treść przepisów niezmieniona (usunięto stopki \"Legalis\", sklejono przeniesienia wyrazów i zawinięte wiersze, "
            "przypisy dolne przeniesiono na koniec). Cyfra doklejona do numeru ustępu po kropce (np. `4.3`, `1d.4`, `5a.10`) "
            "to znacznik przypisu z tekstu surowego, nie część numeru. Źródłem prawdy jest plik w `raw/`.", ">"]
    if kind == "kp":
        note += [
            "> Numeracja z indeksami górnymi: w tekście surowym indeksy zostały spłaszczone (np. `1821a` = art. 182¹a). "
            "W nagłówkach artykułów zapisano je znakami indeksu; w treści (numery § na początku ustępów oraz odwołania wewnątrz zdań, "
            "np. `art. 1801 § 2`, `§ 11`) pozostawiono postać surową. Numery § na początku ustępów, które po spłaszczeniu "
            "wyglądają jak liczby dwucyfrowe, a wg kolejności ustępów są paragrafami z indeksem (heurystyka: kolejność § w artykule):",
            ">"]
        for a, d, r in kp_sections(out):
            note.append(f"> - Art. {a}: `§ {d}` = § {r}")
        note += [">", "> Heurystykę trzeba potwierdzić w PDF przed użyciem w regule.", ""]
    else:
        note.append("")
    out[2:2] = note
    md = "\n".join(out).rstrip() + "\n"
    (ROOT / dst).write_text(md, encoding="utf-8", newline="\n")
    return log, lone, [(p, len(f)) for p, f in foots]


def kp_sections(out):
    """Heurystyka: ktore wiodace '§ NN.' to paragrafy z indeksem gornym (wg kolejnosci)."""
    res = []
    art, base, sup = None, 0, 0
    for l in out:
        m = re.match(r"^## Art\. (.+)$", l)
        if m:
            art, base, sup = m.group(1), 0, 0
            continue
        m = re.match(r"^§ (\d+)\.\s", l)
        if m and art:
            d = m.group(1)
            if int(d) == base + 1:
                base, sup = int(d), 0
            elif len(d) >= 2 and int(d[:-1]) == base and int(d[-1]) == sup + 1:
                sup = int(d[-1])
                res.append((art, d, f"{base}" + str(sup).translate(SUP)))
    return res


# ---------------------------------------------------------------- poradnik
def clean_poradnik(src, dst):
    pages = read_pages(src)
    log = []
    firsts = []
    for p in pages:
        ls = [l for l in p.replace("\f", "").split("\n") if l.strip()]
        firsts.append(ls[0].strip() if ls else "")
    cnt = Counter(firsts)
    headers = {h for h, c in cnt.items() if c >= 4 and h}

    toc_titles = []
    toc_pages = set()
    for pi, p in enumerate(pages):
        if p.count(". . .") > 5:
            toc_pages.add(pi)
            for l in p.split("\n"):
                if ". . ." in l:
                    ttl = re.sub(r"\s*(\. ){2,}\.?\s*\d+\s*$", "", l.strip()).strip()
                    ttl = re.sub(r"\s+", " ", ttl)
                    if ttl:
                        toc_titles.append((len(l) - len(l.lstrip()), ttl))
    # tytuly zawiniete w spisie tresci na 2 wiersze (wiersz 1 bez kropek) - dopisane recznie
    EXTRA = {
        "tytułów": "Zasady podlegania ubezpieczeniom społecznym i ubezpieczeniu zdrowotnemu z wybranych tytułów",
        "„Za życiem”": "Zasiłek macierzyński za okres urlopu rodzicielskiego dla rodziców dzieci z zaświadczeniem „Za życiem”",
    }
    toc_titles = [(i, EXTRA.get(t, t)) for i, t in toc_titles]
    indents = sorted({i for i, _ in toc_titles})
    level_of = {i: min(idx, 2) for idx, i in enumerate(indents)}
    title_level = {}
    toc_seq = []
    for ind, ttl in toc_titles:
        title_level.setdefault(ttl, level_of[ind])
        toc_seq.append((ttl, level_of[ind]))
    toc_pos = 0
    first_body_page = (max(toc_pages) + 1) if toc_pages else 0

    stream = []
    for pi, page in enumerate(pages):
        if pi in toc_pages:
            continue
        lines = [l.rstrip() for l in page.replace("\f", "").split("\n")]
        nonempty = [i for i, l in enumerate(lines) if l.strip()]
        pnum = None
        if nonempty and re.fullmatch(r"\d{1,3}", lines[nonempty[-1]].strip()):
            pnum = lines[nonempty[-1]].strip()
            lines[nonempty[-1]] = ""
        if nonempty and lines[nonempty[0]].strip() in headers:
            lines[nonempty[0]] = ""
        stream.append(dict(kind="m", text=f"<!-- s. {pnum} -->" if pnum else f"<!-- pdf p. {pi + 1} -->",
                           indent=0, body=pi >= first_body_page))
        for l in lines:
            if l.strip():
                stream.append(dict(kind="t", text=re.sub(r"\s+", " ", l.strip().replace("\u009d", "▶")),
                                   indent=len(l) - len(l.lstrip(" "))))
            else:
                stream.append(dict(kind="b", text="", indent=0))
    stream = join_hyphens(stream, log)

    out = ["# PORADNIK DLA RODZICÓW I OPIEKUNÓW DZIECI (ZUS, stan prawny na 1 kwietnia 2026 r.)", ""]
    para, pend, matched = [], [], set()

    def flush():
        nonlocal para
        if para:
            out.append(" ".join(para))
            out.append("")
            para = []

    after_blank = False
    in_body = False
    prev_indent = 0
    n = len(stream)
    k = 0
    while k < n:
        s = stream[k]
        if s["kind"] == "m":
            pend.append(s["text"])
            in_body = s["body"]
            k += 1
            continue
        if s["kind"] == "b":
            after_blank = True
            k += 1
            continue
        t = s["text"]
        blank_before = after_blank
        after_blank = False
        hit = None
        if not in_body:
            pass
        elif t in title_level:
            hit = (t, 1)
        else:
            j = k + 1
            while j < n and stream[j]["kind"] == "b":
                j += 1
            if j < n and stream[j]["kind"] == "t":
                two = t + " " + stream[j]["text"]
                if two in title_level:
                    hit = (two, j - k + 1)
        if hit and s["indent"] < 60:
            flush()
            for m in pend:
                out.append(m)
                out.append("")
            pend = []
            lvl = title_level[hit[0]]
            for j in range(toc_pos, len(toc_seq)):
                if toc_seq[j][0] == hit[0]:
                    lvl = toc_seq[j][1]
                    toc_pos = j + 1
                    break
            out.append("#" * (lvl + 2) + " " + hit[0])
            out.append("")
            matched.add(hit[0])
            k += hit[1]
            continue
        brk = is_marker(t) and not (para and re.search(r"(?:§|art\.|ust\.|pkt|lit\.|poz\.|nr)$", para[-1]))
        if brk and para and re.match(r"^\d{1,3}[a-z]{0,2}\.\d{0,2}\s", t) and not re.search(r"[.;:,)\-–—\"”]$", para[-1]):
            brk = False   # "8. tygodnia" po zawinieciu wiersza to nie nowy ustep
        if (not brk and blank_before and para and re.search(r"[.:;!?)]$", para[-1])
                and re.match(r"^[A-ZĄĆĘŁŃÓŚŹŻ„\"(▶]", t)):
            brk = True
        if (not brk and para and prev_indent - s["indent"] >= 10 and re.search(r"[.:;!?)]$", para[-1])
                and re.match(r"^[A-ZĄĆĘŁŃÓŚŹŻ„\"(▶]", t)):
            brk = True
        prev_indent = s["indent"]
        if brk:
            flush()
        if pend:
            if para:
                para.extend(pend)
            else:
                for m in pend:
                    out.append(m)
                    out.append("")
            pend = []
        para.append(t)
        k += 1
    flush()
    for m in pend:
        out.append(m)
    md = "\n".join(out).rstrip() + "\n"
    (ROOT / dst).write_text(md, encoding="utf-8", newline="\n")
    unmatched = [t for t in title_level if t not in matched]
    return log, unmatched, sorted(headers)


def main():
    which = sys.argv[1] if len(sys.argv) > 1 else "all"
    jobs = {
        "ustawa": lambda: clean_legal("ustawa-zasilkowa.txt", "ustawa-zasilkowa.md", "ustawa"),
        "kp": lambda: clean_legal("kodeks-pracy.txt", "kodeks-pracy.md", "kp"),
        "rozp2026": lambda: clean_legal("rozporzadzenie-2026-09-27-dokumenty-zasilki.txt",
                                        "rozporzadzenie-2026-09-27-dokumenty-zasilki.md", "rozp2026"),
        "rozp2025": lambda: clean_legal("rozporzadzenie-2025-03-11-wnioski-pracownicze.txt",
                                        "rozporzadzenie-2025-03-11-wnioski-pracownicze.md", "rozp2025"),
        "poradnik": lambda: clean_poradnik("poradnik-zus-2026.txt", "poradnik-zus-2026.md"),
    }
    names = list(jobs) if which == "all" else [which]
    for nm in names:
        res = jobs[nm]()
        log = res[0]
        print(f"== {nm}: {len(log)} sklejonych przeniesien; dodatkowe: {res[1:]}")
        uniq = sorted({a + " => " + b for a, b in log})
        (ROOT / "tools" / f"_joins_{nm}.txt").write_text("\n".join(uniq), encoding="utf-8")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
