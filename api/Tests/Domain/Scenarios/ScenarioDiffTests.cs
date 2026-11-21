using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Scenarios;
using static BabyRadnik.Tests.Domain.Scenarios.FakeEngine;
using static BabyRadnik.Tests.Domain.Scenarios.ScenarioKit;

namespace BabyRadnik.Tests.Domain.Scenarios;

public sealed class ScenarioDiffTests
{
    private static ScenarioComparer Comparer(FakeEngine engine) =>
        new(engine, Parameters(), ScenariosModule.BuiltIn());

    private static CompareRequest Wariant(FactSet? facts = null) =>
        new(
            facts ?? BirthWithBothParents(),
            [new ScenarioChange(ScenarioId.WariantWyplaty, PersonRef.Wnioskodawca, P("""{"wariant":"usredniony"}"""))],
            AsOf);

    [Fact]
    public void Status_weeks_and_amount_differences_are_computed_per_benefit_and_person()
    {
        var engine = new FakeEngine(
            Result(
                Benefit(BenefitId.Macierzynski, BenefitStatus.Przysluguje, weeks: 20, total: 10000m),
                Benefit(BenefitId.Rodzicielski, BenefitStatus.Przysluguje, weeks: 41, total: 20000m),
                Benefit(BenefitId.Ojcowski, BenefitStatus.Przysluguje, weeks: 2, total: 3000m, person: PersonRef.DrugiRodzic)),
            Result(
                Benefit(BenefitId.Macierzynski, BenefitStatus.NiePrzysluguje),
                Benefit(BenefitId.Rodzicielski, BenefitStatus.Przysluguje, weeks: 51, total: 24500.5m),
                Benefit(BenefitId.Ojcowski, BenefitStatus.Przysluguje, weeks: 2, total: 3000m, person: PersonRef.DrugiRodzic)));

        var result = Comparer(engine).Compare(Wariant());

        Assert.Empty(result.ValidationErrors);
        Assert.Equal(3, result.Diff.Count);

        var maternity = result.Diff.Single(d => d.Id == BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, maternity.StatusBase);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.StatusVariant);
        Assert.Equal(-20m, maternity.WeeksDelta);
        Assert.Equal(-10000m, maternity.AmountDelta);

        var parental = result.Diff.Single(d => d.Id == BenefitId.Rodzicielski);
        Assert.Equal(10m, parental.WeeksDelta);
        Assert.Equal(4500.5m, parental.AmountDelta);

        var paternity = result.Diff.Single(d => d.Id == BenefitId.Ojcowski);
        Assert.Equal(PersonRef.DrugiRodzic, paternity.Person);
        Assert.Equal(0m, paternity.WeeksDelta);
        Assert.Equal(0m, paternity.AmountDelta);
    }

    [Fact]
    public void Amount_delta_is_null_when_one_side_has_no_amount()
    {
        var engine = new FakeEngine(
            Result(Benefit(BenefitId.Rodzicielski, BenefitStatus.Przysluguje, weeks: 41, total: null)),
            Result(Benefit(BenefitId.Rodzicielski, BenefitStatus.Przysluguje, weeks: 51, total: 24000m)));

        var diff = Comparer(engine).Compare(Wariant()).Diff.Single();

        Assert.Equal(10m, diff.WeeksDelta);
        Assert.Null(diff.AmountDelta);
    }

    [Theory]
    [InlineData(BenefitStatus.BrakujeDanych)]
    [InlineData(BenefitStatus.NieRozstrzygam)]
    public void Weeks_and_amount_deltas_are_null_when_variant_is_undecided(BenefitStatus status)
    {
        var engine = new FakeEngine(
            Result(Benefit(BenefitId.Macierzynski, BenefitStatus.Przysluguje, weeks: 20, total: 10000m)),
            Result(Benefit(BenefitId.Macierzynski, status)));

        var diff = Comparer(engine).Compare(Wariant()).Diff.Single();

        Assert.Equal(status, diff.StatusVariant);
        Assert.Null(diff.WeeksDelta);
        Assert.Null(diff.AmountDelta);
    }

    [Fact]
    public void Benefit_missing_on_one_side_is_treated_as_not_applicable()
    {
        var engine = new FakeEngine(
            Result(),
            Result(Benefit(BenefitId.Ojcowski, BenefitStatus.Przysluguje, weeks: 2, total: 3000m, person: PersonRef.DrugiRodzic)));

        var diff = Comparer(engine).Compare(Wariant()).Diff.Single();

        Assert.Equal(BenefitStatus.NieDotyczy, diff.StatusBase);
        Assert.Equal(2m, diff.WeeksDelta);
        Assert.Equal(3000m, diff.AmountDelta);
    }

    [Fact]
    public void Base_is_computed_on_input_facts_and_variant_on_overlaid_facts()
    {
        var engine = new FakeEngine(Result(), Result());
        var facts = PregnantEmployee();
        var request = new CompareRequest(
            facts,
            [new ScenarioChange(ScenarioId.OdchodzeZPracy, PersonRef.Wnioskodawca, P("""{"data":"2026-11-15"}"""))],
            AsOf);

        Comparer(engine).Compare(request);

        Assert.Equal(2, engine.Requests.Count);
        Assert.Same(facts, engine.Requests[0].Facts);
        Assert.Equal(AsOf, engine.Requests[0].AsOf);
        Assert.Equal(D("2026-11-15"), engine.Requests[1].Facts.Wnioskodawca.UbezpieczenieTytuly!.Value.Single().Do);
        Assert.Null(facts.Wnioskodawca.UbezpieczenieTytuly!.Value.Single().Do);
    }

    [Fact]
    public void Several_scenarios_are_applied_in_order_on_the_same_facts()
    {
        var engine = new FakeEngine(Result(), Result());
        var request = new CompareRequest(
            BirthWithBothParents(),
            [
                new ScenarioChange(ScenarioId.WariantWyplaty, PersonRef.Wnioskodawca, P("""{"wariant":"usredniony"}""")),
                new ScenarioChange(ScenarioId.PracaNaCzescEtatu, PersonRef.Wnioskodawca, P("""{"wymiar":0.5,"tygodnie":20}""")),
                new ScenarioChange(ScenarioId.DrugiRodzicBierze, PersonRef.Wnioskodawca, P("""{"tygodnie":9}""")),
            ],
            AsOf);

        var result = Comparer(engine).Compare(request);

        Assert.Empty(result.ValidationErrors);
        var variantFacts = engine.Requests[1].Facts;
        Assert.Equal(PaymentVariant.Usredniony, variantFacts.Wnioskodawca.PlanWariantWyplaty!.Value);
        Assert.Equal(20, variantFacts.Wnioskodawca.PlanPracaNaCzescEtatu!.Value.Single().Tygodnie);
        Assert.Equal(9, variantFacts.DrugiRodzic!.PlanRodzicielskiCzesci!.Value.Single().Tygodnie);
    }

    [Fact]
    public void Invalid_scenario_returns_validation_errors_and_never_a_silent_no_op()
    {
        var engine = new FakeEngine(Result(Benefit(BenefitId.Macierzynski, BenefitStatus.Przysluguje)));
        var request = new CompareRequest(PregnantEmployee(), [new ScenarioChange(ScenarioId.OdchodzeZPracy)], AsOf);

        var result = Comparer(engine).Compare(request);

        Assert.Contains(result.ValidationErrors, e => e.Code == "scenariusz_brak_parametru");
        Assert.Empty(result.Diff);
        Assert.Same(result.Base, result.Variant);
        Assert.Single(engine.Requests);
    }

    [Fact]
    public void Errors_from_every_failing_scenario_are_reported_together()
    {
        var engine = new FakeEngine(Result());
        var request = new CompareRequest(
            PregnantEmployee(),
            [
                new ScenarioChange(ScenarioId.OdchodzeZPracy),
                new ScenarioChange(ScenarioId.SplacamZadluzenie, PersonRef.Wnioskodawca, P("""{"data":"2026-11-30"}""")),
            ],
            AsOf);

        var result = Comparer(engine).Compare(request);

        Assert.Equal(2, result.ValidationErrors.Count);
    }

    [Fact]
    public void Same_scenario_for_the_same_person_twice_is_an_error()
    {
        var engine = new FakeEngine(Result(), Result());
        var change = new ScenarioChange(ScenarioId.WariantWyplaty, PersonRef.Wnioskodawca, P("""{"wariant":"usredniony"}"""));

        var result = Comparer(engine).Compare(new CompareRequest(BirthWithBothParents(), [change, change], AsOf));

        Assert.Contains(result.ValidationErrors, e => e.Code == "scenariusz_powtorzony");
    }

    [Fact]
    public void Empty_scenario_list_is_an_error()
    {
        var result = Comparer(new FakeEngine(Result())).Compare(new CompareRequest(PregnantEmployee(), [], AsOf));

        Assert.Contains(result.ValidationErrors, e => e.Code == "scenariusz_brak");
    }

    [Fact]
    public void Engine_validation_errors_introduced_by_scenario_are_returned_but_not_inherited_ones()
    {
        var inherited = new ValidationIssue("stary", "Był już w bazie.", FactKeys.PlanPracaNaCzescEtatu, PersonRef.Wnioskodawca);
        var introduced = new ValidationIssue("rodzicielski_ponad_limit_jednego_rodzica", "Za dużo.", FactKeys.PlanRodzicielskiCzesci, PersonRef.DrugiRodzic);
        var engine = new FakeEngine(
            new AssessmentResult { RulesetVersion = "fake", ValidationErrors = [inherited] },
            new AssessmentResult { RulesetVersion = "fake", ValidationErrors = [inherited, introduced] });
        var request = new CompareRequest(
            BirthWithBothParents(),
            [new ScenarioChange(ScenarioId.DrugiRodzicBierze, PersonRef.Wnioskodawca, P("""{"tygodnie":33}"""))],
            AsOf);

        var result = Comparer(engine).Compare(request);

        Assert.Equal([introduced], result.ValidationErrors);
        Assert.Equal(2, result.Variant.ValidationErrors.Count);
    }
}
