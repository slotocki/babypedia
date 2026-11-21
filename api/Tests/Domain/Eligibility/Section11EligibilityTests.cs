using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using static BabyRadnik.Tests.Domain.Eligibility.Kit;

namespace BabyRadnik.Tests.Domain.Eligibility;

/// <summary>Instrukcja, sekcja 11, punkty 16–23.</summary>
public sealed class Section11EligibilityTests
{
    private static readonly DateOnly Birth = D(2026, 3, 10);

    // 16. Brak ubezpieczenia w dniu porodu → odmowa (sprawa ewa).
    [Fact]
    public void T16_no_insurance_on_birth_day_is_refused()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Zleceniobiorca, D(2024, 1, 1), null, SicknessInsurance.Brak));
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf);

        var result = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("Brak ubezpieczenia chorobowego", result.Reason);
        Assert.Contains("art. 29 ust. 1", result.LegalBasis);
        Assert.NotNull(result.WhatWouldChange);
        Assert.Contains(result.Trace, t => t.Rule == InsuranceOnDateRule.RuleId && t.Met == false);
    }

    [Fact]
    public void T16_empty_title_list_and_ended_title_are_refused()
    {
        var empty = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), Person(ParentRole.Matka)), AsOf);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(empty, BenefitId.Macierzynski).Status);

        var ended = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2025, 12, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(InsuranceEndReason.Inna),
        };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), ended), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains(result.Trace, t => t.Rule == InsuranceEndExceptionRule.RuleId && t.Met == false);
    }

    [Fact]
    public void T16_ended_title_without_reason_asks_for_the_reason()
    {
        var ended = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1), D(2025, 12, 31)));
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), ended), AsOf);

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieUstaniePrzyczyna);
    }

    // 17. Działalność, zadłużenie 120,00 zł → odmowa, „spłać 120,00 zł do <data + 6 mies.>”; 48,06 → przysługuje; 48,07 → odmowa.
    [Fact]
    public void T17_business_debt_120_is_refused_with_amount_and_deadline()
    {
        var result = Get(Service().Assess(BusinessFacts(120.00m), D(2026, 5, 1)), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("120,00 zł", result.WhatWouldChange);
        Assert.Contains("2026-09-10", result.WhatWouldChange);
        Assert.Contains("10.09.2026", result.WhatWouldChange);
        Assert.Contains("48,06 zł", result.Reason);
        Assert.Contains("art. 2a", result.LegalBasis);
        Assert.Contains(result.Trace, t => t.Rule == ContributionDebtRule.RuleId && t.Met == false);
        var deadline = Assert.Single(result.Deadlines);
        Assert.Equal("splata_zadluzenia", deadline.Id);
        Assert.Equal(D(2026, 9, 10), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejDo, deadline.Type);
    }

    [Theory]
    [InlineData("48.06", BenefitStatus.Przysluguje)]
    [InlineData("48.07", BenefitStatus.NiePrzysluguje)]
    [InlineData("0", BenefitStatus.Przysluguje)]
    public void T17_debt_threshold_is_exclusive_at_48_06(string amount, BenefitStatus expected)
    {
        var result = Get(Service().Assess(BusinessFacts(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)), D(2026, 5, 1)), BenefitId.Macierzynski);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void T17_repayment_in_time_restores_the_right_and_late_repayment_does_not()
    {
        var inTime = BusinessFacts(120.00m, repaid: D(2026, 9, 10));
        Assert.Equal(BenefitStatus.Przysluguje, Get(Service().Assess(inTime, D(2026, 5, 1)), BenefitId.Macierzynski).Status);

        var late = BusinessFacts(120.00m, repaid: D(2026, 9, 11));
        var refused = Get(Service().Assess(late, D(2026, 11, 21)), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, refused.Status);
        Assert.Contains("przedawni", refused.Reason);
        Assert.Contains("2026-09-10", refused.WhatWouldChange);
    }

    [Fact]
    public void T17_missing_debt_balance_for_business_is_asked_and_employees_are_not_checked()
    {
        var business = Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, D(2020, 1, 1), null, SicknessInsurance.Dobrowolne));
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), business), D(2026, 5, 1));
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.SkladkiZadluzenie && m.Person == PersonRef.Wnioskodawca);

        var employee = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with
        {
            SkladkiZadluzenie = F(new ContributionDebt(Birth, 5000m)),
            PlatnikLiczbaUbezpieczonych = F(30),
        };
        Assert.Equal(BenefitStatus.Przysluguje, Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), employee), D(2026, 5, 1)), BenefitId.Macierzynski).Status);
    }

    [Fact]
    public void Debt_threshold_follows_the_parameter_valid_on_the_day_of_the_right()
    {
        var raised = Row(EligibilityParameterKeys.ProgZadluzenia, 60m, D(2027, 1, 1));
        var service = Service(raised);

        var in2026 = BusinessFacts(55m);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(service.Assess(in2026, D(2026, 5, 1)), BenefitId.Macierzynski).Status);

        var in2027 = Facts(
            Case(EventKind.Urodzenie, D(2027, 2, 1)),
            Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, D(2020, 1, 1), null, SicknessInsurance.Dobrowolne)) with
            {
                SkladkiZadluzenie = F(new ContributionDebt(D(2027, 2, 1), 55m)),
            });
        Assert.Equal(BenefitStatus.Przysluguje, Get(service.Assess(in2027, D(2027, 3, 1)), BenefitId.Macierzynski).Status);
    }

    // 18. Dobrowolne ubezpieczenie od 60 dni, e-ZLA z kodem B → chorobowy: odmowa (90 dni); macierzyński przysługuje.
    [Fact]
    public void T18_voluntary_insurance_for_60_days_refuses_sickness_but_maternity_is_due()
    {
        var firstDay = D(2026, 10, 31);
        var person = Person(ParentRole.Matka, Title(TitleKind.Zleceniobiorca, firstDay.AddDays(-60), null, SicknessInsurance.Dobrowolne)) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(10),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 3, 15)), person), AsOf);

        var sickness = Get(assessment, BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, sickness.Status);
        Assert.Contains("60 dni", sickness.Reason);
        Assert.Contains("90 dni", sickness.Reason);
        Assert.Contains("art. 4", sickness.LegalBasis);
        Assert.Contains("2026-11-30", sickness.WhatWouldChange);

        var maternity = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, maternity.Status);
        Assert.Equal(Payer.Zus, maternity.Payer);
        Assert.Contains(maternity.Documents, d => d.StartsWith("Z-3a", StringComparison.Ordinal));
    }

    [Fact]
    public void T18_exactly_90_days_of_voluntary_insurance_is_enough()
    {
        var firstDay = D(2026, 10, 31);
        var person = Person(ParentRole.Matka, Title(TitleKind.Zleceniobiorca, firstDay.AddDays(-90), null, SicknessInsurance.Dobrowolne)) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(10),
        };
        var sickness = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 3, 15)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.Przysluguje, sickness.Status);
        Assert.Equal(Payer.Zus, sickness.Payer);
    }

    // 19. Pracownica po 20 dniach bez wcześniejszych okresów → odmowa; z 10 latami wcześniejszego obowiązkowego → przysługuje.
    [Fact]
    public void T19_employee_with_20_days_is_refused_and_with_10_years_is_entitled()
    {
        var firstDay = D(2026, 10, 21);
        var baseline = Person(ParentRole.Matka, Title(TitleKind.Pracownik, firstDay.AddDays(-20))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(50),
            UbezpieczenieLatWczesniejszegoObowiazkowego = F(0m),
        };
        var facts = Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), baseline);

        var refused = Get(Service().Assess(facts, AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, refused.Status);
        Assert.Contains("20 dni", refused.Reason);
        Assert.Contains("30 dni", refused.Reason);
        Assert.Contains("2026-10-31", refused.WhatWouldChange);

        var withYears = Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), baseline with { UbezpieczenieLatWczesniejszegoObowiazkowego = F(10m) });
        var entitled = Get(Service().Assess(withYears, AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.Przysluguje, entitled.Status);
        Assert.Contains(entitled.Trace, t => t.Rule == WaitingPeriodExceptionRule.RuleId && t.Met == true);

        var nineYears = Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), baseline with { UbezpieczenieLatWczesniejszegoObowiazkowego = F(9.9m) });
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(nineYears, AsOf), BenefitId.ChorobowyWCiazy).Status);
    }

    [Fact]
    public void T19_exception_facts_are_asked_when_unknown_and_graduate_exception_works()
    {
        var firstDay = D(2026, 10, 21);
        var baseline = Person(ParentRole.Matka, Title(TitleKind.Pracownik, firstDay.AddDays(-20))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(50),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), baseline), AsOf);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.ChorobowyWCiazy).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieLatWczesniejszegoObowiazkowego);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.OsobaDataUkonczeniaSzkoly);

        var graduate = baseline with { OsobaDataUkonczeniaSzkoly = F(firstDay.AddDays(-20 - 90)) };
        Assert.Equal(BenefitStatus.Przysluguje, Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), graduate), AsOf), BenefitId.ChorobowyWCiazy).Status);

        var lateGraduate = baseline with { OsobaDataUkonczeniaSzkoly = F(firstDay.AddDays(-20 - 91)) };
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), lateGraduate), AsOf), BenefitId.ChorobowyWCiazy).Status);
    }

    [Theory]
    [InlineData(11, BenefitStatus.Przysluguje)]
    [InlineData(12, BenefitStatus.NiePrzysluguje)]
    public void Earlier_insurance_counts_only_when_the_gap_is_at_most_30_days(int secondTitleDayInOctober, BenefitStatus expected)
    {
        var firstDay = D(2026, 10, 21);
        var person = Person(
            ParentRole.Matka,
            Title(TitleKind.Pracownik, D(2026, 8, 1), D(2026, 9, 10)),
            Title(TitleKind.Pracownik, D(2026, 10, secondTitleDayInOctober), null)) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(50),
            UbezpieczenieLatWczesniejszegoObowiazkowego = F(0m),
        };

        var result = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 4, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(expected, result.Status);
        if (expected == BenefitStatus.NiePrzysluguje)
        {
            Assert.Contains("9 dni", result.Reason);
        }
        else
        {
            Assert.Contains(result.Trace, t => t.Rule == WaitingPeriodRule.RuleId && t.Text.Contains("51 dni", StringComparison.Ordinal));
        }
    }

    // 20. Własne wypowiedzenie w 5. miesiącu ciąży → macierzyński: odmowa. Likwidacja pracodawcy → przysługuje + zasiłek do dnia porodu.
    [Fact]
    public void T20_own_resignation_is_refused()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2022, 1, 1), D(2026, 8, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(InsuranceEndReason.WypowiedzeniePrzezPracownika),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 15)), person), AsOf);

        var maternity = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
        Assert.Contains("własnego wypowiedzenia", maternity.Reason);
        Assert.Contains("art. 30", maternity.LegalBasis);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.ZasilekDoDniaPorodu).Status);
        Assert.Contains(assessment.Risks, r => r.Id == "koniec_ubezpieczenia_przed_porodem");
    }

    [Theory]
    [InlineData(InsuranceEndReason.LikwidacjaPracodawcy)]
    [InlineData(InsuranceEndReason.UpadloscPracodawcy)]
    [InlineData(InsuranceEndReason.SmiercPracodawcy)]
    public void T20_employer_liquidation_bankruptcy_or_death_gives_maternity_and_benefit_until_birth(InsuranceEndReason reason)
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2022, 1, 1), D(2026, 8, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(reason),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 15)), person), AsOf);

        var maternity = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, maternity.Status);
        Assert.Equal(Payer.Zus, maternity.Payer);
        Assert.Equal(ApplicationPlace.Zus, maternity.WhereToApply);
        Assert.Contains(maternity.Trace, t => t.Rule == InsuranceEndExceptionRule.RuleId && t.Met == true);

        var untilBirth = Get(assessment, BenefitId.ZasilekDoDniaPorodu);
        Assert.Equal(BenefitStatus.Przysluguje, untilBirth.Status);
        Assert.Contains("art. 30 ust. 3", untilBirth.Trace.Single(t => t.Rule == BenefitUntilBirthRule.RuleId).Basis);
    }

    [Fact]
    public void T20_court_ruling_gives_maternity_but_not_the_benefit_until_birth()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2022, 1, 1), D(2026, 8, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(InsuranceEndReason.NaruszeniePrawaOrzeczonePrzezSad),
        };
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 15)), person), AsOf);

        Assert.Equal(BenefitStatus.Przysluguje, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.ZasilekDoDniaPorodu).Status);
    }

    [Theory]
    [InlineData(InsuranceEndReason.WypowiedzeniePrzezPracodawce)]
    [InlineData(InsuranceEndReason.PorozumienieStron)]
    [InlineData(InsuranceEndReason.Inna)]
    public void T20_other_end_reasons_are_not_exceptions(InsuranceEndReason reason)
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2022, 1, 1), D(2026, 8, 31))) with
        {
            UbezpieczenieUstaniePrzyczyna = F(reason),
        };
        var maternity = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 1, 15)), person), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
    }

    // 21. Umowa na czas określony kończąca się po 3. miesiącu ciąży → przedłużenie do dnia porodu, macierzyński przysługuje.
    [Fact]
    public void T21_fixed_term_contract_ending_after_third_month_is_extended_to_birth()
    {
        var expectedBirth = D(2027, 2, 1);
        var person = FixedTermMother(D(2026, 9, 30));
        var assessment = Service().Assess(Facts(Case(EventKind.Ciaza, expectedBirth, weekAtBirth: 40), person), AsOf);

        var maternity = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, maternity.Status);
        Assert.Equal(Payer.Zus, maternity.Payer);
        var step = maternity.Trace.Single(t => t.Rule == InsuranceEndExceptionRule.RuleId);
        Assert.True(step.Met);
        Assert.Contains("KP art. 177 § 3", step.Basis);
        Assert.Contains("2026-07-27", step.Text);
    }

    [Fact]
    public void T21_fixed_term_contract_ending_before_end_of_third_month_is_not_extended()
    {
        var person = FixedTermMother(D(2026, 7, 27));
        var maternity = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1), weekAtBirth: 40), person), AsOf), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
        Assert.Contains("trzeciego miesiąca ciąży", maternity.Reason);
        Assert.Contains("177 § 3", maternity.LegalBasis);

        var oneDayLater = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1), weekAtBirth: 40), FixedTermMother(D(2026, 7, 28))), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, oneDayLater.Status);
    }

    [Fact]
    public void T21_uses_the_pregnancy_week_from_the_facts_and_asks_for_it_when_nothing_is_known()
    {
        var premature = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, D(2027, 2, 1), weekAtBirth: 30), FixedTermMother(D(2026, 9, 30))), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, premature.Status);

        var seedOnly = new EligibilityService(SeedOnly());
        var assessment = seedOnly.Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1)), FixedTermMother(D(2026, 9, 30))), AsOf);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoTydzienCiazyPrzyPorodzie);
    }

    [Fact]
    public void T21_without_the_statutory_parameter_the_engine_does_not_decide()
    {
        var seedOnly = new EligibilityService(SeedOnly());
        var assessment = seedOnly.Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1), weekAtBirth: 40), FixedTermMother(D(2026, 9, 30))), AsOf);

        var maternity = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NieRozstrzygam, maternity.Status);
        Assert.Contains("ciaza_umowa_przedluzenie_miesiace", maternity.Reason);
    }

    [Fact]
    public void T21_contract_not_fixed_term_is_not_extended()
    {
        var person = FixedTermMother(D(2026, 9, 30)) with { UmowaNaCzasOkreslony = F(false) };
        var maternity = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 2, 1), weekAtBirth: 40), person), AsOf), BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
    }

    // 22. Matka nieubezpieczona, ojciec ubezpieczony → rodzicielski i ojcowski dla ojca przysługują.
    [Fact]
    public void T22_uninsured_mother_insured_father_gets_parental_and_paternity()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(40) };
        var mother = new PersonFacts { OsobaRola = F(ParentRole.Matka), UbezpieczeniePodlegaChorobowemu = F(false) };
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, mother), AsOf);

        foreach (var id in new[] { BenefitId.Rodzicielski, BenefitId.Ojcowski })
        {
            var result = Get(assessment, id);
            Assert.Equal(BenefitStatus.Przysluguje, result.Status);
            Assert.Equal(Payer.PlatnikSkladek, result.Payer);
            Assert.Contains(result.Trace, t => t.Rule == "matka_nieubezpieczona_ojciec_ubezpieczony");
        }

        var motherMaternity = Get(assessment, BenefitId.Macierzynski, PersonRef.DrugiRodzic);
        Assert.Equal(BenefitStatus.NiePrzysluguje, motherMaternity.Status);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.Ojcowski, PersonRef.DrugiRodzic).Status);
    }

    [Fact]
    public void T22_uninsured_father_gets_neither()
    {
        var father = Person(ParentRole.Ojciec, Title(TitleKind.Zleceniobiorca, D(2020, 1, 1), null, SicknessInsurance.Brak));
        var mother = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(40) };
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, mother), AsOf);

        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Rodzicielski).Status);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Ojcowski).Status);
        Assert.Equal(BenefitStatus.Przysluguje, Get(assessment, BenefitId.Rodzicielski, PersonRef.DrugiRodzic).Status);
    }

    [Fact]
    public void T22_father_with_declaration_only_is_assessed_with_a_warning_note()
    {
        var father = new PersonFacts { OsobaRola = F(ParentRole.Ojciec), UbezpieczeniePodlegaChorobowemu = F(true) };
        var mother = new PersonFacts { OsobaRola = F(ParentRole.Matka), UbezpieczeniePodlegaChorobowemu = F(false) };
        var result = Get(Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), father, mother), AsOf), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Contains(result.Notes, n => n.Contains("deklaracji", StringComparison.Ordinal));
    }

    // 23. Płatnik 150 osób → wypłaca płatnik; 15 osób → ZUS + Z-3; działalność → ZUS.
    [Fact]
    public void T23_payer_with_150_insured_pays_the_benefit_and_applications_go_to_the_payer()
    {
        var result = Get(Service().Assess(EmployeeFacts(150), AsOf), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(Payer.PlatnikSkladek, result.Payer);
        Assert.Equal(ApplicationPlace.PlatnikSkladek, result.WhereToApply);
        Assert.Contains(result.Documents, d => d.StartsWith("ZAM", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Documents, d => d.StartsWith("Z-3", StringComparison.Ordinal));
        Assert.Contains(result.Trace, t => t.Rule == PaymentRouteRule.RuleId && t.Met == true && t.Basis.Contains("art. 61", StringComparison.Ordinal));
    }

    [Fact]
    public void T23_payer_with_15_insured_means_zus_pays_and_employer_forwards_z3()
    {
        var result = Get(Service().Assess(EmployeeFacts(15), AsOf), BenefitId.Macierzynski);

        Assert.Equal(Payer.Zus, result.Payer);
        Assert.Equal(ApplicationPlace.Pracodawca, result.WhereToApply);
        Assert.Contains(result.Documents, d => d.StartsWith("Z-3 ", StringComparison.Ordinal));
        Assert.Contains(result.Documents, d => d.StartsWith("ZAM", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(20, Payer.Zus)]
    [InlineData(21, Payer.PlatnikSkladek)]
    public void T23_threshold_is_more_than_20_insured(int headcount, Payer expected)
    {
        Assert.Equal(expected, Get(Service().Assess(EmployeeFacts(headcount), AsOf), BenefitId.Macierzynski).Payer);
    }

    [Fact]
    public void T23_business_is_always_paid_by_zus_and_applies_to_zus()
    {
        var result = Get(Service().Assess(BusinessFacts(0m), D(2026, 5, 1)), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(Payer.Zus, result.Payer);
        Assert.Equal(ApplicationPlace.Zus, result.WhereToApply);
        Assert.Contains(result.Documents, d => d.StartsWith("ZAM", StringComparison.Ordinal));
    }

    [Fact]
    public void T23_headcount_is_asked_when_unknown_and_status_is_not_blocked()
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1)));
        var assessment = Service().Assess(Facts(Case(EventKind.Urodzenie, Birth), person), AsOf);

        var result = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Null(result.Payer);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.PlatnikLiczbaUbezpieczonych);
    }

    [Fact]
    public void T23_sickness_by_business_person_names_zas53_and_z3b_is_recognized_but_never_proposed()
    {
        var firstDay = D(2026, 10, 1);
        var person = Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, D(2025, 1, 1), null, SicknessInsurance.Dobrowolne)) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            SkladkiZadluzenie = F(new ContributionDebt(firstDay, 0m)),
        };
        var result = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 3, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(Payer.Zus, result.Payer);
        Assert.Contains(result.Documents, d => d.StartsWith("ZAS-53 — wniosek o zasiłek chorobowy", StringComparison.Ordinal));
        Assert.Contains(result.Documents, d => d.StartsWith("ZAS-12", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Documents, d => d.Contains("Z-3b", StringComparison.Ordinal));
        Assert.True(ZusForms.Recognize("Z-3b").Recognized);
        Assert.False(ZusForms.Recognize("Z-3b").ProposedToUser);
        Assert.True(ZusForms.Recognize("Z-3").ProposedToUser);
        Assert.True(ZusForms.Recognize("ZAS-12").ProposedToUser);
    }

    [Theory]
    [InlineData(TitleKind.Pracownik, SicknessInsurance.Obowiazkowe, "Z-3 ", "zamiast kolejnego Z-3")]
    [InlineData(TitleKind.Zleceniobiorca, SicknessInsurance.Dobrowolne, "Z-3a", "zamiast kolejnego Z-3a")]
    public void T23_sickness_paid_by_zus_lists_zas12_for_a_further_period(TitleKind kind, SicknessInsurance sickness, string firstForm, string zas12Text)
    {
        var firstDay = D(2026, 10, 1);
        var person = Person(ParentRole.Matka, Title(kind, D(2024, 1, 1), null, sickness)) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(5),
            OsobaDataUrodzenia = F(D(1990, 1, 1)),
        };
        var result = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 3, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);

        Assert.Equal(Payer.Zus, result.Payer);
        Assert.Contains(result.Documents, d => d.StartsWith(firstForm, StringComparison.Ordinal));
        Assert.Contains(result.Documents, d => d.StartsWith("ZAS-12", StringComparison.Ordinal) && d.Contains(zas12Text, StringComparison.Ordinal));
    }

    [Fact]
    public void T23_zas12_is_not_listed_when_payer_pays_or_for_maternity()
    {
        Assert.DoesNotContain(Get(Service().Assess(EmployeeFacts(15), AsOf), BenefitId.Macierzynski).Documents, d => d.Contains("ZAS-12", StringComparison.Ordinal));

        var firstDay = D(2026, 10, 1);
        var person = Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2024, 1, 1))) with
        {
            EzlaZwolnienia = F<IReadOnlyList<SickLeave>>([Leave(firstDay, D(2026, 11, 20))]),
            PlatnikLiczbaUbezpieczonych = F(40),
            OsobaDataUrodzenia = F(D(1990, 1, 1)),
        };
        var sickness = Get(Service().Assess(Facts(Case(EventKind.Ciaza, D(2027, 3, 1)), person), AsOf), BenefitId.ChorobowyWCiazy);
        Assert.Equal(Payer.PlatnikSkladek, sickness.Payer);
        Assert.DoesNotContain(sickness.Documents, d => d.Contains("ZAS-12", StringComparison.Ordinal));
    }

    private static FactSet BusinessFacts(decimal debt, DateOnly? repaid = null)
    {
        var person = Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, D(2020, 1, 1), null, SicknessInsurance.Dobrowolne)) with
        {
            SkladkiZadluzenie = F(new ContributionDebt(Birth, debt)),
            SkladkiDataSplaty = repaid is { } r ? F(r) : null,
        };
        return Facts(Case(EventKind.Urodzenie, Birth), person);
    }

    private static FactSet EmployeeFacts(int headcount) =>
        Facts(
            Case(EventKind.Urodzenie, Birth),
            Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2020, 1, 1))) with { PlatnikLiczbaUbezpieczonych = F(headcount) });

    private static PersonFacts FixedTermMother(DateOnly contractEnd) =>
        Person(ParentRole.Matka, Title(TitleKind.Pracownik, D(2025, 1, 1), contractEnd)) with
        {
            UbezpieczenieUstaniePrzyczyna = F(InsuranceEndReason.KoniecUmowyNaCzasOkreslony),
            UmowaNaCzasOkreslony = F(true),
            UmowaDataKonca = F(contractEnd),
        };
}
