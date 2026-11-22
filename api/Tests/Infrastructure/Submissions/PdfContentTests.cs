using System.Globalization;
using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Infrastructure.Submissions;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

/// <summary>
/// Treść PDF-ów: bez śladów programu („null”, nierozwiązane znaczniki), bez dat spoza danych wniosku,
/// z wyraźnym pustym polem tam, gdzie brakuje danych, i bez podpowiedzi formularza w wydruku.
/// </summary>
public sealed partial class PdfContentTests
{
    private static readonly DateOnly DocDate = new(2026, 11, 21);
    private static readonly IParameterProvider Seed = SeedParameterProvider.Load();

    private static (string Text, DocumentModel Model) Render(TemplateSpec spec, IReadOnlyDictionary<string, string> fields, IParameterProvider? parameters = null)
    {
        Assert.True(SubmissionValidator.Validate(spec, fields).IsValid, $"Dane {spec.Id} powinny być poprawne.");
        var model = DocumentBuilder.Build(spec, fields, "BR-SYM-TEST-20261121-ABCD2345", DocDate, parameters ?? Seed);
        return (PdfText.Extract(PdfRenderer.Render(model)), model);
    }

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public void Pdf_has_no_program_artifacts_or_form_hints(string templateId)
    {
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        var (text, _) = Render(spec, Samples.Fields(templateId));

        Assert.DoesNotMatch(ProgramArtifacts(), text);
        Assert.DoesNotContain("{", text);
        Assert.DoesNotContain("}", text);
        Assert.DoesNotContain("nie podano", text);
        foreach (var hint in spec.Fields.Where(f => f.Hint is not null).Select(f => f.Hint!))
        {
            Assert.DoesNotContain(PdfText.Compact(hint), PdfText.Compact(text));
        }

        Assert.DoesNotContain("wypełnij tylko", text);
    }

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public void Every_date_in_the_pdf_comes_from_the_fields_the_document_date_or_a_cited_regulation(string templateId)
    {
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        var fields = Samples.Fields(templateId);
        var (text, _) = Render(spec, fields);

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            DocumentBuilder.FormatDate(DocDate),
            "11.03.2025", // data rozporządzenia w sprawie wniosków pracowniczych (cytowana w podstawach)
            "27.09.2026", // data rozporządzenia w sprawie dokumentów do zasiłków
        };
        foreach (var value in fields.Values)
        {
            if (SubmissionValidator.TryParseDate(value, out var d))
            {
                allowed.Add(DocumentBuilder.FormatDate(d));
            }
        }

        var printed = DottedDate().Matches(PdfText.Compact(text)).Select(m => m.Value).ToList();
        Assert.NotEmpty(printed);
        Assert.All(printed, date => Assert.Contains(date, allowed));
        Assert.All(fields.Where(f => spec.Fields.Single(s => s.Key == f.Key).Kind == FieldKind.Date),
            f => PdfText.AssertHas(text, DocumentBuilder.FormatDate(DateOnly.ParseExact(f.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture))));
    }

    [Fact]
    public void Leave_request_without_employer_and_place_leaves_blank_lines_to_fill_by_hand()
    {
        var fields = new Dictionary<string, string>(Samples.Fields(LeaveTemplateIds.MacierzynskiIRodzicielski));
        fields.Remove("pracodawca");
        fields.Remove("miejscowosc");
        var (text, _) = Render(TemplateCatalog.FullLeave, fields);

        PdfText.AssertHas(text, "Miejscowość i data: ____________________, 21.11.2026");
        PdfText.AssertHas(text, "Do: ________________________________________");
        PdfText.AssertHas(text, "(nazwa i adres pracodawcy)");
        PdfText.AssertHas(text, "podpis wnioskodawcy");
    }

    [Fact]
    public void Leave_request_prints_employer_and_place_and_regulation_labels_without_form_hints()
    {
        var (text, model) = Render(TemplateCatalog.FullLeave, Samples.Fields(LeaveTemplateIds.MacierzynskiIRodzicielski));

        PdfText.AssertHas(text, "Miejscowość i data: Łódź, 21.11.2026");
        PdfText.AssertHas(text, "Do: Zażółć Gęślą Jaźń sp. z o.o.");
        Assert.Contains(model.Sections.SelectMany(s => s.Rows),
            r => r.Label == "Data, od której ma być udzielona część urlopu macierzyńskiego przed porodem" && r.Value == "04.01.2027");
        Assert.DoesNotContain("wpisz„brak”", PdfText.Compact(text));
    }

    [Fact]
    public void Zam_without_data_shows_blank_fields_instead_of_values()
    {
        var fields = new Dictionary<string, string>
        {
            ["imieNazwisko"] = Samples.Name,
            ["rodzajSwiadczenia"] = "macierzynski",
            ["okresOd"] = "2027-02-11",
            ["okresDo"] = "2027-07-31",
            ["podstawaWniosku"] = "urodzenie",
        };
        var (text, model) = Render(TemplateCatalog.Zam, fields);

        var rows = model.Sections.SelectMany(s => s.Rows).ToList();
        Assert.Equal(TemplateCatalog.Zam.Fields.Count(), rows.Count);
        Assert.Equal(TemplateCatalog.Zam.Fields.Count() - fields.Count, rows.Count(r => r.ToFill));
        Assert.All(rows.Where(r => r.ToFill), r => Assert.Equal(DocumentBuilder.ToFillText, r.Value));
        PdfText.AssertHas(text, "____________________ do uzupełnienia");
        PdfText.AssertHas(text, "11.02.2027");
        PdfText.AssertHas(text, "31.07.2027");
        Assert.DoesNotContain("nie podano", text);
    }

    [Fact]
    public void Zam_payer_note_takes_the_threshold_from_parameters_and_cites_article_61()
    {
        var threshold = Seed.GetInt(EligibilityParameterKeys.ProgPlatnikaUbezpieczonych, DocDate);
        var (text, model) = Render(TemplateCatalog.Zam, Samples.Fields("dane_zam"));

        PdfText.AssertHas(text, $"powyżej {threshold} ubezpieczonych");
        Assert.Contains(model.Notes, n => n.Basis == "art. 61 ust. 1 ustawy zasiłkowej");

        var withoutParameter = DocumentBuilder.Build(TemplateCatalog.Zam, Samples.Fields("dane_zam"), "BR-SYM-TEST", DocDate, SeedParameterProvider.Load().Without());
        var note = Assert.Single(withoutParameter.Notes, n => n.Text.Contains("ubezpieczenia chorobowego powyżej"));
        Assert.DoesNotContain("{", note.Text);
        Assert.DoesNotMatch(@"powyżej \d", note.Text);
    }

    [Fact]
    public void Part_time_note_appears_only_when_combining_leave_with_work()
    {
        var with = Render(TemplateCatalog.Parental, Samples.Fields(LeaveTemplateIds.Rodzicielski)).Model;
        Assert.Contains(with.Notes, n => n.Text.StartsWith("Wniosek o łączenie urlopu rodzicielskiego z pracą"));

        var fields = Samples.Fields(LeaveTemplateIds.Rodzicielski)
            .Where(f => f.Key is not ("lacznieZPraca" or "wymiarCzasuPracy" or "laczenieOd" or "laczenieDo" or "laczenieCzescWydluzona"))
            .ToDictionary();
        var (text, without) = Render(TemplateCatalog.Parental, fields);
        Assert.DoesNotContain(without.Notes, n => n.Text.StartsWith("Wniosek o łączenie urlopu rodzicielskiego z pracą"));
        Assert.DoesNotContain("Łączenie urlopu rodzicielskiego z pracą", text);
    }

    [Fact]
    public void Sections_without_any_regulation_field_have_no_basis_column()
    {
        var ezus = Render(TemplateCatalog.Ezus, Samples.Fields("zapytanie_ezus")).Model;
        Assert.All(ezus.Sections.SelectMany(s => s.Rows), r => Assert.Null(r.Basis));

        var leave = Render(TemplateCatalog.FullLeave, Samples.Fields(LeaveTemplateIds.MacierzynskiIRodzicielski)).Model;
        var birth = leave.Sections.SelectMany(s => s.Rows).Single(r => r.Label.StartsWith("Przewidywana data porodu"));
        Assert.Equal(DocumentBuilder.AuxBasisShort, birth.Basis);
    }

    [Fact]
    public void Birth_certificate_request_has_a_signature_line()
    {
        var (text, model) = Render(TemplateCatalog.BirthCertificate, Samples.Fields("odpis_aktu"));

        Assert.True(model.SignatureLine);
        PdfText.AssertHas(text, "podpis wnioskodawcy");
        PdfText.AssertHas(text, "Data sporządzenia: 21.11.2026");
        Assert.DoesNotContain("Miejsce i data", text);
    }

    [Fact]
    public void Form_labels_keep_hints_for_the_web_form()
    {
        var used = TemplateCatalog.FullLeave.Fields.Single(f => f.Key == "dotychczasWykorzystano");
        Assert.EndsWith("(wpisz „brak”, jeśli nie korzystano)", used.FormLabel);
        Assert.DoesNotContain("wpisz", used.Label);
        Assert.Equal("Treść zapytania (bez numeru PESEL)", TemplateCatalog.Ezus.Fields.Single(f => f.Key == "tresc").FormLabel);
    }

    [Fact]
    public void Debt_certificate_says_when_the_balance_is_only_known_for_an_earlier_day()
    {
        var model = DebtCertificate.Build("Edyta Pokazowa", new ContributionDebt(new DateOnly(2026, 11, 20), 120m),
            new DateOnly(2027, 5, 5), DocDate, "BR-SYM-ZAS-20261121-ABCD2345");
        var text = PdfText.Extract(PdfRenderer.Render(model));

        PdfText.AssertHas(text, "Stan na dzień 05.05.2027");
        PdfText.AssertHas(text, "Saldo znane według stanu konta na dzień 20.11.2026");
        PdfText.AssertHas(text, "Symulowane zadłużenie składkowe według stanu na 20.11.2026.");
        PdfText.AssertHas(text, "120,00 zł");
        PdfText.AssertHas(text, "Data wystawienia: 21.11.2026");
        PdfText.AssertHas(text, "Nr dokumentu: BR-SYM-ZAS-20261121-ABCD2345");
        Assert.DoesNotContain("na wskazany dzień", text);
        Assert.DoesNotMatch(ProgramArtifacts(), text);
    }

    [Fact]
    public void Debt_certificate_for_the_day_the_balance_is_known_has_no_staleness_note()
    {
        var day = new DateOnly(2026, 11, 20);
        var model = DebtCertificate.Build("Edyta Pokazowa", new ContributionDebt(day, 0m), day, DocDate, "BR-SYM-ZAS-TEST");
        var text = PdfText.Extract(PdfRenderer.Render(model));

        PdfText.AssertHas(text, "Brak symulowanego zadłużenia składkowego na wskazany dzień.");
        PdfText.AssertHas(text, "0,00 zł");
        Assert.DoesNotContain("Saldo znane", text);
        Assert.Equal(2, model.Notes.Count);
    }

    [GeneratedRegex(@"\b(null|undefined|NaN|True|False|true|false)\b|System\.")]
    private static partial Regex ProgramArtifacts();

    [GeneratedRegex(@"\d{2}\.\d{2}\.\d{4}")]
    private static partial Regex DottedDate();
}

internal static class ParameterProviderExtensions
{
    /// <summary>Dostawca bez progu płatnika z art. 61 — do sprawdzenia, że brak parametru nie daje zgadniętej liczby.</summary>
    public static IParameterProvider Without(this IParameterProvider inner) => new MissingThreshold(inner);

    private sealed class MissingThreshold(IParameterProvider inner) : IParameterProvider
    {
        public Parameter Get(string key, DateOnly date) =>
            key == EligibilityParameterKeys.ProgPlatnikaUbezpieczonych
                ? throw new ParameterNotFoundException(key, date)
                : inner.Get(key, date);
    }
}
