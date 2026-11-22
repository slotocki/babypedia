using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BabyRadnik.Infrastructure.Assistant;
using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BabyRadnik.Tests.Infrastructure.Assistant;

public sealed partial class FaqTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string LawDirectory = Path.Combine(TestPaths.Root, "docs", "prawo");

    private static readonly Lazy<FaqCatalog> Catalog = new(() =>
    {
        var paths = new RepoPaths { Root = TestPaths.Root };
        return new FaqCatalog(paths, new LegalSearch(paths));
    });

    private static readonly Dictionary<string, string> Files = LegalSources.Files.ToDictionary(
        file => file.Title, file => Path.Combine(LawDirectory, file.File));

    [Fact]
    public void Faq_has_enough_unique_questions()
    {
        var entries = Catalog.Value.Entries;
        Assert.True(entries.Count >= 45, $"Pozycji w FAQ: {entries.Count}");
        Assert.Equal(entries.Count, entries.Select(entry => entry.Id).Distinct().Count());
        var normalized = entries.Select(entry => AssistantPolicy.Normalize(entry.Question)).ToArray();
        Assert.Equal(normalized.Length, normalized.Distinct().Count());
        // Dwie pozycje o tej samej treści odpowiedzi to duplikat pod inną nazwą.
        Assert.Equal(entries.Count, entries.Select(entry => AssistantPolicy.Normalize(entry.Answer)).Distinct().Count());
        Assert.InRange(entries.Count(entry => entry.Starter), 4, 6);
    }

    [Fact]
    public void Every_quote_appears_verbatim_in_its_source()
    {
        // Katalog odrzuca źródło, którego cytatu nie ma dosłownie w wyciągu (po ujednoliceniu białych znaków).
        Assert.True(Catalog.Value.Problems.Count == 0, string.Join(" | ", Catalog.Value.Problems));

        foreach (var entry in Catalog.Value.Entries)
            foreach (var source in entry.Sources)
            {
                var raw = AssistantPolicy.CollapseSpace(File.ReadAllText(Files[source.Document]));
                var quote = AssistantPolicy.CollapseSpace(source.Quote.Trim());
                Assert.True(
                    raw.Contains(quote, StringComparison.Ordinal) ||
                    LegalNumbering.Flatten(raw).Contains(LegalNumbering.Flatten(quote), StringComparison.Ordinal),
                    $"{entry.Id}: cytat nie występuje w pliku {source.Document}: {source.Quote}");
            }
    }

    [Fact]
    public void Answers_are_short_plain_and_without_amounts()
    {
        foreach (var entry in Catalog.Value.Entries)
        {
            Assert.InRange(entry.Sources.Length, 1, 2);
            Assert.InRange(entry.Answer.Length, 20, 420);
            Assert.InRange(Sentence().Matches(entry.Answer).Count, 1, 4);
            Assert.False(Money().IsMatch(AssistantPolicy.Normalize(entry.Answer)), $"{entry.Id}: kwota w odpowiedzi");
            Assert.False(AssistantPolicy.HasSensitiveData(entry.Answer), entry.Id);
            Assert.True(entry.Alternatives.Length >= 2, $"{entry.Id}: za mało parafraz");
        }
    }

    [Theory]
    [InlineData("Ile trwa urlop ojcowski?", "ojcowski-ile")]
    [InlineData("jak długo trwa urlop dla taty", "ojcowski-ile")]
    [InlineData("Czy mogę pracować na urlopie rodzicielskim?", "rodzicielski-praca")]
    [InlineData("Czy można łączyć pracę z urlopem rodzicielskim?", "rodzicielski-praca")]
    [InlineData("Jakie dokumenty muszę złożyć do zasiłku macierzyńskiego?", "dokumenty-macierzynski")]
    [InlineData("Jakie dokumenty są potrzebne do zasiłku macierzyńskiego?", "dokumenty-macierzynski")]
    [InlineData("Kto wypłaca zasiłek macierzyński, ZUS czy pracodawca?", "kto-wyplaca")]
    [InlineData("Ile procent wynosi zasiłek macierzyński?", "wysokosc-zasilku")]
    [InlineData("Ile urlopu macierzyńskiego przy bliźniakach?", "blizniaki")]
    [InlineData("Do kiedy trzeba wykorzystać urlop ojcowski?", "ojcowski-termin")]
    [InlineData("Czy zaległości w składkach wpływają na zasiłek?", "zadluzenie")]
    [InlineData("Co z zasiłkiem, gdy dziecko urodzi się martwe?", "martwe-urodzenie")]
    [InlineData("Czy na zleceniu można dostać zasiłek macierzyński?", "zlecenie")]
    [InlineData("Jak złożyć wniosek o urlop rodzicielski?", "rodzicielski-wniosek")]
    [InlineData("Ile dni zasiłku opiekuńczego przysługuje w roku?", "opiekunczy-dni")]
    [InlineData("Co przysługuje przy wcześniaku?", "wczesniak")]
    [InlineData("Czy przy adopcji przysługuje zasiłek?", "adopcja")]
    [InlineData("Ile trwa urlop macierzyński?", "macierzynski-ile-trwa")]
    [InlineData("Jak długo można być na L4 w ciąży?", "chorobowe-ciaza-okres")]
    [InlineData("Co to jest okres wyczekiwania?", "wyczekiwanie")]
    [InlineData("Co mam zanieść pracodawcy?", "co-zanies-pracodawcy")]
    [InlineData("Jakie wnioski złożyć u pracodawcy po porodzie?", "co-zanies-pracodawcy")]
    [InlineData("Czy pracodawca może mnie zwolnić w ciąży?", "ochrona-zwolnienie")]
    public void Paraphrases_find_the_right_entry(string question, string expectedId)
    {
        var match = Catalog.Value.Match(question);
        Assert.True(match is not null, $"Brak trafienia dla „{question}”");
        Assert.Equal(expectedId, match.Entry.Id);
    }

    [Theory]
    [InlineData("Jaka jest dziś pogoda?")]
    [InlineData("Ile kosztuje wózek dla dziecka?")]
    [InlineData("Urlop rodzicielski")]
    [InlineData("zasiłek macierzyński")]
    [InlineData("Jak założyć konto w banku?")]
    [InlineData("Kto wygrał wczoraj mecz?")]
    [InlineData("Jak napisać CV?")]
    [InlineData("Jaki jest wymiar urlopu wypoczynkowego?")]
    [InlineData("Ile dni urlopu na żądanie przysługuje pracownikowi?")]
    [InlineData("Jak ZUS liczy emeryturę?")]
    [InlineData("Czy mogę zmienić pracodawcę w czasie ciąży i co z kredytem?")]
    public void Unrelated_or_topic_only_questions_are_not_answered_from_faq(string question) =>
        Assert.Null(Catalog.Value.Match(question));

    [Fact]
    public async Task Ask_endpoint_returns_faq_answer_without_model_and_keeps_safeguards()
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var faq = await Ask(client, "Ile trwa urlop ojcowski?", ct);
        Assert.Equal("faq", faq.Mode);
        Assert.Contains("2 tygodni", faq.Answer);
        Assert.NotEmpty(faq.Citations);
        Assert.All(faq.Citations, citation => Assert.Contains(citation.Fragment, citation.ArticleText));

        Assert.Equal("calculator", (await Ask(client, "Ile dostanę zasiłku macierzyńskiego przy pensji 6000?", ct)).Mode);
        Assert.Equal("calculator", (await Ask(client, "Czy dostanę zasiłek, zarabiam mało?", ct)).Mode);
        Assert.Equal("no_results", (await Ask(client, "Czy dostanę becikowe i 800+?", ct)).Mode);

        using var sensitive = await client.PostAsJsonAsync("/api/assistant/ask", new { question = "Ile trwa urlop ojcowski? mój mail jan@example.com" }, ct);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, sensitive.StatusCode);

        var suggestions = await client.GetFromJsonAsync<string[]>("/api/assistant/suggestions", ct);
        Assert.NotNull(suggestions);
        Assert.InRange(suggestions.Length, 4, 6);
        Assert.All(suggestions, suggestion => Assert.NotNull(Catalog.Value.Match(suggestion)));
    }

    private static async Task<AssistantAnswer> Ask(HttpClient client, string question, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AssistantAnswer>(ct))!;
    }

    [GeneratedRegex(@"[.!?](?:\s|$)")]
    private static partial Regex Sentence();

    [GeneratedRegex(@"\d\s*(?:zl\b|zlot\w*|pln\b)")]
    private static partial Regex Money();
}
