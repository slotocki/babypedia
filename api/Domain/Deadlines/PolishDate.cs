namespace BabyRadnik.Domain.Deadlines;

public static class PolishDate
{
    private static readonly string[] Genitive =
    [
        "stycznia", "lutego", "marca", "kwietnia", "maja", "czerwca",
        "lipca", "sierpnia", "września", "października", "listopada", "grudnia",
    ];

    public static string Words(DateOnly date) => $"{date.Day} {Genitive[date.Month - 1]} {date.Year}";
}
