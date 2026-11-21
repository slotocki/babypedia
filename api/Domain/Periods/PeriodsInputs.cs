using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed record UzupelniajacyCase(string Label, int MaxWeeks, int CountFromDay, bool RequiresConsecutiveStay);

public sealed record WorkExtension(int Days, IReadOnlyList<string> Notes);

public sealed record RodzicielskiOutcome(Period? Period, IReadOnlyList<BenefitPart> Parts, IReadOnlyList<string> Notes);

public sealed record OjcowskiOutcome(Period? Period, IReadOnlyList<BenefitPart> Parts, IReadOnlyList<string> Notes);

public enum AdoptionLeaveCase
{
    Full,
    Truncated,
    Minimum,
}

public sealed record AdoptionLeave(Period Period, AdoptionLeaveCase Case, DateOnly AgeLimitLastDay);

public sealed record AgeLimit(DateOnly LastDay, bool Provisional);

public static class PeriodRules
{
    public static readonly MacierzynskiWymiarRule MacierzynskiWymiar = new();
    public static readonly MacierzynskiPrzedPorodemRule MacierzynskiPrzedPorodem = new();
    public static readonly MacierzynskiOkresRule MacierzynskiOkres = new();
    public static readonly MacierzynskiRezygnacjaRule MacierzynskiRezygnacja = new();
    public static readonly MartweUrodzenieZgonDzieckaRule MartweUrodzenieZgon = new();
    public static readonly SwiadczeniaPoZgonieDzieckaRule PoZgonieDziecka = new();

    public static readonly UzupelniajacyPrzypadekRule UzupelniajacyPrzypadek = new();
    public static readonly UzupelniajacyWymiarRule UzupelniajacyWymiar = new();
    public static readonly UzupelniajacyOkresRule UzupelniajacyOkres = new();

    public static readonly RodzicielskiWymiarRule RodzicielskiWymiar = new();
    public static readonly RodzicielskiNieprzenoszalneRule RodzicielskiNieprzenoszalne = new();
    public static readonly RodzicielskiCzesciRule RodzicielskiCzesci = new();
    public static readonly RodzicielskiTerminRule RodzicielskiTermin = new();
    public static readonly RodzicielskiPracaRule RodzicielskiPraca = new();
    public static readonly RodzicielskiPoczatekRule RodzicielskiPoczatek = new();
    public static readonly RodzicielskiOkresRule RodzicielskiOkres = new();

    public static readonly OjcowskiOknoRule OjcowskiOkno = new();
    public static readonly OjcowskiCzesciRule OjcowskiCzesci = new();
    public static readonly OjcowskiOkresRule OjcowskiOkres = new();

    public static readonly PrzyjecieSciezkaRule PrzyjecieSciezka = new();
    public static readonly PrzyjecieWiekRule PrzyjecieWiek = new();
    public static readonly PrzyjecieOkresRule PrzyjecieOkres = new();
    public static readonly PrzyjecieRodzicielskiWymiarRule PrzyjecieRodzicielskiWymiar = new();
}

public static class PeriodMissing
{
    public static MissingFact EventDate() =>
        new(FactKeys.ZdarzenieData, "Jaka jest data porodu (albo przewidywana data porodu) lub data przyjęcia dziecka?", MissingFactType.Date);

    public static MissingFact ChildCount() =>
        new(FactKeys.DzieckoLiczba, "Ile dzieci urodziło się (albo zostało przyjętych) jednocześnie?", MissingFactType.Integer);

    public static MissingFact Role(PersonRef person) =>
        new(FactKeys.OsobaRola, "Czy ta osoba jest matką, czy ojcem dziecka?", MissingFactType.Enum, person, ["matka", "ojciec"]);

    public static MissingFact EventKind() =>
        new(FactKeys.ZdarzenieRodzaj, "Co się dzieje: ciąża, urodzenie dziecka czy przyjęcie dziecka na wychowanie?", MissingFactType.Enum,
            Options: ["planuje", "ciaza", "urodzenie", "przyjecie"]);
}

public sealed class PeriodsInputs(EvaluationContext context)
{
    private readonly Dictionary<string, object> memo = [];

    public EvaluationContext Context { get; } = context;

    public PersonRef Person => Context.Person;

    public PersonRef OtherPerson => Person == PersonRef.Wnioskodawca ? PersonRef.DrugiRodzic : PersonRef.Wnioskodawca;

    public CaseFacts Case => Context.Facts.Sprawa;

    public PersonFacts Me => Context.PersonFacts ?? new PersonFacts();

    public PersonFacts? Other => Context.Facts.For(OtherPerson);

    public DateOnly AsOf => Context.AsOf;

    public EventKind? Kind => Case.ZdarzenieRodzaj?.Value;

    public bool IsAdoption => Kind == EventKind.Przyjecie;

    public bool IsBirth => Kind is EventKind.Planuje or EventKind.Ciaza or EventKind.Urodzenie;

    public DateOnly? EventDate => Case.ZdarzenieData?.Value;

    public ParentRole? Role => Me.OsobaRola?.Value;

    public int? ChildCount => Case.DzieckoLiczba?.Value;

    public bool ZaZyciem => Case.DzieckoZaZyciem?.Value == true;

    public bool Stillborn => Case.DzieckoMartweUrodzenie?.Value == true;

    public DateOnly? DeathDate => Case.DzieckoDataZgonu?.Value;

    public bool ChildDeceased => Stillborn || DeathDate is not null;

    /// <summary>Martwe urodzenie, a przy porodzie mnogim żadne dziecko nie pozostało przy życiu.</summary>
    public bool StillbornWithNoChildAlive =>
        Stillborn && (ChildCount ?? 1) <= (Case.DzieckoLiczbaZmarlych?.Value ?? 1);

    public MissingFact? NeedEventDate() => EventDate is null ? PeriodMissing.EventDate() : null;

    public MissingFact? NeedChildCount() => ChildCount is null ? PeriodMissing.ChildCount() : null;

    public decimal Param(string key) => Context.Parameters.GetDecimal(key, ParamDate);

    public int ParamInt(string key) => (int)Param(key);

    public decimal ParamAt(string key, DateOnly date) => Context.Parameters.GetDecimal(key, date);

    public DateOnly ParamDate => EventDate ?? throw new InvalidOperationException("Brak daty zdarzenia — parametry wymiaru wybiera dzień porodu lub przyjęcia.");

    public ValidationIssue Issue(string code, string message, string fact) => new(code, message, fact, Person);

    public static IReadOnlyList<LeavePart> RodzicielskiPlan(PersonFacts? person) =>
        person?.PlanRodzicielskiCzesci?.Value ?? [];

    public static IReadOnlyList<ReceivedBenefit> RodzicielskiPobrane(PersonFacts? person) =>
        person?.UbezpieczeniePobraneSwiadczenia?.Value.Where(b => b.Rodzaj == ReceivedBenefitKind.Rodzicielski).ToList() ?? [];

    public static int RodzicielskiPlannedDays(PersonFacts? person) =>
        RodzicielskiPlan(person).Sum(p => p.Tygodnie * PeriodMath.DaysPerWeek)
        + RodzicielskiPobrane(person).Sum(b => PeriodMath.DaysInclusive(b.Od, b.Do));

    public SortedSet<DateOnly>? HospitalDays => Memo(nameof(HospitalDays), () =>
    {
        var stays = Case.DzieckoPobytyWSzpitalu?.Value;
        if (stays is null)
        {
            return null;
        }

        var days = new SortedSet<DateOnly>();
        foreach (var stay in stays.Where(s => s.Do >= s.Od))
        {
            for (var day = stay.Od; day <= stay.Do; day = day.AddDays(1))
            {
                days.Add(day);
            }
        }

        return days;
    });

    public bool HasInvalidHospitalRange => Case.DzieckoPobytyWSzpitalu?.Value.Any(s => s.Do < s.Od) == true;

    public RuleStep<int> MacierzynskiWymiar => Memo(nameof(MacierzynskiWymiar), () => PeriodRules.MacierzynskiWymiar.Calculate(this));

    public RuleStep<int> MacierzynskiPrzedPorodem => Memo(nameof(MacierzynskiPrzedPorodem), () => PeriodRules.MacierzynskiPrzedPorodem.Calculate(this));

    public RuleStep<Period> MacierzynskiOkres => Memo(nameof(MacierzynskiOkres), () => PeriodRules.MacierzynskiOkres.Calculate(this));

    public RuleStep<Period> MartweUrodzenieZgon => Memo(nameof(MartweUrodzenieZgon), () => PeriodRules.MartweUrodzenieZgon.Calculate(this));

    public RuleStep<Period> MacierzynskiBlok => ChildDeceased ? MartweUrodzenieZgon : MacierzynskiOkres;

    public RuleStep<Period> MacierzynskiRezygnacja => Memo(nameof(MacierzynskiRezygnacja), () => PeriodRules.MacierzynskiRezygnacja.Calculate(this));

    public RuleStep<UzupelniajacyCase> UzupelniajacyPrzypadek => Memo(nameof(UzupelniajacyPrzypadek), () => PeriodRules.UzupelniajacyPrzypadek.Calculate(this));

    public RuleStep<int> UzupelniajacyWymiar => Memo(nameof(UzupelniajacyWymiar), () => PeriodRules.UzupelniajacyWymiar.Calculate(this));

    public RuleStep<Period> UzupelniajacyOkres => Memo(nameof(UzupelniajacyOkres), () => PeriodRules.UzupelniajacyOkres.Calculate(this));

    public RuleStep<int> RodzicielskiWymiar => Memo(nameof(RodzicielskiWymiar), () =>
        IsAdoption ? PeriodRules.PrzyjecieRodzicielskiWymiar.Calculate(this) : PeriodRules.RodzicielskiWymiar.Calculate(this));

    public RuleStep<bool> RodzicielskiNieprzenoszalne => Memo(nameof(RodzicielskiNieprzenoszalne), () => PeriodRules.RodzicielskiNieprzenoszalne.Calculate(this));

    public RuleStep<int> RodzicielskiCzesci => Memo(nameof(RodzicielskiCzesci), () => PeriodRules.RodzicielskiCzesci.Calculate(this));

    public RuleStep<DateOnly?> RodzicielskiTermin => Memo(nameof(RodzicielskiTermin), () => PeriodRules.RodzicielskiTermin.Calculate(this));

    public RuleStep<WorkExtension> RodzicielskiPraca => Memo(nameof(RodzicielskiPraca), () => PeriodRules.RodzicielskiPraca.Calculate(this));

    public RuleStep<DateOnly?> RodzicielskiPoczatek => Memo(nameof(RodzicielskiPoczatek), () => PeriodRules.RodzicielskiPoczatek.Calculate(this));

    public RuleStep<RodzicielskiOutcome> RodzicielskiOkres => Memo(nameof(RodzicielskiOkres), () => PeriodRules.RodzicielskiOkres.Calculate(this));

    public RuleStep<DateOnly> OjcowskiOkno => Memo(nameof(OjcowskiOkno), () => PeriodRules.OjcowskiOkno.Calculate(this));

    public RuleStep<IReadOnlyList<LeavePart>> OjcowskiCzesci => Memo(nameof(OjcowskiCzesci), () => PeriodRules.OjcowskiCzesci.Calculate(this));

    public RuleStep<OjcowskiOutcome> OjcowskiOkres => Memo(nameof(OjcowskiOkres), () => PeriodRules.OjcowskiOkres.Calculate(this));

    public RuleStep<AdoptionPath> PrzyjecieSciezka => Memo(nameof(PrzyjecieSciezka), () => PeriodRules.PrzyjecieSciezka.Calculate(this));

    public RuleStep<AgeLimit> PrzyjecieWiek => Memo(nameof(PrzyjecieWiek), () => PeriodRules.PrzyjecieWiek.Calculate(this));

    public RuleStep<AdoptionLeave> PrzyjecieOkres => Memo(nameof(PrzyjecieOkres), () => PeriodRules.PrzyjecieOkres.Calculate(this));

    private T Memo<T>(string key, Func<T> factory)
    {
        if (memo.TryGetValue(key, out var cached))
        {
            return (T)cached;
        }

        var value = factory();
        if (value is not null)
        {
            memo[key] = value;
        }

        return value;
    }
}
