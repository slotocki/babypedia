using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class MacierzynskiTests
{
    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 31)]
    [InlineData(3, 33)]
    [InlineData(4, 35)]
    [InlineData(5, 37)]
    [InlineData(6, 37)]
    public void Test1_wymiar_macierzynskiego_zalezy_od_liczby_dzieci(int children, int weeks)
    {
        var result = Get(Assess(Facts(BirthCase("2027-03-15", children))), BenefitId.Macierzynski);

        var from = D("2027-03-15");
        AssertPeriod(result, "2027-03-15", from.AddDays(weeks * 7 - 1).ToString("yyyy-MM-dd"), weeks * 7);
        Assert.Contains(result.Trace, t => t.Rule == "macierzynski_wymiar" && t.Basis == "KP art. 180 § 1" && t.Met == true);
    }

    [Theory]
    [InlineData(1, false, 41)]
    [InlineData(2, false, 43)]
    [InlineData(3, false, 43)]
    [InlineData(1, true, 65)]
    [InlineData(2, true, 67)]
    public void Test1_wymiar_rodzicielskiego_i_za_zyciem(int children, bool zaZyciem, int weeks)
    {
        var sprawa = BirthCase("2027-03-15", children) with { DzieckoZaZyciem = F(zaZyciem) };

        var result = Get(Assess(Facts(sprawa)), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(weeks * 7, result.Period!.Days);
        Assert.Equal(weeks, result.Period.Weeks);
    }

    [Fact]
    public void Test3_bliznieta_za_zyciem_macierzynski_31_rodzicielski_67()
    {
        var sprawa = BirthCase("2027-03-15", 2) with { DzieckoZaZyciem = F(true) };

        var assessment = Assess(Facts(sprawa));

        var maternity = Get(assessment, BenefitId.Macierzynski);
        AssertPeriod(maternity, "2027-03-15", "2027-10-17", 217);
        Assert.Equal(31, maternity.Period!.Weeks);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2027-10-18", "2029-01-28", 469);
        Assert.Equal(67, parental.Period!.Weeks);
    }

    [Fact]
    public void Macierzynski_do_6_tygodni_przed_porodem_przesuwa_poczatek_i_koniec()
    {
        var me = Mother() with { PlanMacierzynskiPrzedPorodemTyg = F(6) };

        var assessment = Assess(Facts(BirthCase("2027-03-15"), me));

        AssertPeriod(Get(assessment, BenefitId.Macierzynski), "2027-02-01", "2027-06-20", 140);
        Assert.Empty(assessment.ValidationErrors);
        Assert.Equal(D("2027-06-21"), Get(assessment, BenefitId.Rodzicielski).Period!.From);
    }

    [Fact]
    public void Macierzynski_ponad_6_tygodni_przed_porodem_to_blad_walidacji_i_okres_od_porodu()
    {
        var me = Mother() with { PlanMacierzynskiPrzedPorodemTyg = F(7) };

        var assessment = Assess(Facts(BirthCase("2027-03-15"), me));

        AssertPeriod(Get(assessment, BenefitId.Macierzynski), "2027-03-15", "2027-08-01", 140);
        var error = Assert.Single(assessment.ValidationErrors, v => v.Code == "macierzynski_przed_porodem_ponad_limit");
        Assert.Equal(FactKeys.PlanMacierzynskiPrzedPorodemTyg, error.Fact);
    }

    [Fact]
    public void Rezygnacja_matki_po_14_tygodniach_skraca_jej_okres_a_przed_14_to_blad()
    {
        var after14 = Assess(Facts(BirthCase("2027-03-15"), Mother() with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(14) }));
        AssertPeriod(Get(after14, BenefitId.Macierzynski), "2027-03-15", "2027-06-20", 98);

        var after10 = Assess(Facts(BirthCase("2027-03-15"), Mother() with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(10) }));
        AssertPeriod(Get(after10, BenefitId.Macierzynski), "2027-03-15", "2027-08-01", 140);
        Assert.Contains(after10.ValidationErrors, v => v.Code == "rezygnacja_z_macierzynskiego_przed_minimum");
    }

    [Fact]
    public void Rezygnacja_matki_z_orzeczeniem_o_niezdolnosci_wymaga_8_tygodni()
    {
        var sprawa = BirthCase("2027-03-15") with
        {
            MatkaSytuacjaSzczegolna = F(new MotherSituation(MotherSituationKind.NiezdolnoscDoSamodzielnejEgzystencji, D("2027-03-15"))),
        };

        var assessment = Assess(Facts(sprawa, Mother() with { PlanRezygnacjaZMacierzynskiegoPoTyg = F(8) }));

        AssertPeriod(Get(assessment, BenefitId.Macierzynski), "2027-03-15", "2027-05-09", 56);
        Assert.Empty(assessment.ValidationErrors);
    }

    [Fact]
    public void Ojciec_nie_dostaje_macierzynskiego_ani_uzupelniajacego()
    {
        var assessment = Assess(Facts(BirthCase("2027-03-15"), Father()));

        Assert.DoesNotContain(assessment.Benefits, b => b.Id is BenefitId.Macierzynski or BenefitId.MacierzynskiUzupelniajacy);
        Assert.Contains(assessment.Benefits, b => b.Id == BenefitId.Ojcowski);
    }

    [Fact]
    public void Brak_liczby_dzieci_to_brak_danych_a_nie_zalozenie_jednego_dziecka()
    {
        var sprawa = BirthCase("2027-03-15") with { DzieckoLiczba = null };

        var assessment = Assess(Facts(sprawa));

        var result = Get(assessment, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.BrakujeDanych, result.Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoLiczba);
        Assert.Null(result.Period);
    }

    [Fact]
    public void Brak_daty_zdarzenia_to_brak_danych_dla_wszystkich_swiadczen()
    {
        var sprawa = BirthCase("2027-03-15") with { ZdarzenieData = null };

        var assessment = Assess(Facts(sprawa));

        Assert.All(assessment.Benefits.Where(b => b.Id != BenefitId.MacierzynskiUzupelniajacy), b => Assert.Equal(BenefitStatus.BrakujeDanych, b.Status));
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.ZdarzenieData);
    }

    [Fact]
    public void Brak_roli_osoby_to_brak_danych()
    {
        var assessment = Assess(Facts(BirthCase("2027-03-15"), new PersonFacts()));

        Assert.All(assessment.Benefits, b => Assert.Equal(BenefitStatus.BrakujeDanych, b.Status));
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.OsobaRola && m.Person == PersonRef.Wnioskodawca);
    }

    [Fact]
    public void Brak_rodzaju_zdarzenia_to_brak_danych_bez_swiadczen()
    {
        var assessment = Assess(Facts(new CaseFacts()));

        Assert.Empty(assessment.Benefits);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.ZdarzenieRodzaj);
    }

    [Fact]
    public void Parametr_wymiaru_wybiera_dzien_porodu()
    {
        var change = Row("macierzynski_tyg.dzieci_1", 21, "2027-01-01");

        var before = Get(Assess(Facts(BirthCase("2026-12-31")), null, change), BenefitId.Macierzynski);
        var after = Get(Assess(Facts(BirthCase("2027-01-01")), null, change), BenefitId.Macierzynski);

        Assert.Equal(140, before.Period!.Days);
        Assert.Equal(147, after.Period!.Days);
    }

    [Fact]
    public void Brak_parametru_to_nie_rozstrzygam()
    {
        var result = Get(Assess(Facts(BirthCase("1999-12-31"))), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
        Assert.Contains("macierzynski_tyg.dzieci_1", result.Reason);
        Assert.Null(result.Period);
    }
}
