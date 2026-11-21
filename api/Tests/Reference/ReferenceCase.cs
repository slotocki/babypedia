using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Tests.Reference;

/// <summary>Przypadek z fixtures/reference/*.json (schemat: contracts/reference.schema.json).</summary>
public sealed record ReferenceCase(
    string File,
    string Id,
    string Opis,
    string Status,
    string Modul,
    int? Test,
    DateOnly? AsOf,
    FactSet? Fakty,
    IReadOnlyList<ScenarioChange> Scenariusze,
    IReadOnlyList<SeedRow> Parametry,
    JsonElement? Oczekiwane)
{
    public bool DoUzupelnienia => Status == "do_uzupelnienia";

    public IParameterProvider Parameters() => SeedParameterProvider.Load(Parametry);

    public AssessmentRequest Request() =>
        new(Fakty ?? throw new InvalidOperationException($"{Id}: brak faktów."), AsOf);

    public static ReferenceCase Load(string path)
    {
        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        var root = doc.RootElement;
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        T? Get<T>(string name) => root.TryGetProperty(name, out var v) ? v.Deserialize<T>(ApiJson.Options) : default;

        return new ReferenceCase(
            path,
            Str("id") ?? Path.GetFileNameWithoutExtension(path),
            Str("opis") ?? "",
            Str("status") ?? "gotowe",
            Str("modul") ?? "",
            root.TryGetProperty("test", out var t) ? t.GetInt32() : null,
            Str("asOf") is { } asOf ? DateOnly.Parse(asOf) : null,
            Get<FactSet>("fakty"),
            Get<List<ScenarioChange>>("scenariusze") ?? [],
            Get<List<SeedRow>>("parametry") ?? [],
            root.TryGetProperty("oczekiwane", out var o) ? o.Clone() : null);
    }
}
