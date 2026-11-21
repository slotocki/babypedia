"""Pobranie brzmienia pojedynczego artykulu z oficjalnego API Sejmu (ELI) do docs/prawo/pobrane/.

Nie odtwarza tresci z pamieci: pobiera PDF tekstu aktu z api.sejm.gov.pl, wyciaga tekst
(pdftotext), wycina artykul i czyści wylacznie formatowanie (naglowki stron, przeniesienia).
Wynik oznacza jako POBRANY - do potwierdzenia przez czlowieka.

Uzycie (z docs/prawo):
  py -I tools/pobierz_art.py <ELI np. DU/2026/1240> <art np. 17c> <plik-wyjsciowy.md> "<etykieta aktu>"
"""
import json
import re
import subprocess
import sys
import tempfile
import urllib.request
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = "https://api.sejm.gov.pl/eli/acts/"
LETTERS = "a-ząćęłńóśźżA-ZĄĆĘŁŃÓŚŹŻ"
MARK = re.compile(r"^(\d{1,3}[a-z]{0,2}\.\s|\d{1,3}[a-z]?\)\s|[a-z]\)\s|[–—-]\s)")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "babyradnik-prawo/1.0"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read()


def main():
    eli, art, out, label = sys.argv[1:5]
    meta = json.loads(get(API + eli).decode("utf-8"))
    pdf = get(API + eli + "/text.pdf")
    with tempfile.TemporaryDirectory() as td:
        p = Path(td) / "a.pdf"
        p.write_bytes(pdf)
        txt = subprocess.run(["pdftotext", "-layout", "-enc", "UTF-8", str(p), "-"],
                             capture_output=True, check=True).stdout.decode("utf-8", errors="replace")
    lines = []
    for l in txt.replace("\f", "\n").split("\n"):
        s = l.strip()
        if re.match(r"^(Dziennik Ustaw|Poz\.\s*\d+|.*–\s*\d+\s*–\s*Poz\.\s*\d+)", s) and len(s) < 60:
            continue
        lines.append(s)
    start = None
    for i, s in enumerate(lines):
        if re.match(rf"^Art\.\s*{re.escape(art)}\.\s", s):
            start = i
            break
    if start is None:
        sys.exit(f"nie znaleziono art. {art}")
    end = len(lines)
    for j in range(start + 1, len(lines)):
        if re.match(r"^Art\.\s*\d+[a-z]*\.\s", lines[j]) or re.match(r"^(Rozdział|Dział)\s", lines[j]):
            end = j
            break
    body = [l for l in lines[start:end] if l]
    # przeniesienia wyrazow
    joined = []
    for l in body:
        if joined and re.search(rf"[{LETTERS}]-$", joined[-1]) and re.match(r"[a-ząćęłńóśźż]", l):
            joined[-1] = joined[-1][:-1] + l
        else:
            joined.append(l)
    paras, cur = [], []
    for l in joined:
        if (cur and MARK.match(l) and not re.search(r"(§|art\.|ust\.|pkt|lit\.|poz\.|nr)$", cur[-1])
                and not (re.match(r"\d{1,3}[a-z]{0,2}\.\s", l) and not re.search(r"[.;:,)\-–—\"”]$", cur[-1]))):
            paras.append(" ".join(cur))
            cur = []
        cur.append(l)
    if cur:
        paras.append(" ".join(cur))
    paras[0] = re.sub(rf"^Art\.\s*{re.escape(art)}\.\s*", "", paras[0])
    head = [
        f"# POBRANE: {label}, art. {art}",
        "",
        "> **POBRANE — do potwierdzenia przez człowieka, nie pochodzi z materiałów organizatora.**",
        ">",
        f"> - Źródło: {API}{eli}/text.pdf (metadane: {API}{eli})",
        f"> - Akt: {meta.get('title')}",
        f"> - Adres publikacyjny: {meta.get('displayAddress')}; status w ISAP/API: {meta.get('status')} ({meta.get('inForce')}); "
        f"stan prawny wg obwieszczenia: {meta.get('legalStatusDate')}",
        f"> - Data pobrania: {date.today().isoformat()}",
        "> - Tekst wycięty skryptem `tools/pobierz_art.py` z PDF-a (usunięto tylko nagłówki stron i przeniesienia wyrazów).",
        "",
        f"## Art. {art}",
        "",
    ]
    (ROOT / "pobrane").mkdir(exist_ok=True)
    (ROOT / "pobrane" / out).write_text("\n".join(head + [p + "\n" for p in paras]).rstrip() + "\n",
                                       encoding="utf-8", newline="\n")
    print("zapisano", out, len(paras), "akapitow")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
