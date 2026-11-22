using System.Globalization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public static class DebtCertificate
{
    public const string Banner = "SYMULACJA — dokument nie jest zaświadczeniem ZUS";

    /// <param name="onDate">Dzień, o który pyta użytkownik.</param>
    /// <param name="balance">Saldo z konta; <see cref="ContributionDebt.NaDzien"/> to dzień, na który saldo jest naprawdę znane (może być wcześniejszy niż <paramref name="onDate"/>).</param>
    public static DocumentModel Build(string fullName, ContributionDebt? balance, DateOnly onDate, DateOnly issuedOn, string number)
    {
        var rows = new List<DocumentRow>
        {
            new("Osoba, której dotyczy zestawienie", fullName, null),
            new("Stan na dzień", DocumentBuilder.FormatDate(onDate), null),
        };
        var notes = new List<NoteSpec>();

        if (balance is null)
        {
            rows.Add(new DocumentRow("Saldo rozliczeń z tytułu składek", "brak danych w symulatorze", null));
            rows.Add(new DocumentRow("Wynik", "Symulator nie prowadzi rachunku składkowego dla tej osoby (osoba nie opłaca składek samodzielnie).", null));
        }
        else
        {
            var known = DocumentBuilder.FormatDate(balance.NaDzien);
            var stale = balance.NaDzien != onDate;
            if (stale)
            {
                rows.Add(new DocumentRow("Saldo znane według stanu konta na dzień", known, null));
                notes.Add(new NoteSpec(
                    $"Symulator zna saldo konta tylko według stanu na {known}. Na dzień {DocumentBuilder.FormatDate(onDate)} przyjęto ostatnie znane saldo — wpłaty i nowe należności po {known} nie są uwzględnione."));
            }

            rows.Add(new DocumentRow("Saldo zadłużenia z tytułu składek", Money(balance.Kwota), null));
            var when = stale ? $"według stanu na {known}" : "na wskazany dzień";
            rows.Add(new DocumentRow("Wynik", balance.Kwota > 0m
                ? $"Symulowane zadłużenie składkowe {when}."
                : $"Brak symulowanego zadłużenia składkowego {when}.", null));
        }

        notes.Add(new NoteSpec("Dane pochodzą z fikcyjnego symulatora. Dokument nie ma mocy zaświadczenia o niezaleganiu w opłacaniu składek ani żadnego innego dokumentu urzędowego."));
        notes.Add(new NoteSpec("Prawdziwe zaświadczenie wydaje ZUS — informacje na https://www.zus.pl."));

        return new DocumentModel(
            "Zestawienie stanu rozliczeń z tytułu składek",
            number,
            issuedOn,
            null,
            null,
            Banner,
            [new DocumentSection("Dane z symulatora Kompasu Rodzica", rows)],
            [],
            notes,
            false,
            DateLabel: "Data wystawienia",
            NumberLabel: "Nr dokumentu");
    }

    public static string Money(decimal amount)
    {
        var text = amount.ToString("N2", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',');
        return text + " zł";
    }
}
