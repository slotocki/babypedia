"""Weryfikacja zgodnosci .md z raw/*.txt (niezalezna od clean_prawo.py).

Porownuje ciagi tokenow (slow rozdzielonych bialymi znakami) z raw i z .md,
po usunieciu z raw: stopek "Legalis", form feed, samotnych numerow stron; po
sklejeniu przeniesien wyrazow prostym wyrazeniem; z .md: znacznikow naglowkow.
Wypisuje wszystkie roznice (insert/delete/replace), zeby mozna je ocenic recznie.

Uzycie (z docs/prawo):  py -I tools/verify_prawo.py <plik-bazowy-bez-rozszerzenia> [ile-roznic]
"""
import difflib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
L = "a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ"


def raw_tokens(name):
    t = (ROOT / "raw" / (name + ".txt")).read_text(encoding="utf-8").replace("\f", "\n")
    t = re.sub(r"(?m)^\s*Legalis\s*$", "", t)
    t = re.sub(rf"([{L}])-\n\s*([{'a-ząćęłńóśźż'}])", r"\1\2", t)
    t = t.replace("\u009d", "▶")
    return t.split()


def md_tokens(name):
    t = (ROOT / (name + ".md")).read_text(encoding="utf-8")
    t = re.sub(r"<!--.*?-->", " ", t)
    t = re.sub(r"(?m)^>.*$", "", t)  # uwaga redakcyjna
    t = re.sub(r"(?m)^#{1,6} ", "", t)
    t = t.replace("*(przypisy dolne przeniesione ze stron surowego tekstu; numer przypisu = pierwszy znak wiersza)*", "")
    return t.split()


def main():
    name = sys.argv[1]
    lim = int(sys.argv[2]) if len(sys.argv) > 2 else 80
    a, b = raw_tokens(name), md_tokens(name)
    sm = difflib.SequenceMatcher(None, a, b, autojunk=False)
    ops = [o for o in sm.get_opcodes() if o[0] != "equal"]
    same = sum(i2 - i1 for tag, i1, i2, j1, j2 in sm.get_opcodes() if tag == "equal")
    print(f"{name}: raw={len(a)} md={len(b)} zgodne={same} roznic={len(ops)}")
    for tag, i1, i2, j1, j2 in ops[:lim]:
        print(f"  {tag:7} raw[{i1}:{i2}]={' '.join(a[max(i1,0):i2])[:100]!r}  md={' '.join(b[j1:j2])[:100]!r}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
