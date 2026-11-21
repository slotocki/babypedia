using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using Microsoft.Extensions.DependencyInjection;
using static BabyRadnik.Tests.Domain.Eligibility.Kit;

namespace BabyRadnik.Tests.Domain.Eligibility;

public sealed class EligibilityRuleTests
{
    private static readonly DateOnly Birth = D(2026, 3, 10);

    [Fact]
    public void Title_features_follow_kind_and_sickness_field_not_the_code()
    {
        var employee = TitleFeatures.Of(new InsuranceTitle(TitleKind.Pracownik, Birth, null, SicknessInsurance.Obowiazkowe, KodTytulu: "9999"));
        Assert.True(employee.WagesFromEmployerBeforeBenefit);
        Assert.True(employee.LeaveApplicationToEmployer);
        Assert.True(employee.EmploymentProtection);
        Assert.False(employee.DebtControl);
        Assert.Equal(EligibilityParameterKeys.WyczekiwanieObowiazkoweDni, employee.WaitingPeriodKey);

        var contractor = TitleFeatures.Of(Title(TitleKind.Zleceniobiorca, Birth, null, SicknessInsurance.Dobrowolne));
        Assert.False(contractor.WagesFromEmployerBeforeBenefit);
        Assert.False(contractor.LeaveApplicationToEmployer);
        Assert.Equal(EligibilityParameterKeys.WyczekiwanieDobrowolneDni, contractor.WaitingPeriodKey);

        foreach (var kind in new[] { TitleKind.Dzialalnosc, TitleKind.OsobaWspolpracujaca })
        {
            var business = TitleFeatures.Of(Title(kind, Birth, null, SicknessInsurance.Dobrowolne));
            Assert.True(business.DebtControl);
            Assert.False(business.EmploymentProtection);
        }

        var none = TitleFeatures.Of(Title(TitleKind.Dzialalnosc, Birth, null, SicknessInsurance.Brak));
        Assert.False(none.HasSicknessInsurance);
        Assert.Null(none.WaitingPeriodKey);
    }

    [Fact]
    public void Concurrent_titles_are_not_decided()
    {
        var person = Person(
            ParentRole.Matka,
            Title(TitleKind.Pracownik, D(2020, 1, 1)),
            Title(TitleKind.Zleceniobiorca, D(2021, 1, 1), null, SicknessInsurance.Dobrowolne));
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
        Assert.Contains("zbieg tytułów", result.Reason);
        Assert.Contains(result.Links, l => l.Url.Contains("zus.pl", StringComparison.Ordinal));
    }

    [Fact]
    public void Employment_next_to_business_without_sickness_insurance_is_not_decided()
    {
        var person = Person(
            ParentRole.Matka,
            Title(TitleKind.Pracownik, D(2020, 1, 1)),
            Title(TitleKind.Dzialalnosc, D(2021, 1, 1), null, SicknessInsurance.Brak));
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
    }

    [Fact]
    public void Missing_parameter_means_undecided_never_a_guess()
    {
        var empty = new EligibilityService(SeedParameterProviderWithout(EligibilityParameterKeys.ProgZadluzenia));
        var person = Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, D(2020, 1, 1), null, SicknessInsurance.Dobrowolne)) with
        {
            SkladkiZadluzenie = F(new ContributionDebt(Birth, 500m)),
        };
        var result = Get(empty.Assess(Facts(Case(EventKind.Urodzenie, Birth), person), D(2026, 5, 1)), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
        Assert.Contains(EligibilityParameterKeys.ProgZadluzenia, result.Reason);
    }

    [Fact]
    public void Missing_event_and_role_are_asked()
    {
        var noEvent = Service().Assess(Facts(new CaseFacts(), Person(ParentRole.Matka)), AsOf);
        Assert.All(noEvent.Benefits, b => Assert.Equal(BenefitStatus.BrakujeDanych, b.Status));
        Assert.Contains(noEvent.MissingFacts, m => m.Fact == FactKeys.ZdarzenieRodzaj);

        var noDate = Service().Assess(
            Facts(new CaseFacts { ZdarzenieRodzaj = F(EventKind.Urodzenie) }, Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1)))),
            AsOf);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(noDate, BenefitId.Macierzynski).Status);
        Assert.Contains(noDate.MissingFacts, m => m.Fact == FactKeys.ZdarzenieData);

        var noRole = Service().Assess(
            Facts(Case(EventKind.Urodzenie, Birth), new PersonFacts { UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>([Title(TitleKind.Pracownik, D(2020, 1, 1))]) }),
            AsOf);
        Assert.Contains(noRole.MissingFacts, m => m.Fact == FactKeys.OsobaRola);
    }

    [Fact]
    public void No_title_data_at_all_asks_for_titles()
    {
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), new PersonFacts { OsobaRola = F(ParentRole.Matka) }), AsOf);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieTytuly);
    }

    [Fact]
    public void Adoption_uses_insurance_on_the_adoption_day_for_both_parents()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var assessment = Service().Assess(Facts(Case(EventKind.Przyjecie, Birth), father), AsOf);

        var adoption = Get(assessment, BenefitId.PrzyjecieDziecka);
        Assert.Equal(BenefitStatus.Przysluguje, adoption.Status);
        Assert.Equal(Payer.Zus, adoption.Payer);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.Macierzynski).Status);

        var notInsured = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 1, 31)));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(Facts(Case(EventKind.Przyjecie, Birth), notInsured), AsOf), BenefitId.PrzyjecieDziecka).Status);
    }

    [Theory]
    [InlineData(14, BenefitStatus.Przysluguje)]
    [InlineData(20, BenefitStatus.Przysluguje)]
    [InlineData(13, BenefitStatus.NiePrzysluguje)]
    public void Mother_resignation_must_come_after_at_least_14_weeks(int weeks, BenefitStatus expected)
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var mother = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(weeks) };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, mother), AsOf), BenefitId.PrzejeciePrzezOjca);

        Assert.Equal(expected, result.Status);
        Assert.Contains(result.Trace, t => t.Rule == FatherTakeoverRule.RuleId);
        if (expected == BenefitStatus.NiePrzysluguje)
        {
            Assert.Contains("14", result.Reason);
        }
    }

    [Fact]
    public void Takeover_is_not_applicable_without_a_trigger_and_uninsured_mother_cannot_resign()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var mother = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1)));
        Assert.Equal(BenefitStatus.NieDotyczy, Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, mother), AsOf), BenefitId.PrzejeciePrzezOjca).Status);

        var uninsuredMother = Person(ParentRole.Matka) with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(20) };
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, uninsuredMother), AsOf), BenefitId.PrzejeciePrzezOjca).Status);
    }

    [Theory]
    [InlineData(MotherSituationKind.Zgon, true, BenefitStatus.Przysluguje)]
    [InlineData(MotherSituationKind.PorzucenieDziecka, true, BenefitStatus.Przysluguje)]
    [InlineData(MotherSituationKind.Zgon, false, BenefitStatus.Przysluguje)]
    [InlineData(MotherSituationKind.NiezdolnoscDoSamodzielnejEgzystencji, false, BenefitStatus.Przysluguje)]
    [InlineData(MotherSituationKind.PobytWSzpitalu, false, BenefitStatus.NieRozstrzygam)]
    public void Special_mother_situations_follow_art_29(MotherSituationKind kind, bool motherInsured, BenefitStatus expected)
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var mother = motherInsured ? Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) : Person(ParentRole.Matka);
        var sprawa = Case(EventKind.Urodzenie, Birth) with { MatkaSytuacjaSzczegolna = F(new MotherSituation(kind, Birth.AddDays(30))) };
        var result = Get(Service().Assess(Facts(sprawa, father, mother), AsOf), BenefitId.PrzejeciePrzezOjca);

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void Hospital_stay_of_insured_mother_must_reach_beyond_8_weeks_after_birth()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var mother = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1)));

        var early = Case(EventKind.Urodzenie, Birth) with
        {
            MatkaSytuacjaSzczegolna = F(new MotherSituation(MotherSituationKind.PobytWSzpitalu, Birth.AddDays(10), Birth.AddDays(55))),
        };
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(Facts(early, father, mother), AsOf), BenefitId.PrzejeciePrzezOjca).Status);

        var overlapping = Case(EventKind.Urodzenie, Birth) with
        {
            MatkaSytuacjaSzczegolna = F(new MotherSituation(MotherSituationKind.PobytWSzpitalu, Birth.AddDays(40), Birth.AddDays(70))),
        };
        var result = Get(Service().Assess(Facts(overlapping, father, mother), AsOf), BenefitId.PrzejeciePrzezOjca);
        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Contains(result.Notes, n => n.Contains("8 tygodniach", StringComparison.Ordinal));
    }

    [Fact]
    public void Disability_of_insured_mother_needs_resignation_after_8_weeks()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var sprawa = Case(EventKind.Urodzenie, Birth) with
        {
            MatkaSytuacjaSzczegolna = F(new MotherSituation(MotherSituationKind.NiezdolnoscDoSamodzielnejEgzystencji, Birth)),
        };

        var without = Service().Assess(Facts(sprawa, father, Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1)))), AsOf);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(without, BenefitId.PrzejeciePrzezOjca).Status);
        Assert.Contains(without.MissingFacts, m => m.Fact == FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg && m.Person == PersonRef.DrugiRodzic);

        PersonFacts Mother(int weeks) => Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(weeks) };
        Assert.Equal(BenefitStatus.Przysluguje, Get(Service().Assess(Facts(sprawa, father, Mother(8)), AsOf), BenefitId.PrzejeciePrzezOjca).Status);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(Facts(sprawa, father, Mother(7)), AsOf), BenefitId.PrzejeciePrzezOjca).Status);
    }

    [Fact]
    public void Business_father_with_debt_loses_paternity_benefit()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Dzialalnosc, D(2020, 1, 1), null, SicknessInsurance.Dobrowolne)) with
        {
            SkladkiZadluzenie = F(new ContributionDebt(Birth, 1000m)),
        };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father), D(2026, 5, 1)), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("1000,00 zł", result.WhatWouldChange);
    }

    [Fact]
    public void Paternity_uses_the_planned_leave_start_for_insurance()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 4, 30))) with
        {
            PlatnikLiczbaUbezpieczonych = F(5),
            PlanOjcowskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(1, D(2026, 6, 1))]),
        };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father), AsOf), BenefitId.Ojcowski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("rozpoczęcia urlopu", result.Reason);
    }

    [Fact]
    public void Father_insured_only_after_birth_keeps_parental_and_paternity_right_without_plan()
    {
        // Art. 29 ust. 1 wymaga ubezpieczenia w dniu porodu od matki; ojciec ma być ubezpieczony, gdy korzysta z urlopu.
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2026, 6, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father), AsOf);

        foreach (var id in new[] { BenefitId.Rodzicielski, BenefitId.Ojcowski })
        {
            var result = Get(assessment, id);
            Assert.Equal(BenefitStatus.Przysluguje, result.Status);
            Assert.Contains(result.Notes, n => n.Contains("w dniu oceny", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Father_without_insurance_on_assessment_day_is_denied_with_that_day_not_birth_day()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 9, 30))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father), AsOf), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("w dniu oceny", result.Reason);
        Assert.DoesNotContain("porodu", result.Reason);
    }

    [Fact]
    public void Father_before_birth_without_plan_is_checked_on_birth_day()
    {
        var birth = D(2027, 2, 1);
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 12, 31))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var result = Get(Service().Assess(Facts(Case(EventKind.Ciaza, birth), father), AsOf), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains(result.Notes, n => n.Contains("najwcześniejszym możliwym początku urlopu", StringComparison.Ordinal));
    }

    [Fact]
    public void Sickness_period_of_270_days_is_exhausted_and_rehabilitation_is_not_decided()
    {
        var firstDay = D(2026, 1, 1);
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 5, 31)), Leave(D(2026, 6, 1), D(2026, 10, 31))]),
            PlatnikLiczbaUbezpieczonych = F(5),
            OsobaDataUrodzenia = F(D(1990, 1, 1)),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 20)), person), AsOf);

        var sickness = Get(assessment, BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, sickness.Status);
        Assert.Contains("270", sickness.Reason);

        var rehab = Get(assessment, BenefitId.Rehabilitacyjne);
        Assert.Equal(BenefitStatus.NieRozstrzygam, rehab.Status);
        Assert.Contains("lekarza orzecznika", rehab.Reason);
    }

    [Fact]
    public void Sickness_period_within_270_days_is_open_and_rehabilitation_not_applicable()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(D(2026, 9, 1), D(2026, 11, 30))]),
            PlatnikLiczbaUbezpieczonych = F(5),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 20)), person), AsOf);

        var sickness = Get(assessment, BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.Przysluguje, sickness.Status);
        Assert.Contains(sickness.Trace, t => t.Rule == SicknessPeriodRule.RuleId && t.Text.Contains("82 z 270", StringComparison.Ordinal));
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.Rehabilitacyjne).Status);
        Assert.Contains(assessment.Risks, r => r.Id == "praca_w_czasie_zwolnienia");
    }

    [Theory]
    [InlineData(1976, 12, 31, "33")]
    [InlineData(1976, 10, 22, "33")]
    [InlineData(1976, 10, 21, "14")]
    [InlineData(1960, 1, 1, "14")]
    public void Employee_wages_limit_depends_on_age_50_on_first_day(int y, int m, int d, string limit)
    {
        var firstDay = D(2026, 10, 21);
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with
        {
            OsobaDataUrodzenia = F(D(y, m, d)),
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(5),
        };
        var step = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), person), AsOf), BenefitId.ChorobowyWCiazy)
            .Trace.Single(t => t.Rule == "wynagrodzenie_chorobowe_pracownika");

        Assert.Contains($"limit {limit} dni", step.Text);
    }

    [Fact]
    public void Wages_already_used_this_year_reduce_the_employer_period_and_missing_birth_date_is_asked()
    {
        var firstDay = D(2026, 10, 21);
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with
        {
            OsobaDataUrodzenia = F(D(1990, 1, 1)),
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(5),
            UbezpieczeniePobraneSwiadczenia = F<IReadOnlyList<ReceivedBenefit>>(
                [new ReceivedBenefit(ReceivedBenefitKind.WynagrodzenieChorobowe, D(2026, 3, 1), D(2026, 3, 10))]),
        };
        var step = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), person), AsOf), BenefitId.ChorobowyWCiazy)
            .Trace.Single(t => t.Rule == "wynagrodzenie_chorobowe_pracownika");
        Assert.Contains("pierwsze 23 dni", step.Text);

        var noBirth = person with { OsobaDataUrodzenia = null };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), noBirth), AsOf);
        Assert.Equal(BenefitStatus.Przysluguje, Get(assessment, BenefitId.ChorobowyWCiazy).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.OsobaDataUrodzenia);
    }

    [Fact]
    public void Sickness_after_insurance_ended_is_never_guessed()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 9, 30))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(D(2026, 10, 5), D(2026, 11, 20))]),
        };
        var sickness = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NieRozstrzygam, sickness.Status);
        Assert.Contains("art. 13", sickness.Reason);
        Assert.Contains("art. 7", sickness.LegalBasis);

        var late = person with { EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(D(2027, 1, 15), D(2027, 2, 20))]) };
        var refused = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), late), D(2027, 3, 1)), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, refused.Status);
    }

    [Fact]
    public void Sickness_starting_during_insurance_but_lasting_past_its_end_is_noted()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2026, 11, 10))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(D(2026, 10, 20), D(2026, 11, 30))]),
            PlatnikLiczbaUbezpieczonych = F(5),
            OsobaDataUrodzenia = F(D(1990, 1, 1)),
        };
        var sickness = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.Przysluguje, sickness.Status);
        Assert.Contains(sickness.Notes, n => n.Contains("art. 13", StringComparison.Ordinal));
    }

    [Fact]
    public void Sickness_without_any_leave_is_assessed_hypothetically_only_while_pregnant()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };

        var pregnant = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.Przysluguje, pregnant.Status);
        Assert.Contains(pregnant.Notes, n => n.Contains("dniu oceny", StringComparison.Ordinal));

        var born = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NieDotyczy, born.Status);
    }

    [Fact]
    public void Every_refusal_has_a_one_sentence_reason_and_a_legal_basis_and_every_step_has_text()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2022, 1, 1), D(2026, 8, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(InsuranceEndReason.WypowiedzeniePrzezPracownika),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 15)), person), AsOf);

        foreach (var refused in assessment.Benefits.Where(b => b.Status == BenefitStatus.NiePrzysluguje))
        {
            Assert.False(string.IsNullOrWhiteSpace(refused.Reason));
            Assert.False(string.IsNullOrWhiteSpace(refused.LegalBasis));
            Assert.All(refused.Trace, t =>
            {
                Assert.False(string.IsNullOrWhiteSpace(t.Rule));
                Assert.False(string.IsNullOrWhiteSpace(t.Basis));
                Assert.False(string.IsNullOrWhiteSpace(t.Text));
            });
        }
    }

    [Fact]
    public void Evaluate_returns_the_basic_right_for_the_supplementary_maternity_benefit()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(5) };
        var evaluation = Service().Evaluate(BenefitId.MacierzynskiUzupelniajacy, Facts(Case(EventKind.Urodzenie, Birth), person), PersonRef.Wnioskodawca, AsOf);

        Assert.Equal(BenefitId.MacierzynskiUzupelniajacy, evaluation.Result.Id);
        Assert.Equal(BenefitStatus.Przysluguje, evaluation.Result.Status);
    }

    [Fact]
    public void Risks_report_missing_sickness_insurance_and_debt_neutrally()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Zleceniobiorca, D(2024, 1, 1), null, SicknessInsurance.Brak)) with
        {
            SkladkiZadluzenie = F(new ContributionDebt(Birth, 100m)),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf);

        Assert.Contains(assessment.Risks, r => r.Id == "brak_dobrowolnego_chorobowego");
        Assert.Contains(assessment.Risks, r => r.Id == "zadluzenie_skladkowe" && r.Text.Contains("100,00 zł", StringComparison.Ordinal));
    }

    [Fact]
    public void Service_is_registered_by_AddEligibility()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Params());
        services.AddEligibility();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEligibilityService>());
    }

    private static IParameterProvider SeedParameterProviderWithout(string key) => new WithoutKey(Params(), key);

    private sealed class WithoutKey(IParameterProvider inner, string key) : IParameterProvider
    {
        public Parameter Get(string requested, DateOnly date) =>
            requested == key ? throw new ParameterNotFoundException(requested, date) : inner.Get(requested, date);
    }
}
