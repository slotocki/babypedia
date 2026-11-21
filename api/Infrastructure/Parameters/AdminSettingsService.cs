using System.Text.Json;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Parameters;

public sealed record CatalogEntryDto(string? Id, string? Nazwa, bool? Wlaczone);

public sealed record IntegrationFieldDto(string? Source, string? Fact, bool? Wlaczone);

public sealed record OrganizationDto(string? Id, string? Nazwa, string? Opis, List<string>? Sytuacje, string? Url, string? Telefon, bool? Zweryfikowano);

public sealed record SettingsOutcome<T>(IReadOnlyList<T>? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Value is not null;
}

public sealed class AdminSettingsService(ISettingsStore store, TimeProvider time)
{
    private const string CatalogName = "catalog";
    private const string IntegrationsName = "integrations";
    private const string OrganizationsName = "organizations";
    private const int MaxOrganizations = 500;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static readonly IReadOnlyList<CatalogEntryDto> CatalogDefaults =
    [
        new("chorobowy_w_ciazy", "Zasiłek chorobowy w okresie ciąży", true),
        new("rehabilitacyjne", "Świadczenie rehabilitacyjne", true),
        new("zasilek_do_dnia_porodu", "Zasiłek w wysokości zasiłku macierzyńskiego (art. 30 ust. 3)", true),
        new("macierzynski", "Zasiłek macierzyński za okres urlopu macierzyńskiego", true),
        new("macierzynski_uzupelniajacy", "Zasiłek macierzyński za okres uzupełniającego urlopu macierzyńskiego", true),
        new("rodzicielski", "Zasiłek macierzyński za okres urlopu rodzicielskiego", true),
        new("ojcowski", "Zasiłek macierzyński za okres urlopu ojcowskiego", true),
        new("przyjecie_dziecka", "Zasiłek za okres urlopu na warunkach urlopu macierzyńskiego", true),
        new("przejecie_przez_ojca", "Przejęcie zasiłku w sytuacjach szczególnych matki", true),
        new("pogrzebowy", "Zasiłek pogrzebowy (informacja)", true),
    ];

    internal static readonly IReadOnlyList<IntegrationFieldDto> IntegrationDefaults =
    [
        new("mobywatel", FactKeys.OsobaDataUrodzenia, true),
        new("zus", FactKeys.UbezpieczenieTytuly, true),
        new("zus", FactKeys.UbezpieczeniePodstawySkladek, true),
        new("zus", FactKeys.UbezpieczeniePrzerwy, true),
        new("zus", FactKeys.UbezpieczeniePobraneSwiadczenia, true),
        new("zus", FactKeys.PlatnikLiczbaUbezpieczonych, true),
        new("zus", FactKeys.SkladkiZadluzenie, true),
        new("ezla", FactKeys.EzlaZwolnienia, true),
        new("usc", FactKeys.DzieckoDataUrodzenia, true),
        new("usc", FactKeys.DzieckoLiczba, true),
    ];

    private static readonly HashSet<string> Sources = ["mobywatel", "zus", "ezla", "usc", "user", "assumption"];

    public async Task<IReadOnlyList<CatalogEntryDto>> GetCatalogAsync(CancellationToken ct) =>
        Merge(CatalogDefaults, await ReadAsync<CatalogEntryDto>(CatalogName, ct), e => e.Id!);

    public async Task<SettingsOutcome<CatalogEntryDto>> PutCatalogAsync(List<CatalogEntryDto>? input, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (input is null)
        {
            errors["body"] = ["Oczekiwano tablicy wpisów katalogu."];
            return new(null, errors);
        }

        var known = CatalogDefaults.Select(d => d.Id!).ToHashSet();
        var seen = new HashSet<string>();
        for (var i = 0; i < input.Count; i++)
        {
            var e = input[i];
            if (e is null || e.Id is null || !known.Contains(e.Id))
            {
                errors[$"[{i}].id"] = ["Nieznany identyfikator świadczenia."];
            }
            else if (!seen.Add(e.Id))
            {
                errors[$"[{i}].id"] = ["Identyfikator występuje więcej niż raz."];
            }

            if (e is null || string.IsNullOrWhiteSpace(e.Nazwa) || e.Nazwa.Length > 200)
            {
                errors[$"[{i}].nazwa"] = ["Nazwa jest wymagana (do 200 znaków)."];
            }

            if (e?.Wlaczone is null)
            {
                errors[$"[{i}].wlaczone"] = ["Pole wlaczone jest wymagane."];
            }
        }

        if (errors.Count > 0)
        {
            return new(null, errors);
        }

        var current = await ReadAsync<CatalogEntryDto>(CatalogName, ct);
        var merged = Merge(current, input.Select(e => e with { Nazwa = e.Nazwa!.Trim() }).ToList(), e => e.Id!);
        await WriteAsync(CatalogName, merged, ct);
        return new(await GetCatalogAsync(ct), errors);
    }

    public async Task<IReadOnlyList<IntegrationFieldDto>> GetIntegrationsAsync(CancellationToken ct) =>
        Merge(IntegrationDefaults, await ReadAsync<IntegrationFieldDto>(IntegrationsName, ct), e => $"{e.Source}/{e.Fact}");

    public async Task<SettingsOutcome<IntegrationFieldDto>> PutIntegrationsAsync(List<IntegrationFieldDto>? input, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (input is null)
        {
            errors["body"] = ["Oczekiwano tablicy pól integracji."];
            return new(null, errors);
        }

        var known = IntegrationDefaults.Select(d => $"{d.Source}/{d.Fact}").ToHashSet();
        var seen = new HashSet<string>();
        for (var i = 0; i < input.Count; i++)
        {
            var e = input[i];
            if (e?.Source is null || !Sources.Contains(e.Source))
            {
                errors[$"[{i}].source"] = ["Nieznane źródło (mobywatel, zus, ezla, usc, user, assumption)."];
            }
            else if (e.Fact is null || !known.Contains($"{e.Source}/{e.Fact}"))
            {
                errors[$"[{i}].fact"] = ["To pole nie jest dostarczane przez to źródło integracji."];
            }
            else if (!seen.Add($"{e.Source}/{e.Fact}"))
            {
                errors[$"[{i}].fact"] = ["Pole występuje więcej niż raz."];
            }

            if (e?.Wlaczone is null)
            {
                errors[$"[{i}].wlaczone"] = ["Pole wlaczone jest wymagane."];
            }
        }

        if (errors.Count > 0)
        {
            return new(null, errors);
        }

        var current = await ReadAsync<IntegrationFieldDto>(IntegrationsName, ct);
        var merged = Merge(current, input, e => $"{e.Source}/{e.Fact}");
        await WriteAsync(IntegrationsName, merged, ct);
        return new(await GetIntegrationsAsync(ct), errors);
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken ct) =>
        await ReadAsync<OrganizationDto>(OrganizationsName, ct);

    public async Task<SettingsOutcome<OrganizationDto>> PutOrganizationsAsync(List<OrganizationDto>? input, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (input is null)
        {
            errors["body"] = ["Oczekiwano tablicy organizacji."];
            return new(null, errors);
        }

        if (input.Count > MaxOrganizations)
        {
            errors["body"] = [$"Najwyżej {MaxOrganizations} organizacji."];
            return new(null, errors);
        }

        var seen = new HashSet<string>();
        for (var i = 0; i < input.Count; i++)
        {
            var o = input[i];
            if (o is null)
            {
                errors[$"[{i}]"] = ["Pusty wpis."];
                continue;
            }

            Require(errors, i, "id", o.Id, 100);
            Require(errors, i, "nazwa", o.Nazwa, 200);
            Require(errors, i, "opis", o.Opis, 2000);
            if (!string.IsNullOrWhiteSpace(o.Id) && !seen.Add(o.Id.Trim()))
            {
                errors[$"[{i}].id"] = ["Identyfikator występuje więcej niż raz."];
            }

            if (o.Sytuacje is null || o.Sytuacje.Any(string.IsNullOrWhiteSpace))
            {
                errors[$"[{i}].sytuacje"] = ["Sytuacje: tablica niepustych tekstów."];
            }

            if (!string.IsNullOrWhiteSpace(o.Url)
                && !(Uri.TryCreate(o.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
            {
                errors[$"[{i}].url"] = ["Adres musi być bezwzględnym URL http(s)."];
            }

            if (o.Telefon is { Length: > 50 })
            {
                errors[$"[{i}].telefon"] = ["Telefon: najwyżej 50 znaków."];
            }

            if (o.Zweryfikowano is null)
            {
                errors[$"[{i}].zweryfikowano"] = ["Pole zweryfikowano jest wymagane."];
            }
        }

        if (errors.Count > 0)
        {
            return new(null, errors);
        }

        var clean = input.Select(o => o with
        {
            Id = o.Id!.Trim(),
            Nazwa = o.Nazwa!.Trim(),
            Opis = o.Opis!.Trim(),
            Url = string.IsNullOrWhiteSpace(o.Url) ? null : o.Url.Trim(),
            Telefon = string.IsNullOrWhiteSpace(o.Telefon) ? null : o.Telefon.Trim(),
        }).ToList();
        await WriteAsync(OrganizationsName, clean, ct);
        return new(clean, errors);
    }

    private static void Require(Dictionary<string, string[]> errors, int index, string field, string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
        {
            errors[$"[{index}].{field}"] = [$"Pole {field} jest wymagane (do {max} znaków)."];
        }
    }

    private static List<T> Merge<T>(IEnumerable<T> baseline, IEnumerable<T> overrides, Func<T, string> keyOf)
    {
        var result = baseline.ToDictionary(keyOf);
        foreach (var o in overrides)
        {
            result[keyOf(o)] = o;
        }

        var order = new Dictionary<string, int>();
        foreach (var b in baseline)
        {
            order.TryAdd(keyOf(b), order.Count);
        }

        return [.. result.Values.OrderBy(v => order.GetValueOrDefault(keyOf(v), int.MaxValue))];
    }

    private async Task<List<T>> ReadAsync<T>(string name, CancellationToken ct)
    {
        var json = await store.GetAsync(name, ct);
        return json is null ? [] : JsonSerializer.Deserialize<List<T>>(json, Json) ?? [];
    }

    private Task WriteAsync<T>(string name, IReadOnlyList<T> value, CancellationToken ct) =>
        store.SetAsync(name, JsonSerializer.Serialize(value, Json), time.GetUtcNow(), ct);
}
