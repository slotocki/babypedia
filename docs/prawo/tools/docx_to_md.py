"""Konwersja Zalacznika ZUS (DOCX) do Markdown z poprawnymi tabelami.

Czyta bezposrednio word/document.xml z oryginalnego DOCX (tylko do odczytu), zachowuje
kolejnosc akapitow i tabel. Tresci nie zmienia. Naglowki: akapity w calosci pogrubione
i krotkie. Uzycie:
    py -I tools/docx_to_md.py "<sciezka-do.docx>" zalacznik-zus.md
"""
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
ROOT = Path(__file__).resolve().parent.parent


def runs_bold(p):
    runs = [r for r in p.iter(W + "r") if "".join((t.text or "") for t in r.iter(W + "t")).strip()]
    if not runs:
        return False
    for r in runs:
        b = r.find(W + "rPr/" + W + "b")
        if b is None or b.get(W + "val") in ("0", "false"):
            return False
    return True


def ptext(p):
    out = []
    for el in p.iter():
        if el.tag == W + "t":
            out.append(el.text or "")
        elif el.tag == W + "tab":
            out.append(" ")
        elif el.tag == W + "br":
            out.append("\n")
    return "".join(out)


def cell_text(tc):
    paras = [ptext(p).strip() for p in tc.iter(W + "p")]
    paras = [x for x in paras if x]
    return "<br>".join(x.replace("\n", "<br>").replace("|", "\\|").replace("*", "\\*") for x in paras)


def main():
    src, dst = sys.argv[1], sys.argv[2]
    z = zipfile.ZipFile(src)
    root = ET.fromstring(z.read("word/document.xml"))
    body = root.find(W + "body")
    out = ["# Załącznik do Template Wyzwania HackNation 2026 (Załącznik ZUS)", "",
           "> Uwaga redakcyjna (nie jest częścią załącznika). Plik wygenerowany skryptem `tools/docx_to_md.py` "
           "bezpośrednio z oryginalnego DOCX (tabele odtworzone ze struktury dokumentu, nie z tekstu spłaszczonego). "
           "Treść niezmieniona. Źródłem porównawczym jest `raw/zalacznik-zus.txt`.", ""]
    first = True
    tables = 0
    for ch in body:
        if ch.tag == W + "p":
            t = re.sub(r"[ \t]+", " ", ptext(ch)).strip()
            if not t:
                continue
            if first:
                first = False
                continue  # pierwszy akapit = tytul pliku (juz w H1)
            if runs_bold(ch) and len(t) < 150:
                level = "##" if not re.match(r"^\d+\.\s", t) else "###"
                out += [f"{level} {t}", ""]
            else:
                out += [t.replace("\n", "  \n").replace("*", "\\*"), ""]
                if t.startswith("Wartości wyliczone od płacy minimalnej"):
                    out += ["> [Uwaga redakcyjna: w oryginalnym DOCX po tym zdaniu nie ma tabeli ani wartości "
                            "(brak elementów tabeli, obrazów i obiektów w document.xml). Patrz ROZBIEZNOSCI.md.]", ""]
        elif ch.tag == W + "tbl":
            tables += 1
            rows = []
            for tr in ch.findall(W + "tr"):
                rows.append([cell_text(tc) for tc in tr.findall(W + "tc")])
            ncol = max(len(r) for r in rows)
            rows = [r + [""] * (ncol - len(r)) for r in rows]
            out.append("| " + " | ".join(rows[0]) + " |")
            out.append("|" + "|".join(["---"] * ncol) + "|")
            for r in rows[1:]:
                out.append("| " + " | ".join(r) + " |")
            out.append("")
    (ROOT / dst).write_text("\n".join(out).rstrip() + "\n", encoding="utf-8", newline="\n")
    print(f"tabel: {tables}, plik: {dst}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
