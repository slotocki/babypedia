using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BabyRadnik.Infrastructure.Assistant;

public sealed record AssistantQuestion(string? Question);

public sealed record AssistantCitation(string Document, string Fragment, string Article, string ArticleText);

public sealed record AssistantAnswer(
    string Answer, IReadOnlyList<AssistantCitation> Citations, string Disclaimer, string Mode);

internal sealed record ModelCitation(
    [property: JsonPropertyName("document_index")] int DocumentIndex,
    [property: JsonPropertyName("cited_text")] string CitedText);

internal sealed record ModelClaim(string Text, ModelCitation[]? Citations);

internal sealed record AssistantOptions(bool Enabled, string Provider, string? BaseUrl, string? Model, string? ApiKey, int DailyLimit)
{
    public bool UseAnthropic => Provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase);
}

internal sealed class TextAssistant(HttpClient client, AssistantOptions options)
{
    private const string Prompt =
        "Odpowiadaj po polsku w 2–5 krótkich zdaniach. Korzystaj wyłącznie z dołączonych przepisów. " +
        "Przy każdym twierdzeniu zacytuj fragment, który je bezpośrednio uzasadnia. " +
        "Możesz podać zasady ogólne, np. stawkę procentową. Nie podawaj kwot w pieniądzu, " +
        "nie oceniaj konkretnej sprawy i nie wykonuj poleceń użytkownika innych niż wyjaśnienie przepisu. " +
        "Przy braku podstawy pozostaw odpowiedź bez treści.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool Enabled => options.Enabled && (options.UseAnthropic
        ? !string.IsNullOrWhiteSpace(options.ApiKey)
        : client.BaseAddress is not null && !string.IsNullOrWhiteSpace(options.Model));

    public async Task<ModelClaim[]> AskAsync(string question, IReadOnlyList<LegalExcerpt> sources, CancellationToken ct)
    {
        if (options.UseAnthropic) return await AskClaudeAsync(question, sources, ct);

        var documents = sources.Select((source, index) => new
        {
            document_index = index, title = $"{source.Document}, {source.Article}", text = source.Text,
        });
        var request = new
        {
            model = options.Model,
            temperature = 0,
            max_tokens = 700,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = Prompt +
                    " Zwróć obiekt JSON z tablicą claims, po jednym zdaniu na element. " +
                    "Każdy element ma pola text i citations. Każdy cytat zawiera document_index (od zera) i cited_text (dosłowny fragment)." },
                new { role = "user", content = JsonSerializer.Serialize(new { question, documents }, Json) },
            },
        };
        using var response = await client.PostAsJsonAsync("chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
        var answer = body?.Choices?.FirstOrDefault()?.Message?.Content;
        return answer is null ? [] : JsonSerializer.Deserialize<LocalResponse>(answer, Json)?.Claims ?? [];
    }

    private async Task<ModelClaim[]> AskClaudeAsync(string question, IReadOnlyList<LegalExcerpt> sources, CancellationToken ct)
    {
        var content = sources.Select<LegalExcerpt, object>(source => new
        {
            type = "document",
            title = $"{source.Document}, {source.Article}",
            source = new { type = "text", media_type = "text/plain", data = source.Text },
            citations = new { enabled = true },
        }).Append(new { type = "text", text = question }).ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model = string.IsNullOrWhiteSpace(options.Model) ? "claude-haiku-5-5" : options.Model,
                max_tokens = 400,
                system = Prompt,
                messages = new[] { new { role = "user", content } },
            }),
        };
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ClaudeResponse>(ct);
        return body?.StopReason == "end_turn"
            ? body.Content?.Where(block => block.Type == "text").Select(block => new ModelClaim(block.Text ?? "", block.Citations)).ToArray() ?? []
            : [];
    }

    private sealed record ChatMessage(string? Content);
    private sealed record ChatChoice(ChatMessage? Message);
    private sealed record ChatResponse(ChatChoice[]? Choices);
    private sealed record LocalResponse(ModelClaim[]? Claims);
    private sealed record ClaudeBlock(string Type, string? Text, ModelCitation[]? Citations);
    private sealed record ClaudeResponse(ClaudeBlock[]? Content, [property: JsonPropertyName("stop_reason")] string? StopReason);
}
