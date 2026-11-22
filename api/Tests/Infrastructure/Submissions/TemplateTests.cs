using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Submissions;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

public sealed partial class TemplateTests
{
    private static string Squash(string text) => WhiteSpace().Replace(text, "");

    private static void Has(string squashedText, string expected) => Assert.Contains(Squash(expected), squashedText);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();

    private static byte[] Render(string templateId, out DocumentModel model)
    {
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        var fields = Samples.Fields(templateId);
        Assert.True(SubmissionValidator.Validate(spec, fields).IsValid, $"Przykład {templateId} powinien być poprawny.");
        model = DocumentBuilder.Build(spec, fields, "BR-SYM-TEST-20261121-ABCD2345", new DateOnly(2026, 11, 21));
        return PdfRenderer.Render(model);
    }

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public void Each_template_renders_a_pdf_with_footer_banner_and_number(string templateId)
    {
        var pdf = Render(templateId, out var model);

        Assert.True(pdf.Length > 5_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        var text = Squash(PdfText.Extract(pdf));
        Has(text, PdfRenderer.FooterText);
        Has(text, "SYMULACJA");
        Has(text, "BR-SYM-TEST-20261121-ABCD2345");
        Has(text, "21.11.2026");
        Has(text, model.Title);
        Assert.Equal(model.Title, PdfText.Title(pdf));
    }

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public void Person_name_with_polish_characters_survives_in_the_pdf(string templateId)
    {
        var text = Squash(PdfText.Extract(Render(templateId, out _)));

        Has(text, Samples.Fields(templateId)["imieNazwisko"]);
        Has(text, "złożeniem");
    }

    [Fact]
    public void Pdf_contains_every_polish_diacritic()
    {
        var model = new DocumentModel(
            "Zażółć gęślą jaźń — ĄĆĘŁŃÓŚŹŻ",
            "BR-SYM-TEST-20261121-ABCD2345",
            new DateOnly(2026, 11, 21),
            null,
            null,
            "SYMULACJA",
            [new DocumentSection("Test", [new DocumentRow("ąćęłńóśźż", "ĄĆĘŁŃÓŚŹŻ", null)])],
            [],
            [],
            false);
        var pdf = PdfRenderer.Render(model);
        var text = Squash(PdfText.Extract(pdf));

        Has(text, "Zażółć gęślą jaźń — ĄĆĘŁŃÓŚŹŻ");
        Has(text, "ąćęłńóśźż");
        Has(text, "ĄĆĘŁŃÓŚŹŻ");
        Assert.Equal("Zażółć gęślą jaźń — ĄĆĘŁŃÓŚŹŻ", PdfText.Title(pdf));
    }

    [Fact]
    public void Full_leave_contains_elements_of_paragraph_2_and_15()
    {
        var pdf = Render(LeaveTemplateIds.MacierzynskiIRodzicielski, out var model);
        var text = Squash(PdfText.Extract(pdf));

        Has(text, "04.01.2027");
        Has(text, "02.08.2027");
        Has(text, "01.03.2028");
        Has(text, "od 2028-03-02 do 2028-04-30");
        Has(text, "zaświadczenia lekarskiego");
        Has(text, "Kopia zaświadczenia, o którym mowa w art. 4 ust. 3 ustawy „Za życiem”");
        Assert.DoesNotContain(model.Attachments, a => a.Text.Contains("odroczeniu obowiązku szkolnego"));
        Has(text, "Do: Zażółć Gęślą Jaźń sp. z o.o.");
        Has(text, "Łódź, 21.11.2026");
    }

    [Fact]
    public void Parental_leave_lists_adoption_attachments_and_part_time_section()
    {
        var pdf = Render(LeaveTemplateIds.Rodzicielski, out var model);
        var text = Squash(PdfText.Extract(pdf));

        Has(text, "1/2 etatu");
        Has(text, "15.06.2027");
        Assert.Contains(model.Attachments, a => a.Text.Contains("odroczeniu obowiązku szkolnego") && a.Basis!.Contains("§ 15 ust. 4 pkt 2"));
        Assert.DoesNotContain(model.Attachments, a => a.Text.Contains("Za życiem"));
        Assert.Contains(model.Sections, s => s.Title.StartsWith("Łączenie urlopu rodzicielskiego z pracą"));
    }

    [Fact]
    public void Paternity_and_supplementary_leave_contain_regulation_fields()
    {
        var paternity = Squash(PdfText.Extract(Render(LeaveTemplateIds.Ojcowski, out var p)));
        Has(paternity, "15.02.2027");
        Has(paternity, "28.02.2027");
        Assert.Contains(p.Attachments, a => a.Text.Contains("postanowienia sądu o przysposobieniu") && a.Basis!.Contains("§ 17 ust. 2 pkt 2"));

        var supplementary = Squash(PdfText.Extract(Render(LeaveTemplateIds.Uzupelniajacy, out var u)));
        Has(supplementary, "31.07.2027");
        Has(supplementary, "26.09.2027");
        Assert.Contains(u.Attachments, a => a.Text.Contains("art. 180² § 5") && a.Basis!.Contains("§ 14 ust. 2 pkt 1"));
    }

    [Fact]
    public void Zam_summary_covers_annex_6_without_pesel_or_zus_markings()
    {
        var pdf = Render("dane_zam", out var model);
        var text = Squash(PdfText.Extract(pdf));

        Has(text, "Dane do wniosku o zasiłek macierzyński (ZAM) — zestawienie");
        Has(text, "5252525252 / 123456789");
        Has(text, "https://www.zus.pl");
        Has(text, "To nie jest druk ZUS ZAM");
        Has(text, "nie zbiera i nie drukuje");
        Assert.Equal(TemplateCatalog.Zam.Fields.Count(f => SubmissionValidator.IsApplicable(f, Samples.Fields("dane_zam"))),
            model.Sections.Sum(s => s.Rows.Count));
        Assert.All(TemplateCatalog.Zam.Fields, f => Assert.Matches(@"Zał\. 6 pkt \d+", f.Basis));
        Assert.DoesNotContain(TemplateCatalog.Zam.Fields, f => f.Label.Contains("PESEL") || f.Label.Contains("rachunk"));
    }

    [Fact]
    public void Every_field_names_its_legal_basis_and_regulation_fields_cite_a_paragraph()
    {
        Assert.All(TemplateCatalog.All, spec => Assert.All(spec.Fields, f => Assert.False(string.IsNullOrWhiteSpace(f.Basis), $"{spec.Id}/{f.Key}")));
        Assert.All(TemplateCatalog.Leave, spec =>
            Assert.All(spec.Fields.Where(f => !f.Basis.StartsWith("pole pomocnicze")), f => Assert.Contains("§", f.Basis)));
        Assert.All(TemplateCatalog.All.SelectMany(t => t.Fields).Where(f => f.Kind == FieldKind.Choice), f => Assert.NotEmpty(f.Options!));
    }

    [Fact]
    public void Leave_paragraph_requirements_are_covered()
    {
        static string[] BasisOf(TemplateSpec spec) => spec.Fields.Select(f => f.Basis).ToArray();

        Assert.Contains(BasisOf(TemplateCatalog.FullLeave), b => b.StartsWith("§ 2 ust. 1 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.FullLeave), b => b.StartsWith("§ 15 ust. 1 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.FullLeave), b => b.StartsWith("§ 15 ust. 1 pkt 3"));
        Assert.Contains(BasisOf(TemplateCatalog.Parental), b => b.StartsWith("§ 15 ust. 2 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.Parental), b => b.StartsWith("§ 16 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.Parental), b => b.StartsWith("§ 16 pkt 3"));
        Assert.Contains(BasisOf(TemplateCatalog.Parental), b => b.StartsWith("§ 16 pkt 4"));
        Assert.Contains(BasisOf(TemplateCatalog.Paternity), b => b.StartsWith("§ 17 ust. 1 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.Paternity), b => b.StartsWith("§ 17 ust. 2 pkt 3"));
        Assert.Contains(BasisOf(TemplateCatalog.Supplementary), b => b.StartsWith("§ 14 ust. 1 pkt 2"));
        Assert.Contains(BasisOf(TemplateCatalog.Supplementary), b => b.StartsWith("§ 14 ust. 1 pkt 3"));
        Assert.Contains(BasisOf(TemplateCatalog.Supplementary), b => b.StartsWith("§ 14 ust. 2 pkt 2"));
    }

    [Fact]
    public void Validation_rejects_pesel_like_numbers_unknown_keys_bad_dates_and_missing_required_fields()
    {
        var fields = new Dictionary<string, string>
        {
            ["imieNazwisko"] = Samples.Name,
            ["temat"] = "nie_ma_takiego",
            ["tresc"] = "Mój numer to 12345678901, proszę o odpowiedź.",
            ["cokolwiek"] = "x",
        };
        var errors = SubmissionValidator.Validate(TemplateCatalog.Ezus, fields).Errors;

        Assert.Contains("fields.tresc", errors.Keys);
        Assert.Contains("fields.temat", errors.Keys);
        Assert.Contains("fields.cokolwiek", errors.Keys);
        Assert.Contains("PESEL", errors["fields.tresc"][0]);

        var badFormat = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Ojcowski)) { ["ojcowskiOd"] = "15.02.2027" };
        Assert.Contains("fields.ojcowskiOd", SubmissionValidator.Validate(TemplateCatalog.Paternity, badFormat).Errors.Keys);

        var reversed = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Ojcowski)) { ["ojcowskiDo"] = "2027-02-01" };
        Assert.Contains("fields.ojcowskiDo", SubmissionValidator.Validate(TemplateCatalog.Paternity, reversed).Errors.Keys);

        var noName = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Uzupelniajacy));
        noName.Remove("imieNazwisko");
        noName["drugiRodzicBrakZamiaru"] = "nie";
        var missing = SubmissionValidator.Validate(TemplateCatalog.Supplementary, noName).Errors;
        Assert.Contains("fields.imieNazwisko", missing.Keys);
        Assert.Contains("fields.drugiRodzicBrakZamiaru", missing.Keys);
    }

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public void Samples_pass_leave_limits(string templateId)
    {
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        Assert.True(SubmissionValidator.Validate(spec, Samples.Fields(templateId), Domain.Amounts.AmountsKit.Params()).IsValid);
    }

    [Theory]
    [InlineData("2027-02-15", "2027-03-07", null)] // 3 tygodnie
    [InlineData("2027-02-15", "2027-02-24", null)] // 10 dni — nie pełne tygodnie
    [InlineData("2027-02-15", "2027-02-28", "tak")] // 2 tygodnie, choć 1 tydzień już wykorzystany
    public void Paternity_leave_over_the_limit_or_not_in_whole_weeks_is_rejected(string from, string to, string? usedBefore)
    {
        var fields = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Ojcowski)) { ["ojcowskiOd"] = from, ["ojcowskiDo"] = to };
        if (usedBefore is not null)
        {
            fields["korzystalZUrlopuOjcowskiego"] = usedBefore;
            fields["urlopOjcowskiOpis"] = "1 tydzień w styczniu 2027";
        }

        var errors = SubmissionValidator.Validate(TemplateCatalog.Paternity, fields, Domain.Amounts.AmountsKit.Params()).Errors;
        Assert.Contains("KP art. 182³", Assert.Single(errors["fields.ojcowskiDo"]));
    }

    [Fact]
    public void Paternity_second_week_after_first_is_accepted()
    {
        var fields = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Ojcowski))
        {
            ["ojcowskiOd"] = "2027-02-15",
            ["ojcowskiDo"] = "2027-02-21",
            ["korzystalZUrlopuOjcowskiego"] = "tak",
            ["urlopOjcowskiOpis"] = "1 tydzień w styczniu 2027",
        };

        Assert.True(SubmissionValidator.Validate(TemplateCatalog.Paternity, fields, Domain.Amounts.AmountsKit.Params()).IsValid);
    }

    [Fact]
    public void Parental_sixth_part_and_60_weeks_for_one_parent_are_rejected()
    {
        var parameters = Domain.Amounts.AmountsKit.Params();
        var sixth = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Rodzicielski)) { ["liczbaWykorzystanychCzesci"] = "5" };
        Assert.Contains("6. częścią", SubmissionValidator.Validate(TemplateCatalog.Parental, sixth, parameters).Errors["fields.liczbaWykorzystanychCzesci"][0]);

        var tooLong = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Rodzicielski))
        {
            ["lacznieZPraca"] = "nie",
            ["rodzicielskiOd"] = "2027-08-02",
            ["rodzicielskiDo"] = "2028-09-24", // 60 tygodni
        };
        Assert.Contains("wyłącznie drugiemu rodzicowi", SubmissionValidator.Validate(TemplateCatalog.Parental, tooLong, parameters).Errors["fields.rodzicielskiDo"][0]);

        tooLong["zaZyciem"] = "tak"; // „Za życiem”: 67 − 9 = 58 tyg. dla jednego rodzica — 60 nadal za dużo
        Assert.False(SubmissionValidator.Validate(TemplateCatalog.Parental, tooLong, parameters).IsValid);
        tooLong["rodzicielskiDo"] = "2028-08-27"; // 56 tygodni
        Assert.True(SubmissionValidator.Validate(TemplateCatalog.Parental, tooLong, parameters).IsValid);
    }

    [Fact]
    public void Conditional_fields_are_required_only_when_their_condition_holds()
    {
        var fields = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.Rodzicielski));
        fields.Remove("wymiarCzasuPracy");
        fields.Remove("dataPrzyjeciaDziecka");

        var errors = SubmissionValidator.Validate(TemplateCatalog.Parental, fields).Errors;
        Assert.Contains("fields.wymiarCzasuPracy", errors.Keys);
        Assert.Contains("fields.dataPrzyjeciaDziecka", errors.Keys);

        fields["lacznieZPraca"] = "nie";
        fields["dzieckoPrzyjete"] = "nie";
        Assert.True(SubmissionValidator.Validate(TemplateCatalog.Parental, fields).IsValid);
    }

    [Fact]
    public void Summary_has_no_personal_data()
    {
        foreach (var spec in TemplateCatalog.All)
        {
            var summary = DocumentBuilder.Summarize(spec, Samples.Fields(spec.Id));
            Assert.DoesNotContain("Ćwikła", summary);
            Assert.DoesNotContain("Wąsowski", summary);
            Assert.False(string.IsNullOrWhiteSpace(summary));
        }
    }
}
