using System.Text.Json;
using BabyRadnik.Infrastructure.Common;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed record CaseSummary(string Id, string Tytul, string OczekiwanaDecyzja, string? Persona, bool Dostepna);

public sealed record CaseFile(string? Nazwa, int? LiczbaStron, IReadOnlyList<string>? Sha256);

public sealed record CaseEntry(string Id, string Tytul, string OczekiwanaDecyzja, string? Persona, CaseFile? Plik);

public sealed class CaseCatalog
{
    private readonly IReadOnlyList<CaseEntry> _cases;
    private readonly PersonaStore _personas;

    public CaseCatalog(IReadOnlyList<CaseEntry> cases, PersonaStore personas)
    {
        _cases = cases;
        _personas = personas;
    }

    public IReadOnlyList<CaseSummary> List() => _cases.Select(Summarize).ToList();

    public CaseSummary? FindByHash(string sha256, string? fileName, int? pages)
    {
        var byHash = _cases.FirstOrDefault(c => c.Plik?.Sha256?.Contains(sha256, StringComparer.Ordinal) == true);
        if (byHash is not null)
        {
            return Summarize(byHash);
        }

        if (string.IsNullOrWhiteSpace(fileName) || pages is null)
        {
            return null;
        }

        var byName = _cases.FirstOrDefault(c => c.Plik is { Nazwa: not null, LiczbaStron: not null }
            && string.Equals(c.Plik.Nazwa, fileName, StringComparison.OrdinalIgnoreCase)
            && c.Plik.LiczbaStron == pages);
        return byName is null ? null : Summarize(byName);
    }

    private CaseSummary Summarize(CaseEntry c) =>
        new(c.Id, c.Tytul, c.OczekiwanaDecyzja, c.Persona, c.Persona is not null && _personas.Find(c.Persona) is not null);

    public static CaseCatalog Load(RepoPaths paths, PersonaStore personas, ILogger logger)
    {
        var cases = new List<CaseEntry>();
        var files = new List<string> { Path.Combine(paths.Fixtures, "cases.json") };
        if (paths.CasesPrivateDir is not null)
        {
            files.Add(Path.Combine(paths.CasesPrivateDir, "cases.json"));
        }

        foreach (var file in files.Where(File.Exists))
        {
            foreach (var entry in Read(file, logger))
            {
                var index = cases.FindIndex(c => c.Id == entry.Id);
                if (index >= 0)
                {
                    cases[index] = entry;
                }
                else
                {
                    cases.Add(entry);
                }
            }
        }

        return new CaseCatalog(cases, personas);
    }

    private static IReadOnlyList<CaseEntry> Read(string file, ILogger logger)
    {
        try
        {
            return JsonSerializer.Deserialize<CaseList>(File.ReadAllText(file), SimJson.Options)?.Cases ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Pominięto plik spraw {File}: nie udało się go odczytać.", Path.GetFileName(file));
            return [];
        }
    }

    private sealed record CaseList(IReadOnlyList<CaseEntry>? Cases);
}

public sealed record Organization(
    string Id,
    string Nazwa,
    string Opis,
    IReadOnlyList<string> Sytuacje,
    string? Url,
    string? Telefon,
    bool Zweryfikowano);

public sealed class OrganizationCatalog(IReadOnlyList<Organization> all)
{
    public IReadOnlyList<Organization> Verified(string? situation) => all
        .Where(o => o.Zweryfikowano)
        .Where(o => string.IsNullOrEmpty(situation) || (o.Sytuacje ?? []).Contains(situation, StringComparer.OrdinalIgnoreCase))
        .ToList();

    public static OrganizationCatalog Load(RepoPaths paths, ILogger logger)
    {
        var file = Path.Combine(paths.Fixtures, "organizations.json");
        if (!File.Exists(file))
        {
            return new OrganizationCatalog([]);
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var list = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("organizations", out var wrapped) ? wrapped : default;
            var items = list.ValueKind == JsonValueKind.Array
                ? list.Deserialize<List<Organization>>(SimJson.Options) ?? []
                : [];
            return new OrganizationCatalog(items);
        }
        catch (JsonException)
        {
            logger.LogWarning("Pominięto fixtures/organizations.json: nieprawidłowy format.");
            return new OrganizationCatalog([]);
        }
    }
}
