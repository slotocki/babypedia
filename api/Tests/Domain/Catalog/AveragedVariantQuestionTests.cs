using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>Wariant uśredniony bez daty wniosku: po terminie 21 dni pytamy o datę wniosku zamiast cicho liczyć 70%.</summary>
public sealed class AveragedVariantQuestionTests
{
    private static FactSet Averaged(string persona, string? filed = null)
    {
        var facts = LoadPersona(persona);
        var me = facts.Wnioskodawca with
        {
            PlanWariantWyplaty = F(PaymentVariant.Usredniony),
            PlanDataWnioskuUsrednionego = filed is null ? null : F(D(filed)),
        };
        return facts with { Wnioskodawca = me };
    }

    private static bool AsksForDate(AssessmentResult result) =>
        result.MissingFacts.Any(m => m.Fact == FactKeys.PlanDataWnioskuUsrednionego && m.Person == PersonRef.Wnioskodawca);

    [Fact]
    public void After_birth_and_past_deadline_without_date_asks_for_application_date()
    {
        var result = Assess(Averaged("monika-demo"), engine: FullEngine());

        Assert.True(AsksForDate(result));
        var maternity = Get(result, BenefitId.Macierzynski);
        Assert.Equal(1m, maternity.Rate);
        Assert.Contains(maternity.Notes, n => n.Contains("minął", StringComparison.Ordinal));
    }

    [Fact]
    public void After_birth_with_application_in_time_applies_81_5_and_does_not_ask()
    {
        var result = Assess(Averaged("monika-demo", filed: "2026-10-20"), engine: FullEngine());

        Assert.False(AsksForDate(result));
        Assert.Equal(0.815m, Get(result, BenefitId.Macierzynski).Rate);
    }

    [Fact]
    public void Future_birth_assumes_application_in_time_and_does_not_ask()
    {
        var result = Assess(Averaged("klara-demo"), engine: FullEngine());

        Assert.False(AsksForDate(result));
        var maternity = Get(result, BenefitId.Macierzynski);
        Assert.Equal(0.815m, maternity.Rate);
        Assert.Contains(maternity.Notes, n => n.Contains("pod warunkiem", StringComparison.Ordinal));
    }

    [Fact]
    public void Standard_variant_never_asks_for_application_date()
    {
        var facts = LoadPersona("monika-demo");

        Assert.False(AsksForDate(Assess(facts, engine: FullEngine())));
    }
}
