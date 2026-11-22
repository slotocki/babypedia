using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed class NonEmployeeAverageRule : BasisRule<BasisValue>
{
    public override string Id => "srednia_przychodu_niepracownika";

    public override string PodstawaPrawna => "ustawa art. 48 ust. 1–2, art. 3 pkt 4, art. 50, art. 52";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.Absence.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.Absence);
        }

        var months = i.Months.Value!;
        if (months.Insured.Count < 12)
        {
            return BasisSteps.NotMet<BasisValue>(this, "Ubezpieczenie trwa krócej niż 12 miesięcy — stosuje się art. 48a albo art. 49.");
        }

        var plan = i.Absence.Value!;
        var deduction = i.Param(BasisParameterKeys.OdliczenieSkladek, i.ReferenceDay);
        var sum = plan.Used.Sum(m => months.Contributions[m]);
        var gross = sum / plan.Used.Count;
        var average = Fmt.Round(gross * (1 - deduction));
        return BasisSteps.Met(this, new BasisValue(average, BasisSource.SzacunekZus, []),
            $"Przeciętny miesięczny przychód z {plan.Used.Count} miesięcy ({Fmt.Months(plan.Used)}): suma zadeklarowanych podstaw {Fmt.Money(sum)} ÷ {plan.Used.Count} = {Fmt.Money(Fmt.Round(gross))}; po odjęciu {Fmt.Percent(deduction)} = {Fmt.Money(average)}.",
            "przeciętny miesięczny przychód z 12 miesięcy kalendarzowych");
    }
}

public sealed class ShortInsuranceRule : BasisRule<BasisValue>
{
    public override string Id => "art48a_ubezpieczenie_krotsze_niz_12_miesiecy";

    public override string PodstawaPrawna =>
        "ustawa art. 48a ust. 1 i 5, art. 3 pkt 4, art. 50, art. 52; ustawa o systemie ubezpieczeń społecznych art. 18 ust. 8";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.Absence.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.Absence);
        }

        var months = i.Months.Value!;
        if (months.Insured.Count is 0 or >= 12)
        {
            return BasisSteps.NotMet<BasisValue>(this, "Ubezpieczenie trwa 12 miesięcy albo jest krótsze niż jeden pełny miesiąc — art. 48a nie dotyczy.");
        }

        var kind = i.Title.Value!.Title.RodzajTytulu;
        if (kind == TitleKind.Zleceniobiorca)
        {
            return BasisSteps.Undecided<BasisValue>(this,
                "Przy ubezpieczeniu krótszym niż 12 miesięcy art. 48a dotyczy podstawy będącej zadeklarowaną kwotą (działalność, osoba współpracująca); dla zleceniobiorcy stosuje się art. 49 ust. 1 pkt 2 (kwota z umowy), której nie ma w danych z ZUS.");
        }

        var plan = i.Absence.Value!;
        var day = i.ReferenceDay;
        var deduction = i.Param(BasisParameterKeys.OdliczenieSkladek, day);
        var lowest = plan.Used.ToDictionary(m => m, m => i.Param(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, m));
        foreach (var month in months.Insured)
        {
            var lowestThatMonth = lowest.TryGetValue(month, out var known) ? known : i.Param(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, month);
            if (months.Contributions[month] < lowestThatMonth)
            {
                return BasisSteps.Undecided<BasisValue>(this,
                    $"W miesiącu {Fmt.Months([month])} zadeklarowana podstawa {Fmt.Money(months.Contributions[month])} jest niższa od najniższej podstawy {Fmt.Money(lowestThatMonth)} — art. 48a ust. 5 (najniższa podstawa z art. 18 ust. 8 ustawy systemowej) wymaga, by przez cały okres działalności deklarowano nie mniej; innej najniższej podstawy (np. preferencyjnej) nie znamy.");
            }
        }

        var n = plan.Used.Count;
        var averageLowest = plan.Used.Average(m => lowest[m]) * (1 - deduction);
        var averageExcess = plan.Used.Average(m => months.Contributions[m] - lowest[m]) * (1 - deduction);
        var excessPart = averageExcess * n / 12;
        var basis = Fmt.Round(averageLowest + excessPart);
        return BasisSteps.Met(this, new BasisValue(basis, BasisSource.SzacunekZus, []),
            $"Ubezpieczenie krótsze niż 12 miesięcy ({n} pełnych miesięcy: {Fmt.Months(plan.Used)}): przeciętna najniższa podstawa po odliczeniu {Fmt.Percent(deduction)} = {Fmt.Money(Fmt.Round(averageLowest))} + (nadwyżka zadeklarowanej podstawy nad najniższą po odliczeniu {Fmt.Money(Fmt.Round(averageExcess))} × {n} ÷ 12 = {Fmt.Money(Fmt.Round(excessPart))}) = {Fmt.Money(basis)}.",
            "ubezpieczenie chorobowe krótsze niż 12 miesięcy");
    }
}

public sealed class FirstMonthRule : BasisRule<BasisValue>
{
    public override string Id => "art49_ubezpieczenie_krotsze_niz_miesiac";

    public override string PodstawaPrawna => "ustawa art. 49 ust. 1 pkt 1, art. 3 pkt 4, art. 52; ustawa o systemie ubezpieczeń społecznych art. 18 ust. 8";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.Absence.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.Absence);
        }

        if (i.Months.Value!.Insured.Count != 0)
        {
            return BasisSteps.NotMet<BasisValue>(this, "Jest co najmniej jeden pełny miesiąc ubezpieczenia — art. 49 nie dotyczy.");
        }

        if (i.Title.Value!.Title.RodzajTytulu == TitleKind.Zleceniobiorca)
        {
            return BasisSteps.Undecided<BasisValue>(this,
                "Ubezpieczenie zleceniobiorcy trwa krócej niż pełny miesiąc: art. 49 ust. 1 pkt 2 każe przyjąć kwotę z umowy (albo przeciętny przychód innych zleceniobiorców płatnika), czego nie ma w danych z ZUS.");
        }

        var day = i.ReferenceDay;
        var month = MonthMath.First(day);
        var deduction = i.Param(BasisParameterKeys.OdliczenieSkladek, day);
        var lowest = i.Param(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, month);
        var basis = Fmt.Round(lowest * (1 - deduction));
        return BasisSteps.Met(this, new BasisValue(basis, BasisSource.SzacunekZus, []),
            $"Ubezpieczenie chorobowe trwa krócej niż pełny miesiąc kalendarzowy: przyjmuję najniższą podstawę za miesiąc {Fmt.Months([month])} ({Fmt.Money(lowest)}) po odliczeniu {Fmt.Percent(deduction)} = {Fmt.Money(basis)}.",
            "ubezpieczenie chorobowe krótsze niż miesiąc kalendarzowy");
    }
}

public sealed class VoluntaryLimitRule : BasisRule<BasisValue>
{
    public override string Id => "limit_podstawy_dobrowolnej";

    public override string PodstawaPrawna => "Załącznik ZUS (dla osób niebędących pracownikami)";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.NonEmployeeEstimate.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.NonEmployeeEstimate);
        }

        var estimate = i.NonEmployeeEstimate.Value!;
        if (i.Title.Value!.Title.Chorobowe != SicknessInsurance.Dobrowolne)
        {
            return BasisSteps.Met(this, estimate, "Ubezpieczenie chorobowe nie jest dobrowolne — limit podstawy dla dobrowolnej składki nie ma zastosowania.");
        }

        var day = i.ReferenceDay;
        var maxContribution = i.Param(BasisParameterKeys.MaxPodstawaDobrowolnejSkladki, day);
        var limit = i.Param(BasisParameterKeys.LimitPodstawyZasilku, day);
        if (estimate.Amount <= limit)
        {
            return BasisSteps.Met(this, estimate,
                $"Podstawa {Fmt.Money(estimate.Amount)} nie przekracza limitu {Fmt.Money(limit)} (maksymalna dobrowolna podstawa składki {Fmt.Money(maxContribution)} po odliczeniu składek).",
                "limit podstawy zasiłku dla dobrowolnie ubezpieczonych");
        }

        return BasisSteps.Met(this, estimate.With(limit, BasisSource.SzacunekZus,
                $"Przychód przekracza maksymalną dobrowolną podstawę składki ({Fmt.Money(maxContribution)}); podstawa zasiłku ograniczona do limitu {Fmt.Money(limit)}."),
            $"Podstawa {Fmt.Money(estimate.Amount)} przekracza limit {Fmt.Money(limit)} (maksymalna dobrowolna podstawa składki {Fmt.Money(maxContribution)} po odliczeniu składek) — przyjmuję limit.",
            "limit podstawy zasiłku dla dobrowolnie ubezpieczonych");
    }
}
