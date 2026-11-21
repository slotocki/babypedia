using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;
using Microsoft.Extensions.DependencyInjection;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class FullPipelineTests
{
    private static IReadOnlyList<ContributionBase> EightThousand() =>
        [.. Enumerable.Range(0, 12).Select(i => new ContributionBase(new DateOnly(2025, 11, 1).AddMonths(i).ToString("yyyy-MM"), 8000m))];

    private static FactSet Pregnancy(Action<PersonFactsBuilder>? configure = null, string date = "2027-03-15") =>
        Facts(Case(EventKind.Ciaza, date), Person(ParentRole.Matka, Title(from: "2021-02-01"), 150, b =>
        {
            b.Bases = EightThousand();
            configure?.Invoke(b);
        }));

    [Fact]
    public void Basis_and_amounts_modules_price_the_benefit_from_twelve_months_of_contributions()
    {
        var result = FullEngine().Assess(new AssessmentRequest(Pregnancy(), AsOf));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(1m, benefit.Rate);
        Assert.Equal(new Amount(230.11m, 32215.40m, 6903.20m, BasisSource.SzacunekZus), benefit.Amount);
        Assert.DoesNotContain(benefit.Notes, n => n.Contains("podaj podstawę", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(benefit.Notes, n => n.StartsWith("Prawo powstanie 15 marca 2027"));
        Assert.DoesNotContain(result.MissingFacts, m => m.Fact is FactKeys.PodstawaUstalonaPrzezPlatnika or FactKeys.UbezpieczeniePodstawySkladek);
        var parental = Get(result, BenefitId.Rodzicielski);
        Assert.Equal(0.7m, parental.Rate);
        Assert.Equal(6903.20m, parental.Amount!.Basis);
    }

    [Fact]
    public void Trace_runs_from_rights_through_period_and_basis_to_amounts()
    {
        var result = FullEngine().Assess(new AssessmentRequest(Pregnancy(), AsOf));

        var rules = Get(result, BenefitId.Macierzynski).Trace.Select(t => t.Rule).ToList();
        var rights = rules.IndexOf("ubezpieczenie_w_dniu_zdarzenia");
        var period = rules.IndexOf("macierzynski_wymiar");
        var basis = rules.IndexOf("tytul_ubezpieczenia_podstawy");
        var amount = rules.IndexOf("kwota_za_okres");
        Assert.True(rights >= 0 && rights < period && period < basis && basis < amount, string.Join(", ", rules));
    }

    [Fact]
    public void Without_contribution_data_the_basis_is_asked_for_and_the_amount_appears_once_it_is_given()
    {
        var bare = Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(from: "2021-02-01"), 150));
        var withPayerBasis = bare with
        {
            Wnioskodawca = bare.Wnioskodawca with { PodstawaUstalonaPrzezPlatnika = F(5000m) },
        };

        var asked = FullEngine().Assess(new AssessmentRequest(bare, AsOf));
        var answered = FullEngine().Assess(new AssessmentRequest(withPayerBasis, AsOf));

        Assert.Null(Get(asked, BenefitId.Macierzynski).Amount);
        Assert.Contains(Get(asked, BenefitId.Macierzynski).Notes, n => n.Contains("podaj podstawę", StringComparison.OrdinalIgnoreCase));
        var question = asked.MissingFacts.Single(m => m.Fact == FactKeys.PodstawaUstalonaPrzezPlatnika);
        Assert.Equal(MissingFactType.Money, question.Type);
        Assert.Equal(PersonRef.Wnioskodawca, question.Person);
        Assert.DoesNotContain(answered.MissingFacts, m => m.Fact == FactKeys.PodstawaUstalonaPrzezPlatnika);
        var amount = Get(answered, BenefitId.Macierzynski).Amount!;
        Assert.Equal(5000m, amount.Basis);
        Assert.Equal(BasisSource.UstalonaPrzezPlatnika, amount.BasisSource);
    }

    [Fact]
    public void Denied_benefits_are_not_priced()
    {
        var uninsured = Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, null, null, b => b.Titles = []));

        var result = FullEngine().Assess(new AssessmentRequest(uninsured, AsOf));

        Assert.All(result.Benefits, b =>
        {
            Assert.Null(b.Amount);
            Assert.Null(b.Rate);
        });
    }

    [Fact]
    public void Each_person_is_priced_from_their_own_basis()
    {
        var mother = Person(ParentRole.Matka, Title(from: "2021-02-01"), 150, b => b.Bases = EightThousand());
        var father = Person(ParentRole.Ojciec, Title(from: "2019-01-01", etat: 1m), 150, b =>
        {
            b.Bases = [.. EightThousand().Select(x => x with { Kwota = 4000m })];
            b.PaternityParts = [new LeavePart(2, D("2027-05-03"))];
        });
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-15"), mother, father);

        var result = FullEngine().Assess(new AssessmentRequest(facts, AsOf));

        Assert.Equal(6903.20m, Get(result, BenefitId.Macierzynski).Amount!.Basis);
        var paternity = Get(result, BenefitId.Ojcowski, PersonRef.DrugiRodzic).Amount!;
        Assert.Equal(4147.10m, paternity.Basis);
        Assert.Equal(BasisSource.Minimalna, paternity.BasisSource);
    }

    private static BenefitContext Context(BenefitId id, Period? period, params BenefitResult[] siblings)
    {
        var card = new BenefitResult { Id = id, Status = BenefitStatus.Przysluguje, Period = period };
        return new BenefitContext(card, siblings, Facts(Case(EventKind.Ciaza, "2027-03-15"), new PersonFacts()), AsOf, Params());
    }

    private static Period From(string date) => new(D(date), D(date).AddDays(139), 140, 20);

    [Fact]
    public void Rights_day_follows_the_first_maternity_period_of_the_same_person()
    {
        var maternity = new BenefitResult { Id = BenefitId.Macierzynski, Status = BenefitStatus.Przysluguje, Period = From("2027-02-15") };

        Assert.Equal(D("2027-02-15"), BasisServiceStep.RightsDay(Context(BenefitId.Macierzynski, maternity.Period, maternity)));
        Assert.Equal(D("2027-02-15"), BasisServiceStep.RightsDay(Context(BenefitId.Rodzicielski, From("2027-08-02"), maternity)));
        Assert.Equal(D("2027-02-15"), BasisServiceStep.RightsDay(Context(BenefitId.MacierzynskiUzupelniajacy, From("2027-08-02"), maternity)));
    }

    [Fact]
    public void Rights_day_falls_back_to_the_benefit_period_and_then_to_the_event_date()
    {
        Assert.Equal(D("2027-05-03"), BasisServiceStep.RightsDay(Context(BenefitId.Ojcowski, From("2027-05-03"))));
        Assert.Equal(D("2027-03-15"), BasisServiceStep.RightsDay(Context(BenefitId.Ojcowski, null)));
        Assert.Equal(D("2027-08-02"), BasisServiceStep.RightsDay(Context(BenefitId.Rodzicielski, From("2027-08-02"))));
    }

    [Fact]
    public void Catalog_module_picks_the_real_steps_when_basis_and_amounts_are_registered_and_empty_ones_otherwise()
    {
        var withModules = new ServiceCollection();
        withModules.AddSingleton<IParameterProvider>(Params());
        withModules.AddPeriods().AddEligibility().AddBasis().AddAmounts().AddCatalog();
        var without = new ServiceCollection();
        without.AddSingleton<IParameterProvider>(Params());
        without.AddPeriods().AddEligibility().AddCatalog();

        using var provider = withModules.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var bare = without.BuildServiceProvider();
        using var bareScope = bare.CreateScope();

        Assert.IsType<BasisServiceStep>(scope.ServiceProvider.GetRequiredService<IBasisStep>());
        Assert.IsType<AmountsServiceStep>(scope.ServiceProvider.GetRequiredService<IAmountsStep>());
        Assert.IsType<NoBasisStep>(bareScope.ServiceProvider.GetRequiredService<IBasisStep>());
        Assert.IsType<NoAmountsStep>(bareScope.ServiceProvider.GetRequiredService<IAmountsStep>());
    }
}
