using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public static class PeriodMath
{
    public const int DaysPerWeek = 7;

    public static int DaysInclusive(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber + 1;

    public static Period Span(DateOnly from, DateOnly to)
    {
        var days = DaysInclusive(from, to);
        return new Period(from, to, days, days / (decimal)DaysPerWeek);
    }

    public static Period ForDays(DateOnly from, int days) => Span(from, from.AddDays(days - 1));

    public static Period ForWeeks(DateOnly from, int weeks) => ForDays(from, weeks * DaysPerWeek);

    public static int CeilWeeks(int days) => (days + DaysPerWeek - 1) / DaysPerWeek;

    public static DateOnly LastDayBeforeBirthday(DateOnly birth, int years) => birth.AddYears(years).AddDays(-1);

    public static int FullYears(DateOnly birth, DateOnly on)
    {
        var years = on.Year - birth.Year;
        return birth.AddYears(years) > on ? years - 1 : years;
    }

    public static DateOnly EndOfCalendarYear(int year) => new(year, 12, 31);

    public static string Format(DateOnly date) => date.ToString("yyyy-MM-dd");

    public static string Describe(Period period) =>
        $"od {Format(period.From)} do {Format(period.To)} ({period.Days} dni, {period.Weeks:0.##} tyg.)";
}
