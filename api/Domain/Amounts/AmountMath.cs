using System.Globalization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Amounts;

internal sealed class AmountsUndecidedException(string message) : Exception(message);

internal static class AmountMath
{
    public const int DaysPerWeek = 7;

    public static decimal Round(decimal value, int places) => Math.Round(value, places, MidpointRounding.AwayFromZero);

    public static int DaysInclusive(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber + 1;

    public static Period Span(DateOnly from, DateOnly to)
    {
        var days = DaysInclusive(from, to);
        return new Period(from, to, days, days / (decimal)DaysPerWeek);
    }

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string Money(decimal value) =>
        value.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',') + " zł";

    public static string Percent(decimal rate) =>
        (rate * 100).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "%";

    public static string Number(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    public static decimal Monthly(decimal basis, decimal rate) => Round(basis * rate, 2);
}
