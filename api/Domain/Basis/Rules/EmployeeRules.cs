using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed class EmployeeAverageRule : BasisRule<BasisValue>
{
    public override string Id => "srednia_wynagrodzen_pracownika";

    public override string PodstawaPrawna => "ustawa art. 36 ust. 1–2, art. 38 ust. 1, art. 3 pkt 3, art. 47; instrukcja 8.7 pkt 1–2";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.Absence.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.Absence);
        }

        var months = i.Months.Value!;
        if (months.Insured.Count == 0)
        {
            return BasisSteps.Undecided<BasisValue>(this,
                "Zatrudnienie trwa krócej niż jeden pełny miesiąc kalendarzowy przed miesiącem powstania prawa (art. 37): wynagrodzenie z umowy albo za przepracowane dni nie występuje w danych z ZUS.");
        }

        var plan = i.Absence.Value!;
        var deduction = i.Param(BasisParameterKeys.OdliczenieSkladek, i.ReferenceDay);
        var sum = plan.Used.Sum(m => months.Contributions[m]);
        var gross = sum / plan.Used.Count;
        var average = Fmt.Round(gross * (1 - deduction));
        return BasisSteps.Met(this, new BasisValue(average, BasisSource.SzacunekZus, []),
            $"Średnia z {plan.Used.Count} pełnych miesięcy ({Fmt.Months(plan.Used)}): suma podstaw składki chorobowej {Fmt.Money(sum)} ÷ {plan.Used.Count} = {Fmt.Money(Fmt.Round(gross))}; po odjęciu {Fmt.Percent(deduction)} składek = {Fmt.Money(average)}.",
            plan.Used.Count == 12 ? "średnia z 12 miesięcy kalendarzowych" : "średnia z pełnych miesięcy ubezpieczenia");
    }
}

public sealed class EmployeeMinimumRule : BasisRule<BasisValue>
{
    public override string Id => "minimalna_podstawa_pracownika";

    public override string PodstawaPrawna => "ustawa art. 45 ust. 1, art. 47; instrukcja 8.2 i 8.7 pkt 5";

    protected override BasisStep<BasisValue> Compute(BasisInputs i)
    {
        if (!i.EmployeeAverage.Ok)
        {
            return BasisSteps.Blocked<BasisValue>(this, i.EmployeeAverage);
        }

        var average = i.EmployeeAverage.Value!;
        var fullMinimum = i.Param(BasisParameterKeys.PodstawaMinimalnaPelnyEtat, i.ReferenceDay);
        if (average.Amount >= fullMinimum)
        {
            return BasisSteps.Met(this, average,
                $"Średnia {Fmt.Money(average.Amount)} nie jest niższa od minimalnej podstawy dla pełnego etatu ({Fmt.Money(fullMinimum)}), więc dla żadnego wymiaru etatu minimum nie ma zastosowania.",
                "wynik nie niższy niż minimalna podstawa");
        }

        var workingTime = i.Title.Value!.Title.WymiarEtatu;
        if (workingTime is null)
        {
            return BasisSteps.Missing<BasisValue>(this,
                $"Średnia {Fmt.Money(average.Amount)} jest niższa od minimalnej podstawy dla pełnego etatu ({Fmt.Money(fullMinimum)}), a w danych nie ma wymiaru etatu, więc nie da się ustalić minimum.",
                BasisAsk.WorkingTime(i.Person));
        }

        if (workingTime is <= 0 or > 1)
        {
            return BasisSteps.Undecided<BasisValue>(this,
                $"Wymiar etatu {Fmt.Fraction(workingTime.Value)} w danych jest poza zakresem (0; 1].");
        }

        var minimum = Fmt.Round(fullMinimum * workingTime.Value);
        var assumption = workingTime == 1
            ? $"Wartość minimalnej podstawy ({Fmt.Money(fullMinimum)}) pochodzi z parametru {BasisParameterKeys.PodstawaMinimalnaPelnyEtat} (wyliczenie z płacy minimalnej, art. 45 ust. 1), a nie z cytatu w materiałach — do potwierdzenia z ZUS."
            : $"Minimalną podstawę dla niepełnego etatu liczę proporcjonalnie do wymiaru etatu ({Fmt.Money(fullMinimum)} × {Fmt.Fraction(workingTime.Value)}) zgodnie z instrukcją 8.2 i 8.7. Art. 45 ust. 1 ustawy mówi o pracy w pełnym wymiarze czasu pracy i proporcji nie przewiduje — założenie do potwierdzenia z ZUS.";
        if (average.Amount >= minimum)
        {
            return BasisSteps.Met(this, average,
                $"Średnia {Fmt.Money(average.Amount)} nie jest niższa od minimalnej podstawy dla etatu {Fmt.Fraction(workingTime.Value)} ({Fmt.Money(minimum)}).",
                "wynik nie niższy niż minimalna podstawa");
        }

        return BasisSteps.Met(this, average.With(minimum, BasisSource.Minimalna, assumption),
            $"Średnia {Fmt.Money(average.Amount)} jest niższa od minimalnej podstawy dla etatu {Fmt.Fraction(workingTime.Value)} ({Fmt.Money(minimum)}) — przyjmuję minimalną.",
            "wynik nie niższy niż minimalna podstawa");
    }
}
