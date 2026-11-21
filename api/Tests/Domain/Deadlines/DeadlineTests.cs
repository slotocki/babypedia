using System.Text.Json;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Tests.Domain.Catalog;
using BabyRadnik.Tests.Reference;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Deadlines;

public sealed class DeadlineTests
{
    private static FactSet Pregnancy(PersonFacts mother, PersonFacts? other = null, string date = "2027-03-15", EventKind kind = EventKind.Ciaza) =>
        Facts(Case(kind, date), mother, other);

    private static PersonFacts Mother(Action<PersonFactsBuilder>? configure = null, InsuranceTitle? title = null) =>
        Person(ParentRole.Matka, title ?? Title(), 150, configure);

    private static PersonFacts Father(Action<PersonFactsBuilder>? configure = null) =>
        Person(ParentRole.Ojciec, Title(), 150, configure);

    private static InsuranceTitle Contract() => Title(TitleKind.Zleceniobiorca, sickness: SicknessInsurance.Dobrowolne);

    private static Deadline Find(AssessmentResult result, BenefitId benefit, string id, PersonRef person = PersonRef.Wnioskodawca) =>
        Get(result, benefit, person).Deadlines.Single(d => d.Id == id);

    private static bool Has(AssessmentResult result, BenefitId benefit, string id, PersonRef person = PersonRef.Wnioskodawca) =>
        Get(result, benefit, person).Deadlines.Any(d => d.Id == id);

    private static CaseFacts AdoptionCase(string takenOn) => new()
    {
        ZdarzenieRodzaj = F(EventKind.Przyjecie),
        ZdarzenieData = F(D(takenOn)),
        ZdarzenieDlaKogo = F(CalculationScope.Ja),
        DzieckoLiczba = F(1),
        DzieckoDataUrodzenia = F(D("2022-05-01")),
        PrzyjecieSciezka = F(AdoptionPath.Przysposobienie),
        PrzyjecieWniosekDoSadu = F(true),
    };

    [Fact]
    public void Every_rule_has_unique_id_legal_basis_and_benefit()
    {
        var rules = DeadlineService.Rules;

        Assert.Equal(rules.Count, rules.Select(r => r.Id).Distinct().Count());
        Assert.All(rules, r =>
        {
            Assert.Matches("^[a-z0-9_]+$", r.Id);
            Assert.False(string.IsNullOrWhiteSpace(r.PodstawaPrawna));
            Assert.NotEmpty(r.Benefits);
        });
    }

    [Fact]
    public void Maternity_before_birth_request_is_14_days_before_the_leave_starts()
    {
        var result = Assess(Pregnancy(Mother(b => b.PlanBeforeBirthWeeks = 4)));

        var deadline = Find(result, BenefitId.Macierzynski, "wniosek_macierzynski_przed_porodem");
        Assert.Equal(D("2027-02-01"), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejPrzed, deadline.Type);
        Assert.Equal("Poradnik ZUS 2026 s. 27", deadline.Basis);
        Assert.Equal("Złóż u pracodawcy wniosek o urlop macierzyński przed porodem najpóźniej 1 lutego 2027 (14 dni przed jego początkiem 15 lutego 2027).", deadline.Text);
        Assert.Equal("urlop macierzyński zacznie się później", deadline.Consequence);
    }

    [Fact]
    public void Maternity_before_birth_request_needs_a_pre_birth_start_and_an_employee()
    {
        var noPlan = Assess(Pregnancy(Mother()));
        var contractor = Assess(Pregnancy(Mother(b => b.PlanBeforeBirthWeeks = 4, Contract())));

        Assert.False(Has(noPlan, BenefitId.Macierzynski, "wniosek_macierzynski_przed_porodem"));
        Assert.False(Has(contractor, BenefitId.Macierzynski, "wniosek_macierzynski_przed_porodem"));
    }

    [Fact]
    public void Averaged_variant_request_is_21_days_after_birth_and_spelled_out_in_words()
    {
        var result = Assess(Pregnancy(Mother()));

        var deadline = Find(result, BenefitId.Macierzynski, "wniosek_usredniona");
        Assert.Equal(D("2027-04-05"), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejDo, deadline.Type);
        Assert.Equal("ustawa art. 30a", deadline.Basis);
        Assert.StartsWith("Złóż wniosek o wypłatę zasiłku w wariancie 81,5%", deadline.Text);
        Assert.Contains("do 5 kwietnia 2027", deadline.Text);
        Assert.Contains("po porodzie", deadline.Text);
        Assert.Equal("zasiłek za urlop rodzicielski wyniesie 70% podstawy", deadline.Consequence);
    }

    [Fact]
    public void Averaged_variant_after_the_birth_is_counted_from_the_actual_birth_date()
    {
        var result = Assess(Pregnancy(Mother(), date: "2026-10-08", kind: EventKind.Urodzenie));

        var deadline = Find(result, BenefitId.Macierzynski, "wniosek_usredniona");
        Assert.Equal(D("2026-10-29"), deadline.Date);
        Assert.DoesNotContain("przewidywanej", deadline.Text);
    }

    [Fact]
    public void Averaged_variant_does_not_apply_to_fathers_or_after_a_stillbirth()
    {
        var withFather = Assess(Pregnancy(Mother(), Father()));
        var stillbirth = Assess(Facts(Case(EventKind.Urodzenie, "2026-10-08") with { DzieckoMartweUrodzenie = F(true) }, Mother()));

        Assert.All(
            withFather.Benefits.Where(b => b.Person == PersonRef.DrugiRodzic),
            b => Assert.DoesNotContain(b.Deadlines, d => d.Id == "wniosek_usredniona"));
        Assert.False(Has(stillbirth, BenefitId.Macierzynski, "wniosek_usredniona"));
    }

    [Fact]
    public void Averaged_variant_for_an_adopting_parent_is_21_days_after_taking_the_child()
    {
        var result = Assess(Facts(AdoptionCase("2027-02-10"), Mother()));

        Assert.Equal(D("2027-03-03"), Find(result, BenefitId.PrzyjecieDziecka, "wniosek_usredniona").Date);
    }

    [Fact]
    public void Mother_resignation_request_is_7_days_before_returning_to_work()
    {
        var result = Assess(Pregnancy(Mother(b => b.ResignationAfterWeeks = 14)));

        var deadline = Find(result, BenefitId.Macierzynski, "rezygnacja_matki_z_macierzynskiego");
        Assert.Equal(D("2027-06-14"), deadline.Date);
        Assert.Equal("KP art. 180 § 8", deadline.Basis);
        Assert.Contains("powrotem do pracy 21 czerwca 2027", deadline.Text);
    }

    [Fact]
    public void Father_request_for_part_of_maternity_is_14_days_before_it_starts()
    {
        var facts = Pregnancy(Father(), Mother(b => b.ResignationAfterWeeks = 14));

        var result = Assess(facts);

        var benefit = Get(result, BenefitId.PrzejeciePrzezOjca);
        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        var deadline = benefit.Deadlines.Single(d => d.Id == "wniosek_ojca_o_czesc_macierzynskiego");
        Assert.Equal(D("2027-06-07"), deadline.Date);
        Assert.Equal("KP art. 180 § 9", deadline.Basis);
    }

    [Fact]
    public void Supplementary_leave_request_is_21_days_before_maternity_ends()
    {
        var facts = Facts(
            Case(EventKind.Urodzenie, "2027-03-15") with
            {
                DzieckoTydzienCiazyPrzyPorodzie = F(27),
                DzieckoMasaUrodzeniowaG = F(900),
                DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([new DateRange(D("2027-03-15"), D("2027-05-23"))]),
            },
            Mother());

        var result = Assess(facts, D("2027-03-20"));

        var benefit = Get(result, BenefitId.MacierzynskiUzupelniajacy);
        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        var deadline = benefit.Deadlines.Single(d => d.Id == "wniosek_uzupelniajacy");
        Assert.Equal(D("2027-07-11"), deadline.Date);
        Assert.Contains("końcem urlopu macierzyńskiego 1 sierpnia 2027", deadline.Text);
    }

    [Fact]
    public void Parental_leave_request_is_21_days_before_the_start_and_only_for_employees()
    {
        var employee = Assess(Pregnancy(Mother()));
        var contractor = Assess(Pregnancy(Mother(title: Contract())));

        var deadline = Find(employee, BenefitId.Rodzicielski, "wniosek_rodzicielski");
        Assert.Equal(D("2027-07-12"), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejPrzed, deadline.Type);
        Assert.Equal("KP art. 182¹d § 1", deadline.Basis);
        Assert.Equal("Złóż u pracodawcy wniosek o urlop rodzicielski najpóźniej 12 lipca 2027 (21 dni przed jego początkiem 2 sierpnia 2027).", deadline.Text);
        Assert.False(Has(contractor, BenefitId.Rodzicielski, "wniosek_rodzicielski"));
    }

    [Fact]
    public void Each_part_of_parental_leave_has_its_own_request_deadline()
    {
        var mother = Mother(b => b.ParentalParts = [new LeavePart(10, D("2027-08-02")), new LeavePart(5, D("2028-02-07"))]);

        var result = Assess(Pregnancy(mother));

        var deadlines = Get(result, BenefitId.Rodzicielski).Deadlines;
        Assert.Equal(D("2027-07-12"), deadlines.Single(d => d.Id == "wniosek_rodzicielski").Date);
        var second = deadlines.Single(d => d.Id == "wniosek_rodzicielski_czesc_2");
        Assert.Equal(D("2028-01-17"), second.Date);
        Assert.Contains("część 2 urlopu rodzicielskiego", second.Text);
    }

    [Fact]
    public void Part_time_work_request_is_21_days_before_work_starts()
    {
        var mother = Mother(b =>
        {
            b.ParentalParts = [new LeavePart(32, D("2027-08-02"))];
            b.PartTimeWork = [new PartTimeWork(0.5m, 20, D("2027-09-06"))];
        });

        var result = Assess(Pregnancy(mother));

        var deadline = Find(result, BenefitId.Rodzicielski, "wniosek_praca_na_czesc_etatu");
        Assert.Equal(D("2027-08-16"), deadline.Date);
        Assert.Equal("KP art. 182¹e § 2", deadline.Basis);
    }

    [Fact]
    public void Part_time_work_without_a_start_date_gives_no_deadline()
    {
        var mother = Mother(b =>
        {
            b.ParentalParts = [new LeavePart(32, D("2027-08-02"))];
            b.PartTimeWork = [new PartTimeWork(0.5m, 20)];
        });

        var result = Assess(Pregnancy(mother));

        Assert.False(Has(result, BenefitId.Rodzicielski, "wniosek_praca_na_czesc_etatu"));
    }

    [Fact]
    public void Paternity_leave_request_is_7_days_before_each_part()
    {
        var father = Father(b => b.PaternityParts = [new LeavePart(1, D("2027-05-03")), new LeavePart(1, D("2027-06-07"))]);

        var result = Assess(Pregnancy(father));

        var deadlines = Get(result, BenefitId.Ojcowski).Deadlines;
        Assert.Equal(D("2027-04-26"), deadlines.Single(d => d.Id == "wniosek_ojcowski").Date);
        Assert.Equal(D("2027-05-31"), deadlines.Single(d => d.Id == "wniosek_ojcowski_czesc_2").Date);
        Assert.All(deadlines.Where(d => d.Id.StartsWith("wniosek_ojcowski")), d => Assert.Equal("KP art. 182³ § 2", d.Basis));
    }

    [Fact]
    public void Paternity_leave_window_ends_with_the_12th_month_of_life()
    {
        var result = Assess(Pregnancy(Father()));

        var window = Find(result, BenefitId.Ojcowski, "wykorzystanie_ojcowskiego");
        Assert.Equal(D("2028-03-14"), window.Date);
        Assert.Equal(DeadlineType.Okno, window.Type);
        Assert.Equal("prawo do urlopu ojcowskiego wygasa", window.Consequence);
    }

    [Fact]
    public void Parental_leave_window_ends_with_the_year_the_child_turns_6()
    {
        var result = Assess(Pregnancy(Mother()));

        var window = Find(result, BenefitId.Rodzicielski, "wykorzystanie_rodzicielskiego");
        Assert.Equal(D("2033-12-31"), window.Date);
        Assert.Equal(DeadlineType.Okno, window.Type);
        Assert.Equal("prawo do urlopu rodzicielskiego wygasa", window.Consequence);
    }

    [Fact]
    public void Adoption_leave_request_is_7_days_after_taking_the_child_and_leave_starts_within_21_days()
    {
        var result = Assess(Facts(AdoptionCase("2027-02-10"), Mother()));

        var request = Find(result, BenefitId.PrzyjecieDziecka, "wniosek_urlop_przyjecie");
        Assert.Equal(D("2027-02-17"), request.Date);
        Assert.Equal(DeadlineType.NajpozniejDo, request.Type);
        Assert.Equal("KP art. 183 § 6", request.Basis);
        Assert.Contains("do 17 lutego 2027", request.Text);
        var start = Find(result, BenefitId.PrzyjecieDziecka, "start_urlopu_przyjecie");
        Assert.Equal(D("2027-03-03"), start.Date);
        Assert.Contains("3 marca 2027", start.Text);
    }

    [Fact]
    public void Adoption_leave_request_is_not_shown_for_a_non_employee()
    {
        var result = Assess(Facts(AdoptionCase("2027-02-10"), Mother(title: Contract())));

        Assert.False(Has(result, BenefitId.PrzyjecieDziecka, "wniosek_urlop_przyjecie"));
        Assert.False(Has(result, BenefitId.PrzyjecieDziecka, "start_urlopu_przyjecie"));
    }

    [Fact]
    public void Debt_repayment_deadline_is_six_months_after_the_right_arises()
    {
        var mother = Person(
            ParentRole.Matka,
            Title(TitleKind.Dzialalnosc, "2020-01-01", null, SicknessInsurance.Dobrowolne),
            null,
            b => b.Debt = new ContributionDebt(D("2026-08-10"), 120m));

        var result = Assess(Facts(Case(EventKind.Urodzenie, "2026-08-10"), mother));

        var benefit = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, benefit.Status);
        var deadline = benefit.Deadlines.Single();
        Assert.Equal("splata_zadluzenia", deadline.Id);
        Assert.Equal(D("2027-02-10"), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejDo, deadline.Type);
        Assert.Contains("120,00 zł", deadline.Text);
    }

    [Theory]
    [InlineData(BenefitId.Macierzynski, "2028-02-01")]
    [InlineData(BenefitId.Rodzicielski, "2028-11-14")]
    public void Claim_limitation_is_six_months_after_the_last_day_of_the_period(BenefitId benefit, string expected)
    {
        var result = Assess(Pregnancy(Mother()));

        var deadline = Find(result, benefit, $"przedawnienie_{BenefitNames.Of(benefit)}");
        Assert.Equal(D(expected), deadline.Date);
        Assert.Equal(DeadlineType.Przedawnienie, deadline.Type);
        Assert.Equal("ustawa art. 67 ust. 1", deadline.Basis);
        Assert.Equal("roszczenie o wypłatę zasiłku przepada", deadline.Consequence);
    }

    [Fact]
    public void Contractor_gets_claim_limitation_and_averaged_variant_but_no_employer_requests()
    {
        var result = Assess(Pregnancy(Mother(title: Contract())));

        var ids = Get(result, BenefitId.Macierzynski).Deadlines.Select(d => d.Id).ToList();
        Assert.Contains("przedawnienie_macierzynski", ids);
        Assert.Contains("wniosek_usredniona", ids);
        Assert.DoesNotContain(Get(result, BenefitId.Rodzicielski).Deadlines, d => d.Id.StartsWith("wniosek_"));
    }

    [Fact]
    public void Funeral_request_is_12_months_after_the_burial()
    {
        var facts = Facts(
            Case(EventKind.Urodzenie, "2026-10-08") with { DzieckoMartweUrodzenie = F(true), DzieckoDataPochowku = F(D("2026-10-12")) },
            Mother());

        var result = Assess(facts);

        var deadline = Find(result, BenefitId.Pogrzebowy, "wniosek_zasilek_pogrzebowy");
        Assert.Equal(D("2027-10-12"), deadline.Date);
        Assert.Equal(DeadlineType.NajpozniejDo, deadline.Type);
        Assert.Equal("Złóż do ZUS wniosek Z-12 o zasiłek pogrzebowy z rachunkami do 12 października 2027 (12 miesięcy od dnia pochówku 12 października 2026).", deadline.Text);
    }

    [Fact]
    public void Passed_deadline_is_shown_as_overdue_with_its_consequence()
    {
        var result = Assess(Pregnancy(Mother()), D("2027-05-01"));

        var deadline = Find(result, BenefitId.Macierzynski, "wniosek_usredniona");
        Assert.Equal(D("2027-04-05"), deadline.Date);
        Assert.StartsWith("Termin na wniosek o wypłatę zasiłku w wariancie 81,5%", deadline.Text);
        Assert.Contains("minął 5 kwietnia 2027", deadline.Text);
        Assert.Contains("Skutek: zasiłek za urlop rodzicielski wyniesie 70% podstawy.", deadline.Text);
    }

    [Fact]
    public void Deadline_on_the_last_day_is_still_upcoming()
    {
        var result = Assess(Pregnancy(Mother()), D("2027-04-05"));

        Assert.StartsWith("Złóż wniosek", Find(result, BenefitId.Macierzynski, "wniosek_usredniona").Text);
    }

    [Fact]
    public void Missing_deadline_parameter_skips_only_that_deadline_and_says_so()
    {
        var engine = Engine(SeedWithoutDeadlineKeys());

        var result = Assess(Pregnancy(Mother()), engine: engine);

        var parental = Get(result, BenefitId.Rodzicielski);
        Assert.Equal(BenefitStatus.Przysluguje, parental.Status);
        Assert.DoesNotContain(parental.Deadlines, d => d.Id == "wniosek_rodzicielski");
        Assert.Contains(parental.Notes, n => n.Contains("termin_wniosku_rodzicielski_dni"));
        Assert.Contains(parental.Deadlines, d => d.Id == "wykorzystanie_rodzicielskiego");
        Assert.Contains(Get(result, BenefitId.Macierzynski).Deadlines, d => d.Id == "wniosek_usredniona");
    }

    [Fact]
    public void Deadline_day_counts_come_from_parameters()
    {
        var shortened = new SeedRow(
            DeadlineParameterKeys.WniosekRodzicielskiDni,
            JsonSerializer.SerializeToElement(30),
            D("2027-01-01"),
            "test");

        var result = Assess(Pregnancy(Mother()), engine: Engine(Params(shortened)));

        Assert.Equal(D("2027-07-03"), Find(result, BenefitId.Rodzicielski, "wniosek_rodzicielski").Date);
    }

    [Fact]
    public void Denied_benefits_carry_no_request_deadlines()
    {
        var result = Assess(Pregnancy(Person(ParentRole.Matka, null, null)));

        Assert.All(result.Benefits, b => Assert.DoesNotContain(b.Deadlines, d => d.Id.StartsWith("wniosek_") || d.Id.StartsWith("przedawnienie_")));
    }
}
