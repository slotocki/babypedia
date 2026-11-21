using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Amounts;

internal sealed class AmountsContext(AmountsRequest request, IParameterProvider provider)
{
    private readonly Dictionary<(string Key, DateOnly Date), Parameter> cache = [];

    public FactSet Facts { get; } = request.Facts;

    public DateOnly AsOf { get; } = request.AsOf;

    public IReadOnlyList<BenefitResult> Cards { get; } = request.Benefits;

    public CaseFacts Case => Facts.Sprawa;

    public EventKind? Kind => Case.ZdarzenieRodzaj?.Value;

    public DateOnly? EventDate => Case.ZdarzenieData?.Value;

    public bool IsAdoption => Kind == EventKind.Przyjecie;

    public bool IsBirth => Kind is EventKind.Planuje or EventKind.Ciaza or EventKind.Urodzenie;

    /// <summary>Dzień rozstrzygający dla wymiarów: dzień porodu / przyjęcia dziecka (instrukcja 8.1).</summary>
    public DateOnly ParamDate => EventDate ?? AsOf;

    public DateOnly? ChildBirth => Case.DzieckoDataUrodzenia?.Value ?? (IsBirth ? EventDate : null);

    public PersonFacts? FactsOf(PersonRef person) => Facts.For(person);

    public static PersonRef Other(PersonRef person) =>
        person == PersonRef.Wnioskodawca ? PersonRef.DrugiRodzic : PersonRef.Wnioskodawca;

    public ParentRole? RoleOf(PersonRef person) => FactsOf(person)?.OsobaRola?.Value;

    public Parameter Get(string key, DateOnly date)
    {
        if (!cache.TryGetValue((key, date), out var parameter))
        {
            parameter = provider.Get(key, date);
            cache[(key, date)] = parameter;
        }

        return parameter;
    }

    public decimal Value(string key, DateOnly date) => Get(key, date).Value ?? throw new ParameterNotFoundException(key, date);

    public int Int(string key, DateOnly date) => (int)Value(key, date);

    public BasisInput? BasisFor(PersonRef person)
    {
        if (FactsOf(person)?.PodstawaUstalonaPrzezPlatnika is { } fromPayer)
        {
            return new BasisInput(fromPayer.Value, BasisSource.UstalonaPrzezPlatnika);
        }

        return request.Bases is not null && request.Bases.TryGetValue(person, out var basis) ? basis : null;
    }

    /// <summary>Podział przedziału dni na odcinki, w których obowiązuje ten sam wiersz parametru (wg <see cref="Parameter.ValidFrom"/>).</summary>
    public IReadOnlyList<(DateOnly From, DateOnly To, Parameter Parameter)> Timeline(string key, DateOnly from, DateOnly to)
    {
        var raw = new List<(DateOnly From, DateOnly To, Parameter Parameter)>();
        Walk(key, from, to, raw);
        var merged = new List<(DateOnly From, DateOnly To, Parameter Parameter)>();
        foreach (var segment in raw)
        {
            if (merged.Count > 0 && merged[^1].Parameter.ValidFrom == segment.Parameter.ValidFrom)
            {
                merged[^1] = (merged[^1].From, segment.To, merged[^1].Parameter);
            }
            else
            {
                merged.Add(segment);
            }
        }

        return merged;
    }

    private void Walk(string key, DateOnly from, DateOnly to, List<(DateOnly From, DateOnly To, Parameter Parameter)> into)
    {
        var first = Get(key, from);
        if (from == to || first.ValidFrom == Get(key, to).ValidFrom)
        {
            into.Add((from, to, first));
            return;
        }

        var middle = from.AddDays((to.DayNumber - from.DayNumber) / 2);
        Walk(key, from, middle, into);
        Walk(key, middle.AddDays(1), to, into);
    }
}
