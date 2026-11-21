using System.Net.Http.Json;
using BabyRadnik.Infrastructure.Assistant;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BabyRadnik.Tests.Api;

public sealed class LegalSearchTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("Ile trwa urlop macierzyński przy bliźniętach?", "Kodeks pracy", "Art. 180")]
    [InlineData("Czy ojciec może wziąć urlop rodzicielski, gdy matka nie pracuje?", "Kodeks pracy", "Art. 182¹a")]
    [InlineData("Do kiedy trzeba wykorzystać urlop ojcowski?", "Kodeks pracy", "Art. 182³")]
    [InlineData("Co to jest okres wyczekiwania?", "Ustawa zasiłkowa", "Art. 4")]
    [InlineData("Czy na umowie zlecenia dostanę zasiłek macierzyński?", "Poradnik ZUS", "Zleceniobiorcy")]
    [InlineData("Co się dzieje z zasiłkiem, gdy mam zaległe składki?", "Ustawa zasiłkowa", "Art. 2a")]
    [InlineData("Czy można pracować na urlopie rodzicielskim?", "Kodeks pracy", "Art. 182¹e")]
    [InlineData("Ile wynosi zasiłek macierzyński?", "Ustawa zasiłkowa", "Art. 31")]
    [InlineData("Co po martwym urodzeniu?", "Kodeks pracy", "Art. 180¹")]
    [InlineData("Kto wypłaca zasiłek, pracodawca czy ZUS?", "Ustawa zasiłkowa", "Art. 61")]
    [InlineData("Jakie dokumenty do zasiłku macierzyńskiego?", "Rozporządzenie w sprawie dokumentów do zasiłków", "§")]
    [InlineData("Czy rodzina zastępcza ma prawo do zasiłku?", "Ustawa zasiłkowa", "Art. 29")]
    [InlineData("Co gdy umowa kończy się w ciąży?", "Kodeks pracy", "Art. 177")]
    public async Task Relevant_section_appears_in_first_three_results(string question, string document, string article)
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var answer = await client.GetFromJsonAsync<AssistantAnswer>($"/api/law/search?q={Uri.EscapeDataString(question)}", ct);
        Assert.NotNull(answer);
        Assert.Equal("search", answer.Mode);
        Assert.True(answer.Citations.Take(3).Any(citation =>
            citation.Document.StartsWith(document, StringComparison.Ordinal) && citation.Article.StartsWith(article, StringComparison.Ordinal)),
            $"Oczekiwano: {document}, {article}. Wyniki: {string.Join(" | ", answer.Citations.Select(c => $"{c.Document}, {c.Article}"))}");
    }
}
