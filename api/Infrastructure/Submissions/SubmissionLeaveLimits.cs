using System.Globalization;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;

namespace BabyRadnik.Infrastructure.Submissions;

/// <summary>
/// Twarde limity Kodeksu pracy, które da się sprawdzić z samych pól wniosku (bez danych sprawy):
/// urlop ojcowski w pełnych tygodniach i najwyżej w wymiarze z art. 182³ § 1, liczba części urlopu rodzicielskiego (art. 182¹c)
/// i długość jednej części rodzicielskiego dla jednego rodzica (art. 182¹a § 1–4). Wartości pochodzą z parametrów.
/// Termin złożenia wniosku (np. 21 dni przed urlopem) nie jest tu błędem — spóźniony wniosek nadal jest wnioskiem.
/// </summary>
public static partial class SubmissionValidator
{
    private static void CheckLeaveLimits(
        TemplateSpec spec, IReadOnlyDictionary<string, string> values, IParameterProvider parameters, Action<string, string> add)
    {
        try
        {
            if (spec.Id == LeaveTemplateIds.Ojcowski)
            {
                CheckPaternity(values, parameters, add);
            }
            else if (spec.Id is LeaveTemplateIds.Rodzicielski or LeaveTemplateIds.MacierzynskiIRodzicielski)
            {
                CheckParental(values, parameters, add);
            }
        }
        catch (ParameterNotFoundException)
        {
            // Bez parametru prawnego nie oceniam limitu — wniosek przechodzi tylko walidację formalną.
        }
    }

    private static void CheckPaternity(IReadOnlyDictionary<string, string> values, IParameterProvider parameters, Action<string, string> add)
    {
        if (DaysOf(values, "ojcowskiOd", "ojcowskiDo") is not { } range)
        {
            return;
        }

        var (from, days) = range;
        var weeks = parameters.GetInt(PeriodsParameterKeys.OjcowskiTyg, from);
        var minPart = parameters.GetInt(PeriodsParameterKeys.OjcowskiMinCzescTyg, from);
        if (days % 7 != 0 || days < minPart * 7)
        {
            add("ojcowskiDo", $"Urlop ojcowski udziela się w pełnych tygodniach; część nie może być krótsza niż {Weeks(minPart)} (KP art. 182³ § 1). Wniosek obejmuje {days} dni.");
            return;
        }

        if (days > weeks * 7)
        {
            add("ojcowskiDo", $"Urlop ojcowski trwa najwyżej {Weeks(weeks)} (KP art. 182³ § 1). Wniosek obejmuje {days / 7} tyg.");
            return;
        }

        if (Normalize(values, "korzystalZUrlopuOjcowskiego") == "tak" && days > (weeks - minPart) * 7)
        {
            add("ojcowskiDo", $"Skoro część urlopu ojcowskiego została już wykorzystana, do wykorzystania zostało najwyżej {Weeks(weeks - minPart)} (KP art. 182³ § 1).");
        }
    }

    private static void CheckParental(IReadOnlyDictionary<string, string> values, IParameterProvider parameters, Action<string, string> add)
    {
        var range = DaysOf(values, "rodzicielskiOd", "rodzicielskiDo");
        var date = range?.From ?? DateOnly.FromDateTime(DateTime.Today);

        if (Normalize(values, "liczbaWykorzystanychCzesci") is { } usedText
            && int.TryParse(usedText, NumberStyles.None, CultureInfo.InvariantCulture, out var used))
        {
            var maxParts = parameters.GetInt(PeriodsParameterKeys.RodzicielskiMaxCzesci, date);
            if (used >= maxParts)
            {
                add("liczbaWykorzystanychCzesci",
                    $"Urlop rodzicielski można wykorzystać najwyżej w {maxParts} częściach (KP art. 182¹c). Ten wniosek byłby {used + 1}. częścią.");
            }
        }

        // Przy łączeniu z pracą urlop wydłuża się proporcjonalnie (art. 182¹f), więc górnej granicy długości części tu nie oceniam.
        if (range is not { } leave || Normalize(values, "lacznieZPraca") == "tak")
        {
            return;
        }

        var (from, days) = leave;

        // Liczby dzieci wniosek nie podaje — przyjmuję najdłuższy wymiar (więcej niż jedno dziecko), żeby nie odrzucić poprawnego wniosku.
        var zaZyciem = Normalize(values, "zaZyciem") == "tak";
        var total = parameters.GetInt(PeriodsParameterKeys.RodzicielskiTyg(2, zaZyciem), from);
        var single = parameters.GetInt(PeriodsParameterKeys.RodzicielskiTyg(1, zaZyciem), from);
        var reserved = parameters.GetInt(PeriodsParameterKeys.RodzicielskiNieprzenoszalneTyg, from);
        var maxDays = (total - reserved) * 7;
        if (days > maxDays)
        {
            add("rodzicielskiDo",
                $"Jeden rodzic może wykorzystać najwyżej {Weeks(single - reserved)} urlopu rodzicielskiego przy jednym dziecku ({Weeks(total - reserved)} przy większej liczbie dzieci); " +
                $"{Weeks(reserved)} przysługuje wyłącznie drugiemu rodzicowi (KP art. 182¹a § 1–4). Wniosek obejmuje {days} dni.");
        }
    }

    private static (DateOnly From, int Days)? DaysOf(IReadOnlyDictionary<string, string> values, string fromKey, string toKey)
    {
        var from = Normalize(values, fromKey);
        var to = Normalize(values, toKey);
        if (from is null || to is null || !TryParseDate(from, out var f) || !TryParseDate(to, out var t) || t < f)
        {
            return null;
        }

        return (f, t.DayNumber - f.DayNumber + 1);
    }

    private static string Weeks(int weeks)
    {
        if (weeks == 1)
        {
            return "1 tydzień";
        }

        var fewForm = weeks % 10 is >= 2 and <= 4 && weeks % 100 is not (>= 12 and <= 14);
        return fewForm ? $"{weeks} tygodnie" : $"{weeks} tygodni";
    }
}
