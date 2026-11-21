using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class RodzicielskiTests
{
    private const string Birth = "2026-03-01";

    private static LeavePart Part(int weeks, string? from = null) => new(weeks, from is null ? null : D(from));

    private static PersonFacts MotherWithParts(params LeavePart[] parts) =>
        Mother() with { PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>(parts) };

    private static PersonFacts FatherWithParts(params LeavePart[] parts) =>
        Father() with { PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>(parts) };

    [Fact]
    public void Test10_szosta_czesc_rodzicielskiego_to_odmowa()
    {
        var plan = MotherWithParts(Part(5), Part(5, "2027-01-04"), Part(5, "2027-03-01"), Part(5, "2027-05-03"), Part(5, "2027-07-05"), Part(5, "2027-09-06"));

        var assessment = Assess(Facts(BirthCase(Birth), plan));

        var result = Get(assessment, BenefitId.Rodzicielski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("5 częściach", result.Reason);
        Assert.Equal("KP art. 182¹c; art. 182¹d § 2", result.LegalBasis);
        Assert.Contains(assessment.ValidationErrors, v => v.Code == "rodzicielski_ponad_limit_czesci" && v.Fact == FactKeys.PlanRodzicielskiCzesci);
        Assert.Contains(result.Trace, t => t.Rule == "rodzicielski_max_czesci" && t.Met == false);
    }

    [Fact]
    public void Szosta_czesc_liczy_tez_wczesniej_pobrane_swiadczenia_rodzicielskie()
    {
        var received = Enumerable.Range(0, 5)
            .Select(k => new ReceivedBenefit(ReceivedBenefitKind.Rodzicielski, D("2026-06-01").AddDays(k * 30), D("2026-06-07").AddDays(k * 30)))
            .ToList();
        var father = FatherWithParts(Part(4, "2027-01-04")) with { UbezpieczeniePobraneSwiadczenia = F<IReadOnlyList<ReceivedBenefit>>(received) };

        var result = Get(Assess(Facts(BirthCase(Birth), father)), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
    }

    [Fact]
    public void Piec_czesci_jest_dopuszczalne()
    {
        var plan = MotherWithParts(Part(5), Part(5, "2027-01-04"), Part(5, "2027-03-01"), Part(5, "2027-05-03"), Part(5, "2027-07-05"));

        var result = Get(Assess(Facts(BirthCase(Birth), plan)), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(5, result.Parts.Count);
        Assert.Equal(25 * 7, result.Period!.Days);
    }

    [Fact]
    public void Test10_jeden_rodzic_33_tygodnie_z_41_to_blad_walidacji()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(33))));

        var error = Assert.Single(assessment.ValidationErrors);
        Assert.Equal("rodzicielski_ponad_limit_jednego_rodzica", error.Code);
        Assert.Equal(FactKeys.PlanRodzicielskiCzesci, error.Fact);
        Assert.Equal(PersonRef.Wnioskodawca, error.Person);
        Assert.Contains("32", error.Message);
        Assert.Equal(41 * 7, Get(assessment, BenefitId.Rodzicielski).Period!.Days);
    }

    [Fact]
    public void Test10_jeden_rodzic_32_tygodnie_z_41_jest_poprawne()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(32))));

        Assert.Empty(assessment.ValidationErrors);
        AssertPeriod(Get(assessment, BenefitId.Rodzicielski), "2026-07-19", "2027-02-27", 224);
    }

    [Theory]
    [InlineData(56, false)]
    [InlineData(57, true)]
    public void Test10_limit_jednego_rodzica_z_za_zyciem_to_wymiar_minus_9(int weeks, bool error)
    {
        var sprawa = BirthCase(Birth) with { DzieckoZaZyciem = F(true) };

        var assessment = Assess(Facts(sprawa, MotherWithParts(Part(weeks))));

        Assert.Equal(error, assessment.ValidationErrors.Any(v => v.Code == "rodzicielski_ponad_limit_jednego_rodzica"));
    }

    [Fact]
    public void Plany_obojga_rodzicow_nie_moga_przekroczyc_wymiaru()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(32)), FatherWithParts(Part(32, "2027-06-07"))));

        Assert.Contains(assessment.ValidationErrors, v => v.Code == "rodzicielski_ponad_wymiar_lacznie");
    }

    [Fact]
    public void Plany_obojga_rodzicow_w_ramach_wymiaru_licza_okresy_dla_kazdego_z_osobna()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(32)), FatherWithParts(Part(9, "2027-06-07"))));

        Assert.Empty(assessment.ValidationErrors);
        AssertPeriod(Get(assessment, BenefitId.Rodzicielski), "2026-07-19", "2027-02-27", 224);
        AssertPeriod(Get(assessment, BenefitId.Rodzicielski, PersonRef.DrugiRodzic), "2027-06-07", "2027-08-08", 63);
        Assert.Contains(assessment.Benefits, b => b.Id == BenefitId.Ojcowski && b.Person == PersonRef.DrugiRodzic);
    }

    [Fact]
    public void Ojciec_bez_planu_ma_wymiar_ale_bez_ustalonego_okresu()
    {
        var result = Get(Assess(Facts(BirthCase(Birth), Father())), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Null(result.Period);
        Assert.Contains(result.Notes, n => n.Contains("plan"));
        Assert.Contains(result.Deadlines, d => d.Id == "wykorzystanie_rodzicielskiego" && d.Date == D("2032-12-31"));
    }

    [Fact]
    public void Czesci_matki_bez_daty_pierwszej_czesci_zaczynaja_sie_po_macierzynskim()
    {
        var plan = MotherWithParts(Part(10), Part(10, "2027-12-01"), Part(12, "2028-06-01"));

        var result = Get(Assess(Facts(BirthCase(Birth), plan)), BenefitId.Rodzicielski);

        Assert.Equal(3, result.Parts.Count);
        Assert.Equal(D("2026-07-19"), result.Parts[0].Period.From);
        Assert.Equal(D("2026-09-26"), result.Parts[0].Period.To);
        Assert.Equal(D("2028-08-23"), result.Parts[2].Period.To);
        Assert.Equal(224, result.Period!.Days);
        Assert.Equal(D("2026-07-19"), result.Period.From);
        Assert.Equal(D("2028-08-23"), result.Period.To);
        Assert.All(result.Parts, p => Assert.Equal(0.7m, p.Rate));
    }

    [Fact]
    public void Czesc_matki_przed_koncem_macierzynskiego_to_blad_i_okres_bez_planu()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(10, "2026-06-01"))));

        Assert.Contains(assessment.ValidationErrors, v => v.Code == "rodzicielski_przed_koncem_macierzynskiego");
        AssertPeriod(Get(assessment, BenefitId.Rodzicielski), "2026-07-19", "2027-05-01", 287);
    }

    [Fact]
    public void Czesci_nachodzace_na_siebie_to_blad()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(10, "2026-08-03"), Part(10, "2026-09-07"))));

        Assert.Contains(assessment.ValidationErrors, v => v.Code == "rodzicielski_czesci_nachodza");
    }

    [Fact]
    public void Czesc_po_koncu_roku_w_ktorym_dziecko_konczy_6_lat_to_odmowa()
    {
        var assessment = Assess(Facts(BirthCase(Birth), MotherWithParts(Part(10, "2032-12-26"))));

        var result = Get(assessment, BenefitId.Rodzicielski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("2032-12-31", result.Reason);
        Assert.Equal(BenefitStatus.Przysluguje, Get(Assess(Facts(BirthCase(Birth), MotherWithParts(Part(1, "2032-12-25")))), BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Test11_pol_etatu_przez_20_tygodni_wydluza_urlop_o_10_tygodni()
    {
        var me = Mother() with { PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 20)]) };

        var result = Get(Assess(Facts(BirthCase(Birth), me)), BenefitId.Rodzicielski);

        AssertPeriod(result, "2026-07-19", "2027-07-10", 357);
        Assert.Equal(51, result.Period!.Weeks);
        Assert.Contains(result.Notes, n => n.Contains("70 dni"));
        Assert.Contains(result.Trace, t => t.Rule == "rodzicielski_wydluzenie_praca" && t.Met == true && t.Basis.Contains("182¹f"));
    }

    [Fact]
    public void Wydluzenie_za_prace_ma_limit_82_tygodni_i_nie_obejmuje_niepelnych_dni()
    {
        var capped = Mother() with { PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 100)]) };
        AssertPeriod(Get(Assess(Facts(BirthCase(Birth), capped)), BenefitId.Rodzicielski), "2026-07-19", "2028-02-12", 574);

        var fractional = Mother() with { PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.3m, 1)]) };
        Assert.Equal(41 * 7 + 2, Get(Assess(Facts(BirthCase(Birth), fractional)), BenefitId.Rodzicielski).Period!.Days);
    }

    [Fact]
    public void Praca_ponad_pol_etatu_to_blad_walidacji_i_urlop_bez_wydluzenia()
    {
        var me = Mother() with { PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.6m, 10)]) };

        var assessment = Assess(Facts(BirthCase(Birth), me));

        Assert.Contains(assessment.ValidationErrors, v => v.Code == "praca_na_rodzicielskim_wymiar" && v.Fact == FactKeys.PlanPracaNaCzescEtatu);
        Assert.Equal(287, Get(assessment, BenefitId.Rodzicielski).Period!.Days);
    }

    [Fact]
    public void Praca_na_rodzicielskim_w_czesciach_dodaje_wydluzenie_do_czesci_z_praca()
    {
        var me = Mother() with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([Part(10), Part(10, "2027-12-01")]),
            PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 10, D("2027-12-10"))]),
        };

        var result = Get(Assess(Facts(BirthCase(Birth), me)), BenefitId.Rodzicielski);

        Assert.Equal(70, result.Parts[0].Period.Days);
        Assert.Equal(70 + 35, result.Parts[1].Period.Days);
        Assert.Equal(175, result.Period!.Days);
    }

    [Fact]
    public void Brak_roli_wnioskodawcy_ukrywa_nic_ale_pyta()
    {
        var assessment = Assess(Facts(BirthCase(Birth), new PersonFacts()));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Dla_ja_i_drugi_rodzic_bez_danych_drugiego_rodzica_pyta_o_jego_role()
    {
        var sprawa = BirthCase(Birth) with { ZdarzenieDlaKogo = F(CalculationScope.JaIDrugiRodzic) };

        var assessment = Assess(Facts(sprawa));

        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.OsobaRola && m.Person == PersonRef.DrugiRodzic);
    }
}
