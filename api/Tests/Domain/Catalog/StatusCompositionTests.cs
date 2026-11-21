using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class StatusCompositionTests
{
    private static readonly CatalogEntry Entry = BenefitCatalog.Get(BenefitId.Rodzicielski);

    private static readonly Period SomePeriod = new(new DateOnly(2027, 8, 2), new DateOnly(2028, 3, 12), 224, 32);

    private static BenefitStatus S(string code) => code switch
    {
        "P" => BenefitStatus.Przysluguje,
        "N" => BenefitStatus.NiePrzysluguje,
        "B" => BenefitStatus.BrakujeDanych,
        "D" => BenefitStatus.NieDotyczy,
        _ => BenefitStatus.NieRozstrzygam,
    };

    private static BenefitResult Rights(BenefitStatus status) => new()
    {
        Id = Entry.Id,
        Status = status,
        Payer = status == BenefitStatus.Przysluguje ? Payer.Zus : null,
        WhereToApply = status == BenefitStatus.Przysluguje ? ApplicationPlace.Zus : null,
        Reason = status is BenefitStatus.NiePrzysluguje or BenefitStatus.NieRozstrzygam ? "powód z uprawnień" : null,
        LegalBasis = status is BenefitStatus.NiePrzysluguje or BenefitStatus.NieRozstrzygam ? "ustawa art. 29" : null,
        Trace = [new TraceStep("regula_prawa", "ustawa", true, "prawo")],
        Notes = ["notatka z uprawnień"],
    };

    private static BenefitResult Periods(BenefitStatus status) => new()
    {
        Id = Entry.Id,
        Status = status,
        Period = status == BenefitStatus.Przysluguje ? SomePeriod : null,
        Reason = status is BenefitStatus.NiePrzysluguje or BenefitStatus.NieRozstrzygam ? "powód z okresów" : null,
        LegalBasis = status is BenefitStatus.NiePrzysluguje or BenefitStatus.NieRozstrzygam ? "KP art. 182¹c" : null,
        Trace = [new TraceStep("regula_okresu", "KP", true, "okres")],
        Notes = ["notatka z okresów"],
    };

    public static TheoryData<string, string, string> Matrix() => new()
    {
        { "P", "P", "P" }, { "P", "N", "N" }, { "P", "B", "B" }, { "P", "D", "D" }, { "P", "R", "R" },
        { "N", "P", "N" }, { "N", "N", "N" }, { "N", "B", "N" }, { "N", "D", "D" }, { "N", "R", "N" },
        { "B", "P", "B" }, { "B", "N", "N" }, { "B", "B", "B" }, { "B", "D", "D" }, { "B", "R", "R" },
        { "D", "P", "D" }, { "D", "N", "D" }, { "D", "B", "D" }, { "D", "D", "D" }, { "D", "R", "D" },
        { "R", "P", "R" }, { "R", "N", "N" }, { "R", "B", "R" }, { "R", "D", "D" }, { "R", "R", "R" },
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Precedence_matrix_for_every_rights_and_period_combination(string rights, string period, string expected)
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(S(rights)), Periods(S(period)), employee: false);

        Assert.Equal(S(expected), result.Status);
        Assert.Equal(Entry.Id, result.Id);
    }

    [Theory]
    [InlineData("P")]
    [InlineData("N")]
    [InlineData("B")]
    [InlineData("D")]
    [InlineData("R")]
    public void Rights_only_benefit_keeps_the_rights_status(string rights)
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(S(rights)), null, employee: false);

        Assert.Equal(S(rights), result.Status);
    }

    [Fact]
    public void Denial_from_rights_hides_the_period_and_keeps_only_rights_trace()
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.NiePrzysluguje), Periods(BenefitStatus.Przysluguje), false);

        Assert.Null(result.Period);
        Assert.Equal("powód z uprawnień", result.Reason);
        Assert.Equal("ustawa art. 29", result.LegalBasis);
        Assert.Equal(["regula_prawa"], result.Trace.Select(t => t.Rule));
    }

    [Fact]
    public void Denial_from_period_keeps_rights_trace_first_and_the_period_reason()
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.Przysluguje), Periods(BenefitStatus.NiePrzysluguje), false);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Equal("powód z okresów", result.Reason);
        Assert.Equal("KP art. 182¹c", result.LegalBasis);
        Assert.Null(result.Payer);
        Assert.Equal(["regula_prawa", "regula_okresu"], result.Trace.Select(t => t.Rule));
    }

    [Fact]
    public void When_both_deny_the_reason_comes_from_rights()
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.NiePrzysluguje), Periods(BenefitStatus.NiePrzysluguje), false);

        Assert.Equal("powód z uprawnień", result.Reason);
    }

    [Fact]
    public void Granted_benefit_merges_period_payer_trace_notes_and_deduplicates_deadlines()
    {
        var shared = new Deadline("wniosek_rodzicielski", DeadlineType.NajpozniejPrzed, new DateOnly(2027, 7, 12), "z uprawnień");
        var rights = Rights(BenefitStatus.Przysluguje) with
        {
            Deadlines = [shared],
            Documents = ["ZAM — wniosek o zasiłek macierzyński"],
        };
        var period = Periods(BenefitStatus.Przysluguje) with
        {
            Deadlines = [shared with { Text = "z okresów" }, new Deadline("wykorzystanie_rodzicielskiego", DeadlineType.Okno, new DateOnly(2033, 12, 31), "okno")],
            Notes = ["notatka z uprawnień", "notatka z okresów"],
        };

        var result = StatusComposer.Compose(Entry, PersonRef.DrugiRodzic, rights, period, employee: true);

        Assert.Equal(PersonRef.DrugiRodzic, result.Person);
        Assert.Equal(SomePeriod, result.Period);
        Assert.Equal(Payer.Zus, result.Payer);
        Assert.Equal(ApplicationPlace.Zus, result.WhereToApply);
        Assert.Equal(["regula_prawa", "regula_okresu"], result.Trace.Select(t => t.Rule));
        Assert.Equal(["wniosek_rodzicielski", "wykorzystanie_rodzicielskiego"], result.Deadlines.Select(d => d.Id));
        Assert.Equal("z uprawnień", result.Deadlines[0].Text);
        Assert.Equal(["notatka z uprawnień", "notatka z okresów"], result.Notes);
        Assert.Contains("Wniosek do pracodawcy o urlop rodzicielski", result.Documents);
        Assert.Contains("ZAM — wniosek o zasiłek macierzyński", result.Documents);
    }

    [Fact]
    public void Employer_documents_are_added_only_for_employees()
    {
        var rights = Rights(BenefitStatus.Przysluguje);

        var employee = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, rights, Periods(BenefitStatus.Przysluguje), employee: true);
        var other = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, rights, Periods(BenefitStatus.Przysluguje), employee: false);

        Assert.Contains(Entry.EmployerDocuments[0], employee.Documents);
        Assert.DoesNotContain(Entry.EmployerDocuments[0], other.Documents);
    }

    [Fact]
    public void Undecided_from_period_adds_zus_contact_channels()
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.Przysluguje), Periods(BenefitStatus.NieRozstrzygam), false);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
        Assert.Equal("powód z okresów", result.Reason);
        Assert.Contains(StatusComposer.ContactNote, result.Notes);
        Assert.Contains(result.Links, l => l.Url == "https://www.zus.pl/kontakt");
        Assert.Null(result.Period);
    }

    [Fact]
    public void Undecided_contact_note_is_not_duplicated_when_rights_already_has_it()
    {
        var rights = Rights(BenefitStatus.NieRozstrzygam) with { Notes = [StatusComposer.ContactNote] };

        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, rights, null, false);

        Assert.Single(result.Notes, n => n.StartsWith("Skontaktuj się z ZUS"));
    }

    [Fact]
    public void Missing_data_and_not_applicable_carry_no_period_reason_or_documents()
    {
        var missing = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.BrakujeDanych), Periods(BenefitStatus.Przysluguje), true);
        var notApplicable = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.NieDotyczy), Periods(BenefitStatus.Przysluguje), true);

        Assert.Null(missing.Period);
        Assert.Null(missing.Reason);
        Assert.Empty(missing.Documents);
        Assert.Empty(notApplicable.Trace);
        Assert.Empty(notApplicable.Notes);
        Assert.Null(notApplicable.Period);
    }

    [Fact]
    public void Every_granted_benefit_links_to_zus_and_leave_benefits_also_to_mrpips_and_pip()
    {
        var result = StatusComposer.Compose(Entry, PersonRef.Wnioskodawca, Rights(BenefitStatus.Przysluguje), Periods(BenefitStatus.Przysluguje), false);

        var urls = result.Links.Select(l => l.Url).ToList();
        Assert.Contains("https://www.zus.pl", urls);
        Assert.Contains("https://www.gov.pl/web/rodzina/urlop-rodzicielski", urls);
        Assert.Contains("https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo", urls);
    }
}
