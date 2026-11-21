using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BabyRadnik.Infrastructure.Assistant;

/// <summary>
/// Opcjonalny pomocnik tekstowy (P2). Dostaje wyłącznie pytanie opisowe — bez danych osoby, bez faktów sprawy.
/// Nie podaje kwot ani decyzji (zasada 2.1, sekcja 3). Silnik (Domain) nie zależy od tego modułu.
/// </summary>
public interface ITextAssistant
{
    bool Enabled { get; }
    Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default);
}

public sealed record AssistantQuestion([property: JsonPropertyName("question")] string Question);

public sealed record AssistantCitation(
    [property: JsonPropertyName("document")] string Document,
    [property: JsonPropertyName("fragment")] string Fragment,
    [property: JsonPropertyName("article")] string? Article = null);

public sealed record AssistantAnswer(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("citations")] IReadOnlyList<AssistantCitation> Citations,
    [property: JsonPropertyName("disclaimer")] string Disclaimer);

public sealed class AssistantOptions
{
    public bool Enabled { get; init; }
    public string? BaseUrl { get; init; }
    public string? Model { get; init; }
}

public sealed class DisabledTextAssistant : ITextAssistant
{
    public bool Enabled => false;

    public Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default) =>
        throw new InvalidOperationException("Pomocnik tekstowy jest wyłączony.");
}

/// <summary>Klient endpointu zgodnego z OpenAI (Ollama, llama.cpp): POST {ASSISTANT_BASE_URL}/chat/completions.</summary>
public sealed class OpenAiCompatibleTextAssistant(HttpClient http, AssistantOptions options) : ITextAssistant
{
    public const string Disclaimer =
        "Odpowiedź ma charakter ogólnej informacji. Uprawnienia, okresy i kwoty wylicza wyłącznie kalkulator Kompas Rodzica.";

    private const string SystemPrompt =
        "Odpowiadasz po polsku na ogólne pytania o zasiłki z ubezpieczenia chorobowego i urlopy rodzicielskie. " +
        "Nie podajesz kwot, nie oceniasz uprawnień konkretnej osoby i nie wydajesz decyzji. " +
        "W takich przypadkach odsyłasz do kalkulatora Kompas Rodzica albo do ZUS.";

    public bool Enabled => true;

    public async Task<AssistantAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        var request = new ChatRequest(options.Model ?? "", [new("system", SystemPrompt), new("user", question)], 0);
        using var response = await http.PostAsJsonAsync("chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
        var text = body?.Choices.FirstOrDefault()?.Message.Content ?? "";
        return new AssistantAnswer(text.Trim(), [], Disclaimer);
    }

    private sealed record ChatMessage([property: JsonPropertyName("role")] string Role, [property: JsonPropertyName("content")] string Content);

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ChatChoice([property: JsonPropertyName("message")] ChatMessage Message);

    private sealed record ChatResponse([property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices);
}
