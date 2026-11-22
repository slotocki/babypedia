using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class OjcowskiTests
{
    private const string Birth = "2026-01-10";

    private static PersonFacts FatherWithParts(params LeavePart[] parts) =>
        Father() with { PlanOjcowskiCzesci = F<IReadOnlyList<LeavePart>>(parts) };

    private static LeavePart Part(int weeks, string? from = null) => new(weeks, from is null ? null : D(from));

    [Fact]
    public void Test7_dziecko_ma_13_miesiecy_to_odmowa()
    {
        var result = Get(Assess(Facts(BirthCase(Birth), Father()), D("2027-02-21")), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("09.01.2027", result.Reason);
        Assert.Equal("KP art. 182³ § 1, § 1¹", result.LegalBasis);
        Assert.Null(result.Period);
    }

    [Fact]
    public void Test7_dwie_czesci_po_tygodniu_to_przyznanie()
    {
        var father = FatherWithParts(Part(1, "2026-02-02"), Part(1, "2026-03-02"));

        var assessment = Assess(Facts(BirthCase(Birth), father));

        var result = Get(assessment, BenefitId.Ojcowski);
        AssertPeriod(result, "2026-02-02", "2026-03-08", 14);
        Assert.Equal(2, result.Parts.Count);
        Assert.Equal(D("2026-02-08"), result.Parts[0].Period.To);
        Assert.All(result.Parts, p => Assert.Equal(1m, p.Rate));
        Assert.Empty(assessment.ValidationErrors);
    }

    [Fact]
    public void Test7_dwie_czesci_bez_dat_to_przyznanie_w_terminie()
    {
        var result = Get(Assess(Facts(BirthCase(Birth), FatherWithParts(Part(1), Part(1)))), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Null(result.Period);
        var window = Assert.Single(result.Deadlines);
        Assert.Equal("wykorzystanie_ojcowskiego", window.Id);
        Assert.Equal(DeadlineType.Okno, window.Type);
        Assert.Equal(D("2027-01-09"), window.Date);
    }

    [Fact]
    public void Jedna_czesc_2_tygodnie_jest_poprawna()
    {
        AssertPeriod(Get(Assess(Facts(BirthCase(Birth), FatherWithParts(Part(2, "2026-02-02")))), BenefitId.Ojcowski), "2026-02-02", "2026-02-15", 14);
    }

    [Fact]
    public void Ostatni_dzien_okna_to_dzien_przed_pierwszymi_urodzinami()
    {
        var onTime = Get(Assess(Facts(BirthCase(Birth), FatherWithParts(Part(1, "2027-01-03")))), BenefitId.Ojcowski);
        var late = Get(Assess(Facts(BirthCase(Birth), FatherWithParts(Part(1, "2027-01-04")))), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.Przysluguje, onTime.Status);
        Assert.Equal(BenefitStatus.NiePrzysluguje, late.Status);
    }

    [Fact]
    public void Trzy_czesci_urlopu_ojcowskiego_to_odmowa()
    {
        var assessment = Assess(Facts(BirthCase(Birth), FatherWithParts(Part(1), Part(1), Part(1))));

        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Ojcowski).Status);
        Assert.Contains(assessment.ValidationErrors, v => v.Code == "ojcowski_ponad_limit_czesci");
    }

    [Theory]
    [InlineData(0, "ojcowski_czesc_zbyt_krotka")]
    [InlineData(3, "ojcowski_ponad_wymiar")]
    public void Czesc_krotsza_niz_tydzien_albo_ponad_2_tygodnie_to_blad_walidacji(int weeks, string code)
    {
        var assessment = Assess(Facts(BirthCase(Birth), FatherWithParts(Part(weeks, "2026-02-02"))));

        Assert.Contains(assessment.ValidationErrors, v => v.Code == code && v.Fact == FactKeys.PlanOjcowskiCzesci);
        Assert.Equal(BenefitStatus.Przysluguje, Get(assessment, BenefitId.Ojcowski).Status);
    }

    [Fact]
    public void Bez_planu_ojcowski_przysluguje_w_oknie_bez_okresu()
    {
        var result = Get(Assess(Facts(BirthCase(Birth), Father()), D("2026-06-01")), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Null(result.Period);
        Assert.Contains(result.Notes, n => n.Contains("09.01.2027"));
    }

    [Fact]
    public void Matce_ojcowski_sie_nie_pojawia()
    {
        Assert.DoesNotContain(Assess(Facts(BirthCase(Birth), Mother())).Benefits, b => b.Id == BenefitId.Ojcowski);
    }

    [Fact]
    public void Ojcowski_przy_przysposobieniu_liczy_12_miesiecy_od_uprawomocnienia()
    {
        var sprawa = AdoptionCase("2026-08-01", "2024-05-10", AdoptionPath.Przysposobienie) with
        {
            PrzyjecieDataUprawomocnieniaPostanowienia = F(D("2026-10-01")),
        };

        var result = Get(Assess(Facts(sprawa, Father())), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(D("2027-09-30"), Assert.Single(result.Deadlines).Date);
    }

    [Fact]
    public void Ojcowski_przy_przysposobieniu_nie_dluzej_niz_do_14_roku_zycia_dziecka()
    {
        var sprawa = AdoptionCase("2026-08-01", "2012-12-15", AdoptionPath.Przysposobienie) with
        {
            PrzyjecieDataUprawomocnieniaPostanowienia = F(D("2026-10-01")),
        };

        var result = Get(Assess(Facts(sprawa, Father())), BenefitId.Ojcowski);

        Assert.Equal(D("2026-12-14"), Assert.Single(result.Deadlines).Date);
    }

    [Fact]
    public void Ojcowski_przy_przysposobieniu_bez_daty_uprawomocnienia_pyta_o_nia()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-08-01", "2024-05-10", AdoptionPath.Przysposobienie), Father()));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Ojcowski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.PrzyjecieDataUprawomocnieniaPostanowienia);
    }

    [Fact]
    public void Ojcowski_w_rodzinie_zastepczej_to_nie_rozstrzygam()
    {
        var result = Get(Assess(Facts(AdoptionCase("2026-08-01", "2024-05-10", AdoptionPath.RodzinaZastepczaNiezawodowa), Father())), BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
    }
}
