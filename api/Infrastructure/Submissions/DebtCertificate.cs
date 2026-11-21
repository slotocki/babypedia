using System.Globalization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public static class DebtCertificate
{
    public const string Banner = "SYMULACJA — dokument nie jest zaświadczeniem ZUS";

    public static DocumentModel Build(string fullName, ContributionDebt? balance, DateOnly onDate, DateOnly issuedOn, string number)
    {
        var rows = new List<DocumentRow>
        {
            new("Osoba, której dotyczy zestawienie", fullName, null),
            new("Stan na dzień", DocumentBuilder.FormatDate(onDate), null),
        };

        if (balance is null)
        {
            rows.Add(new DocumentRow("Saldo rozliczeń z tytułu składek", "brak danych w symulatorze", null));
            rows.Add(new DocumentRow("Wynik", "Symulator nie prowadzi rachunku składkowego dla tej osoby (osoba nie opłaca składek samodzielnie).", null));
        }
        else
        {
            rows.Add(new DocumentRow("Saldo zadłużenia z tytułu składek", Money(balance.Kwota), null));
            rows.Add(new DocumentRow("Wynik", balance.Kwota > 0m ? "Symulowane zadłużenie składkowe na wskazany dzień." : "Brak symulowanego zadłużenia składkowego na wskazany dzień.", null));
        }

        return new DocumentModel(
            "Zestawienie stanu rozliczeń z tytułu składek",
            number,
            issuedOn,
            null,
            null,
            Banner,
            [new DocumentSection("Dane z symulatora Kompasu Rodzica", rows)],
            [],
            [
                new NoteSpec("Dane pochodzą z fikcyjnego symulatora. Dokument nie ma mocy zaświadczenia o niezaleganiu w opłacaniu składek ani żadnego innego dokumentu urzędowego."),
                new NoteSpec("Prawdziwe zaświadczenie wydaje ZUS — informacje na https://www.zus.pl."),
            ],
            false);
    }

    public static string Money(decimal amount)
    {
        var text = amount.ToString("N2", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',');
        return text + " zł";
    }
}
