using System.Text.Json;
using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed class Persona
{
    public required string Id { get; init; }
    public required string Opis { get; init; }
    public required string Status { get; init; }
    public string? Sprawa { get; init; }
    public required Identity Tozsamosc { get; init; }
    public string? DrugiRodzic { get; init; }
    public FactSet Fakty { get; init; } = new();
}

public sealed partial class PersonaStore
{
    private readonly IReadOnlyDictionary<string, Persona> _byId;
    private readonly IReadOnlyDictionary<string, int> _payerHeadcount;

    private PersonaStore(IReadOnlyDictionary<string, Persona> byId)
    {
        _byId = byId;
        _payerHeadcount = IndexPayers(byId.Values);
    }

    public IReadOnlyList<Persona> All => _byId.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToList();

    public Persona? Find(string? id) => id is not null && _byId.TryGetValue(id, out var p) ? p : null;

    public int? PayerHeadcount(string payerId) => _payerHeadcount.TryGetValue(payerId, out var n) ? n : null;

    public static PersonaStore Load(RepoPaths paths, ILogger logger)
    {
        var byId = new Dictionary<string, Persona>(StringComparer.Ordinal);
        foreach (var dir in PersonaDirectories(paths))
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.json").Order(StringComparer.Ordinal))
            {
                var persona = TryRead(file, logger);
                if (persona is null || persona.Status != "gotowe")
                {
                    continue;
                }

                if (!byId.TryAdd(persona.Id, persona))
                {
                    logger.LogWarning("Pominięto osobę o powtórzonym id {PersonaId} z pliku {File}.", persona.Id, Path.GetFileName(file));
                }
            }
        }

        return new PersonaStore(byId);
    }

    private static IEnumerable<string> PersonaDirectories(RepoPaths paths)
    {
        var builtIn = Path.Combine(paths.Fixtures, "personas");
        if (Directory.Exists(builtIn))
        {
            yield return builtIn;
        }

        var privateDir = paths.CasesPrivateDir;
        if (privateDir is null || !Directory.Exists(privateDir))
        {
            yield break;
        }

        var nested = Path.Combine(privateDir, "personas");
        yield return Directory.Exists(nested) ? nested : privateDir;
    }

    private static Persona? TryRead(string file, ILogger logger)
    {
        try
        {
            var persona = JsonSerializer.Deserialize<Persona>(File.ReadAllText(file), SimJson.Options);
            var reason = persona is null ? "pusty plik" : Validate(persona);
            if (reason is null)
            {
                return persona;
            }

            logger.LogWarning("Pominięto plik osoby {File}: {Reason}.", Path.GetFileName(file), reason);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Pominięto plik osoby {File}: nie udało się go odczytać.", Path.GetFileName(file));
        }

        return null;
    }

    private static string? Validate(Persona p)
    {
        if (!IdPattern().IsMatch(p.Id))
        {
            return "nieprawidłowe id";
        }

        if (!PeselPattern().IsMatch(p.Tozsamosc.Pesel))
        {
            return "nieprawidłowy format PESEL";
        }

        return PeselChecksumValid(p.Tozsamosc.Pesel) ? "PESEL przechodzi sumę kontrolną (musi być fikcyjny)" : null;
    }

    public static bool PeselChecksumValid(string pesel)
    {
        ReadOnlySpan<int> weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += weights[i] * (pesel[i] - '0');
        }

        return (10 - sum % 10) % 10 == pesel[10] - '0';
    }

    private static Dictionary<string, int> IndexPayers(IEnumerable<Persona> personas)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var persona in personas.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            var headcount = persona.Fakty.Wnioskodawca.PlatnikLiczbaUbezpieczonych?.Value;
            var payerId = PersonaFacts.MainTitle(persona)?.PlatnikId;
            if (headcount is not null && payerId is not null)
            {
                index.TryAdd(payerId, headcount.Value);
            }
        }

        return index;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex PeselPattern();
}

public static class PersonaFacts
{
    public static DateOnly? EventDate(Persona persona) => persona.Fakty.Sprawa.ZdarzenieData?.Value;

    public static InsuranceTitle? MainTitle(Persona persona) =>
        MainTitle(persona.Fakty.Wnioskodawca.UbezpieczenieTytuly?.Value, EventDate(persona));

    public static InsuranceTitle? MainTitle(IReadOnlyList<InsuranceTitle>? titles, DateOnly? date)
    {
        if (titles is null || titles.Count == 0)
        {
            return null;
        }

        var active = date is null
            ? titles.Where(t => t.Do is null)
            : titles.Where(t => t.Od <= date && (t.Do is null || t.Do >= date));
        return active.OrderByDescending(t => t.Od).FirstOrDefault() ?? titles.OrderByDescending(t => t.Od).First();
    }
}
