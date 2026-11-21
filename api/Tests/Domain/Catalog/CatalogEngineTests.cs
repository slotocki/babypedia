using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;
using Microsoft.Extensions.DependencyInjection;
using BabyRadnik.Tests.Reference;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class CatalogEngineTests
{
    private static FactSet Pregnancy(PersonFacts me, PersonFacts? other = null, string date = "2027-03-15", EventKind kind = EventKind.Ciaza) =>
        Facts(Case(kind, date), me, other);

    private static PersonFacts Mother(Action<PersonFactsBuilder>? configure = null) => Person(ParentRole.Matka, Title(), 150, configure);

    private static PersonFacts Father(Action<PersonFactsBuilder>? configure = null) => Person(ParentRole.Ojciec, Title(), 150, configure);

    [Fact]
    public void Catalog_has_the_ten_p0_entries_with_unique_ids_names_and_links()
    {
        var entries = BenefitCatalog.Entries;

        Assert.Equal(Enum.GetValues<BenefitId>().Order(), entries.Select(e => e.Id).Order());
        Assert.Equal(10, entries.Select(e => e.Id).Distinct().Count());
        Assert.All(entries, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Name));
            Assert.NotEmpty(e.RequiredFacts);
            Assert.All(e.RequiredFacts, key => Assert.True(FactQuestionBook.All.ContainsKey(key), $"{e.Id}: {key}"));
            Assert.Contains(e.Links, l => l.Url == "https://www.zus.pl");
        });
        Assert.Equal("Zasiłek pogrzebowy (informacja)", BenefitCatalog.Get(BenefitId.Pogrzebowy).Name);
    }

    [Fact]
    public void Leave_benefits_link_to_mrpips_and_pip()
    {
        foreach (var id in new[] { BenefitId.Macierzynski, BenefitId.MacierzynskiUzupelniajacy, BenefitId.Rodzicielski, BenefitId.Ojcowski, BenefitId.PrzyjecieDziecka, BenefitId.PrzejeciePrzezOjca })
        {
            var urls = BenefitCatalog.Get(id).Links.Select(l => l.Url).ToList();
            Assert.Contains("https://www.gov.pl/web/rodzina/urlop-rodzicielski", urls);
            Assert.Contains("https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo", urls);
        }
    }

    [Fact]
    public void Benefit_names_match_the_contract_identifiers()
    {
        foreach (var id in Enum.GetValues<BenefitId>())
        {
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(id).Trim('"'), BenefitNames.Of(id));
        }
    }

    [Fact]
    public void Every_catalog_entry_gets_exactly_one_status_per_person()
    {
        var result = Assess(Pregnancy(Mother(), Father()));

        Assert.Equal(20, result.Benefits.Count);
        foreach (var person in new[] { PersonRef.Wnioskodawca, PersonRef.DrugiRodzic })
        {
            Assert.Equal(
                BenefitCatalog.Entries.Select(e => e.Id).Order(),
                result.Benefits.Where(b => b.Person == person).Select(b => b.Id).Order());
        }

        Assert.Equal("2026-11-21", result.RulesetVersion);
    }

    [Fact]
    public void Single_person_request_yields_ten_benefits_and_keeps_not_applicable_ones()
    {
        var result = Assess(Pregnancy(Mother()));

        Assert.Equal(10, result.Benefits.Count);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(result, BenefitId.Ojcowski).Status);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(result, BenefitId.Pogrzebowy).Status);
    }

    [Fact]
    public void Granted_benefit_shows_period_payer_documents_links_and_trace_with_rights_first()
    {
        var result = Assess(Pregnancy(Mother()));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        Assert.Equal(new Period(D("2027-03-15"), D("2027-08-01"), 140, 20), benefit.Period);
        Assert.Equal(Payer.PlatnikSkladek, benefit.Payer);
        Assert.Contains(benefit.Documents, d => d.StartsWith("ZAM"));
        Assert.Contains(benefit.Documents, d => d.Contains("pracodawcy"));
        Assert.Equal(3, benefit.Links.Count);
        Assert.Equal("ubezpieczenie_w_dniu_zdarzenia", benefit.Trace[0].Rule);
        var rules = benefit.Trace.Select(t => t.Rule).ToList();
        Assert.Equal("macierzynski_wymiar", rules.First(r => r.StartsWith("macierzynski_")));
        Assert.True(rules.IndexOf("platnik_zasilku_i_miejsce_zlozenia") < rules.IndexOf("macierzynski_wymiar"));
    }

    [Fact]
    public void Denied_benefit_has_reason_legal_basis_and_no_period()
    {
        var uninsured = Person(ParentRole.Matka, null, null, b => b.Titles = []);

        var result = Assess(Pregnancy(uninsured));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, benefit.Status);
        Assert.False(string.IsNullOrWhiteSpace(benefit.Reason));
        Assert.False(string.IsNullOrWhiteSpace(benefit.LegalBasis));
        Assert.Null(benefit.Period);
        Assert.Empty(benefit.Parts);
    }

    [Fact]
    public void Denial_from_periods_wins_over_granted_rights()
    {
        var result = Assess(Facts(Case(EventKind.Urodzenie, "2025-10-01"), Father()));

        var benefit = Get(result, BenefitId.Ojcowski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, benefit.Status);
        Assert.Null(benefit.Period);
        Assert.Contains("ojcowski", benefit.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(benefit.Trace, t => t.Rule == "ubezpieczenie_w_dniu_zdarzenia");
        Assert.Contains(benefit.Trace, t => t.Met == false);
    }

    [Fact]
    public void Undecided_from_periods_yields_contact_channels()
    {
        var provider = new WithoutKeys(Params(), "macierzynski_tyg.dzieci_1");

        var result = Assess(Pregnancy(Mother()), engine: Engine(provider));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NieRozstrzygam, benefit.Status);
        Assert.Contains("macierzynski_tyg.dzieci_1", benefit.Reason!);
        Assert.Contains(StatusComposer.ContactNote, benefit.Notes);
        Assert.Contains(benefit.Links, l => l.Url == "https://www.zus.pl/kontakt");
    }

    [Fact]
    public void Trace_of_undecided_and_missing_benefits_keeps_the_partial_steps()
    {
        var noTitles = new PersonFactsBuilder { Role = ParentRole.Matka }.Build();

        var result = Assess(Pregnancy(noTitles));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.BrakujeDanych, benefit.Status);
        Assert.Null(benefit.Period);
        Assert.Contains(benefit.Trace, t => t.Met is null);
    }

    [Fact]
    public void Disabled_catalog_entries_are_omitted_with_their_questions()
    {
        var settings = new FixedCatalogSettings([BenefitId.Rodzicielski, BenefitId.ChorobowyWCiazy]);
        var noTitles = new PersonFactsBuilder { Role = ParentRole.Matka }.Build();

        var all = Assess(Pregnancy(Mother()));
        var some = Assess(Pregnancy(Mother()), engine: Engine(settings: settings));

        Assert.Equal(8, some.Benefits.Count);
        Assert.DoesNotContain(some.Benefits, b => b.Id is BenefitId.Rodzicielski or BenefitId.ChorobowyWCiazy);
        Assert.Equal(Get(all, BenefitId.Macierzynski).Period, Get(some, BenefitId.Macierzynski).Period);
        Assert.DoesNotContain("wniosek_rodzicielski", some.Benefits.SelectMany(b => b.Deadlines).Select(d => d.Id));
        var none = Assess(Pregnancy(noTitles), engine: Engine(settings: new FixedCatalogSettings(Enum.GetValues<BenefitId>())));
        Assert.Empty(none.Benefits);
        Assert.DoesNotContain(none.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieTytuly);
    }

    [Fact]
    public void Request_specific_settings_override_the_registered_ones()
    {
        var engine = Engine();

        var result = engine.Assess(new AssessmentRequest(Pregnancy(Mother()), AsOf), new FixedCatalogSettings([BenefitId.Macierzynski]));

        Assert.DoesNotContain(result.Benefits, b => b.Id == BenefitId.Macierzynski);
        Assert.Equal(9, result.Benefits.Count);
    }

    [Fact]
    public void As_of_defaults_to_the_current_date()
    {
        var engine = Engine(today: D("2027-04-06"));

        var result = engine.Assess(new AssessmentRequest(Pregnancy(Mother())));

        Assert.Contains("minął", Get(result, BenefitId.Macierzynski).Deadlines.Single(d => d.Id == "wniosek_usredniona").Text);
    }

    [Fact]
    public void Validation_errors_of_the_plan_are_passed_through()
    {
        var mother = Mother(b => b.ParentalParts = [new LeavePart(33, D("2027-08-02"))]);
        var father = Father();

        var result = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15") with { ZdarzenieDlaKogo = F(CalculationScope.JaIDrugiRodzic) }, mother, father));

        Assert.Contains(result.ValidationErrors, v => v.Fact == FactKeys.PlanRodzicielskiCzesci && v.Person == PersonRef.Wnioskodawca);
    }

    [Fact]
    public void Default_pipeline_leaves_rate_and_amount_empty_with_a_note_asking_for_the_basis()
    {
        var result = Assess(Pregnancy(Mother()));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Null(benefit.Rate);
        Assert.Null(benefit.Amount);
        Assert.Contains(benefit.Notes, n => n.Contains("podaj podstawę, żeby zobaczyć kwotę", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(Get(result, BenefitId.Ojcowski).Notes, n => n.Contains("podstawę"));
    }

    private sealed class FakeBasis(IReadOnlyList<MissingFact>? missing = null) : IBasisStep
    {
        public BasisStepResult Resolve(BenefitContext context) =>
            new(new BasisResolution(8000m, BasisSource.SzacunekZus), ["Podstawa z danych ZUS."], missing ?? [], [new TraceStep("podstawa_testowa", "test", true, "podstawa")]);
    }

    private sealed class FakeAmounts : IAmountsStep
    {
        public int Calls { get; private set; }

        public BenefitResult Apply(BenefitContext context, BasisStepResult basis)
        {
            Calls++;
            var amount = new Amount(230.11m, 32215.40m, basis.Basis!.Amount, basis.Basis.Source);
            return context.Benefit with
            {
                Rate = 1m,
                Amount = amount,
                Notes = [.. context.Benefit.Notes, "Kwota brutto."],
                Deadlines = [.. context.Benefit.Deadlines, new Deadline("termin_z_kwot", DeadlineType.NajpozniejDo, D("2027-12-31"), "z kwot")],
            };
        }
    }

    [Fact]
    public void Basis_and_amounts_steps_are_pluggable_and_run_only_for_granted_benefits()
    {
        var amounts = new FakeAmounts();
        var engine = Engine(basis: new FakeBasis(), amounts: amounts);
        var uninsured = Person(ParentRole.Matka, null, null, b => b.Titles = []);

        var granted = Assess(Pregnancy(Mother()), engine: engine);
        var callsAfterGranted = amounts.Calls;
        Assess(Pregnancy(uninsured), engine: engine);

        var benefit = Get(granted, BenefitId.Macierzynski);
        Assert.Equal(1m, benefit.Rate);
        Assert.Equal(new Amount(230.11m, 32215.40m, 8000m, BasisSource.SzacunekZus), benefit.Amount);
        Assert.Contains("Podstawa z danych ZUS.", benefit.Notes);
        Assert.Contains("Kwota brutto.", benefit.Notes);
        Assert.DoesNotContain(benefit.Notes, n => n.Contains("podaj podstawę", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(benefit.Deadlines, d => d.Id == "termin_z_kwot");
        var rules = benefit.Trace.Select(t => t.Rule).ToList();
        Assert.True(rules.IndexOf("macierzynski_wymiar") < rules.IndexOf("podstawa_testowa"));
        Assert.Equal(callsAfterGranted, amounts.Calls);
        Assert.True(callsAfterGranted >= 2);
    }

    [Fact]
    public void Basis_step_can_ask_for_missing_facts()
    {
        var missing = new[] { FactQuestionBook.Ask(FactKeys.PodstawaUstalonaPrzezPlatnika, PersonRef.Wnioskodawca) };

        var result = Assess(Pregnancy(Mother()), engine: Engine(basis: new FakeBasis(missing), amounts: new FakeAmounts()));

        Assert.Contains(result.MissingFacts, m => m.Fact == FactKeys.PodstawaUstalonaPrzezPlatnika && m.Type == MissingFactType.Money);
    }

    [Fact]
    public void Single_person_request_has_no_second_parent_results_or_questions()
    {
        var result = Assess(Pregnancy(Mother()));

        Assert.All(result.Benefits, b => Assert.Equal(PersonRef.Wnioskodawca, b.Person));
        Assert.DoesNotContain(result.MissingFacts, m => m.Person == PersonRef.DrugiRodzic);
    }

    [Fact]
    public void Catalog_module_registers_the_engine_with_default_steps()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IParameterProvider>(Params());
        services.AddPeriods().AddEligibility().AddCatalog();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var engine = scope.ServiceProvider.GetRequiredService<IAssessmentEngine>();
        var result = engine.Assess(new AssessmentRequest(Pregnancy(Mother()), AsOf));

        Assert.Equal(10, result.Benefits.Count);
        Assert.IsAssignableFrom<ICatalogAssessmentEngine>(engine);
    }
}
