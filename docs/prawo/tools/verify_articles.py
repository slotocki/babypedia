"""Wyrywkowa weryfikacja artykulow: raw/*.txt vs *.md, slowo po slowie.

Uzycie (z docs/prawo): py -I tools/verify_articles.py
Dla kazdego wskazanego artykulu wycina jego tekst z raw (od wiersza naglowka do nastepnego
naglowka artykulu) i z .md (od '## Art. N' do kolejnego '## '), normalizuje (Legalis, przypisy
dolne, przeniesienia wyrazow, numery KP z indeksami) i porownuje ciagi slow.
"""
import difflib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SUPD = str.maketrans("⁰¹²³⁴⁵⁶⁷⁸⁹", "0123456789")
L = "a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ"

CHECKS = [
    ("ustawa-zasilkowa", ["2a", "4", "29", "30", "31", "48a", "45", "61"]),
    ("kodeks-pracy", ["180", "180²", "182¹a", "183", "182³", "177"]),
]


def raw_article(raw, art, kp):
    key = art.translate(SUPD)
    lines = raw.split("\n")
    out, on = [], False
    for ln in lines:
        s = ln.replace("\f", "").strip()
        if on and re.match(r"^(DZIAŁ|ROZDZIAŁ)\b", s):
            break
        m = re.match(r"^Art\.\s*(\d+[a-z]*)\b", s)
        if m and (s[len(m.group(0)):].strip() == "" or s[len(m.group(0)):].strip().startswith(("[", "(uchylon"))):
            if on:
                break
            if m.group(1) == key:
                on = True
        if on:
            out.append(s)
    txt = "\n".join(out)
    txt = re.sub(r"(?m)^Legalis$", "", txt)
    txt = re.sub(r"(?m)^\d{1,2} [A-ZĄĆĘŁŃÓŚŹŻ].*$", "", txt)   # przypisy dolne (jednowierszowe)
    txt = re.sub(rf"([{L}])-\n([a-ząćęłńóśźż])", r"\1\2", txt)
    return txt.split()


def md_article(md, art):
    lines = md.split("\n")
    out, on = [], False
    for ln in lines:
        if ln.startswith("#"):
            if on:
                break
            if ln.strip() == f"## Art. {art}":
                on = True
        if on:
            out.append(ln)
    txt = "\n".join(out)
    txt = re.sub(r"(?m)^## ", "", txt)
    return txt.split()


def main():
    bad = 0
    for base, arts in CHECKS:
        raw = (ROOT / "raw" / f"{base}.txt").read_text(encoding="utf-8")
        md = (ROOT / f"{base}.md").read_text(encoding="utf-8")
        kp = base == "kodeks-pracy"
        for a in arts:
            r, m = raw_article(raw, a, kp), md_article(md, a)
            # naglowek: raw "Art. 1821a" vs md "Art. 182¹a"
            r = [x.translate(SUPD) if i < 2 else x for i, x in enumerate(r)]
            m = [x.translate(SUPD) if i < 2 else x for i, x in enumerate(m)]
            sm = difflib.SequenceMatcher(None, r, m, autojunk=False)
            ops = [o for o in sm.get_opcodes() if o[0] != "equal"]
            status = "OK" if not ops else "ROZNICE"
            bad += bool(ops)
            print(f"{base} art. {a}: raw={len(r)} md={len(m)} slow zgodnych; {status}")
            for tag, i1, i2, j1, j2 in ops[:5]:
                print("    ", tag, r[i1:i2][:12], m[j1:j2][:12])
    print("artykulow z roznicami:", bad)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
