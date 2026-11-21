using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BabyRadnik.Infrastructure.Parameters;

public sealed record ParameterChangeRequest(string? Key, JsonElement Value, string? ValidFrom, string? LegalBasis, string? Reason);

public sealed record ParameterDto(
    string Key,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Wiersz,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] object? Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Unit,
    DateOnly ValidFrom,
    string LegalBasis,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Note);

public sealed record ParameterHistoryDto(
    string Key,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] object? Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] object? PreviousValue,
    DateOnly ValidFrom,
    DateTimeOffset ChangedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Reason);

public sealed record ChangeOutcome(ParameterDto? Parameter, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Parameter is not null;
}

public static partial class ParameterChangeValidator
{
    public const string Actor = "admin";
    private const int MaxText = 500;
    private static readonly DateOnly MinDate = new(1900, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);
    private static readonly HashSet<string> IntegerUnits = ["tygodnie", "dni", "miesiace", "lata", "liczba"];

    [GeneratedRegex("^[a-z0-9_]+(\\.[a-z0-9_]+)*$")]
    private static partial Regex KeyPattern();

    public static (ParameterRow? Row, Dictionary<string, string[]> Errors) Validate(ParameterChangeRequest request, IReadOnlyList<ParameterRow> existing)
    {
        var errors = new Dictionary<string, string[]>();

        var key = request.Key?.Trim();
        ParameterRow? template = null;
        if (string.IsNullOrEmpty(key) || key.Length > 200 || !KeyPattern().IsMatch(key))
        {
            errors["key"] = ["Klucz jest wymagany i ma postać np. stawka_rodzicielski lub zaokraglanie.kwota_za_okres."];
        }
        else
        {
            template = ParameterSelector.Latest(existing, key);
            if (template is null)
            {
                errors["key"] = [$"Nieznany klucz parametru '{key}'."];
            }
        }

        DateOnly validFrom = default;
        if (!DateOnly.TryParseExact(request.ValidFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out validFrom)
            || validFrom < MinDate || validFrom > MaxDate)
        {
            errors["validFrom"] = ["Data obowiązywania jest wymagana, format RRRR-MM-DD, w latach 1900–2100."];
        }

        decimal? number = null;
        string? text = null;
        var valueError = ValidateValue(request.Value, template?.Unit, out number, out text);
        if (valueError is not null)
        {
            errors["value"] = [valueError];
        }

        if (request.LegalBasis is { Length: > MaxText })
        {
            errors["legalBasis"] = [$"Podstawa prawna może mieć najwyżej {MaxText} znaków."];
        }

        if (request.Reason is { Length: > MaxText })
        {
            errors["reason"] = [$"Powód może mieć najwyżej {MaxText} znaków."];
        }

        if (errors.Count > 0 || template is null)
        {
            return (null, errors);
        }

        var legalBasis = string.IsNullOrWhiteSpace(request.LegalBasis) ? template.LegalBasis : request.LegalBasis.Trim();
        var row = new ParameterRow
        {
            Key = template.Key,
            Wiersz = template.Wiersz,
            ValueNumber = number,
            ValueText = text,
            Unit = template.Unit,
            ValidFrom = validFrom,
            LegalBasis = legalBasis,
            Status = ParameterStatus.Ok,
        };
        return (row, errors);
    }

    private static string? ValidateValue(JsonElement value, string? unit, out decimal? number, out string? text)
    {
        number = null;
        text = null;
        switch (value.ValueKind)
        {
            case JsonValueKind.Undefined:
                return "Wartość jest wymagana (liczba, tekst albo null).";
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Number:
                if (unit == "ustawienie")
                {
                    return "To ustawienie tekstowe; podaj tekst albo null.";
                }

                if (!value.TryGetDecimal(out var n) || n < 0)
                {
                    return "Wartość liczbowa musi być nieujemna.";
                }

                if (unit == "ulamek" && n > 1)
                {
                    return "Ułamek musi mieścić się w przedziale 0–1 (100% = 1).";
                }

                if (unit is not null && IntegerUnits.Contains(unit) && n != decimal.Truncate(n))
                {
                    return $"Jednostka '{unit}' wymaga liczby całkowitej.";
                }

                number = n;
                return null;
            case JsonValueKind.String:
                if (unit is not null && unit != "ustawienie")
                {
                    return "Ten parametr jest liczbowy; podaj liczbę albo null.";
                }

                var s = value.GetString()!.Trim();
                if (s.Length is 0 or > 200)
                {
                    return "Tekst musi mieć od 1 do 200 znaków.";
                }

                text = s;
                return null;
            default:
                return "Wartość musi być liczbą, tekstem albo null.";
        }
    }
}

public sealed class ParameterAdminService(IParameterStore store, CachedParameterProvider provider, TimeProvider time)
{
    public Task<IReadOnlyList<ParameterDto>> ListAsync() =>
        Task.FromResult<IReadOnlyList<ParameterDto>>(store.LoadAll()
            .OrderBy(r => r.Key, StringComparer.Ordinal).ThenBy(r => r.ValidFrom).ThenBy(r => r.Id)
            .Select(ToDto).ToList());

    public async Task<IReadOnlyList<ParameterHistoryDto>> HistoryAsync(CancellationToken ct) =>
        (await store.LoadHistoryAsync(ct)).Select(h => new ParameterHistoryDto(
            h.Key, Boxed(h.ValueNumber, h.ValueText), Boxed(h.PreviousNumber, h.PreviousText), h.ValidFrom, h.ChangedAt, h.Reason)).ToList();

    public async Task<ChangeOutcome> ChangeAsync(ParameterChangeRequest request, CancellationToken ct)
    {
        var existing = store.LoadAll();
        var (row, errors) = ParameterChangeValidator.Validate(request, existing);
        if (row is null)
        {
            return new ChangeOutcome(null, errors);
        }

        var previous = ParameterSelector.Select(existing, row.Key, row.ValidFrom);
        var history = new ParameterHistoryRow
        {
            Key = row.Key,
            ValueNumber = row.ValueNumber,
            ValueText = row.ValueText,
            PreviousNumber = previous?.ValueNumber,
            PreviousText = previous?.ValueText,
            ValidFrom = row.ValidFrom,
            ChangedAt = time.GetUtcNow(),
            ChangedBy = ParameterChangeValidator.Actor,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
        };

        await store.AddAsync(row, history, ct);
        provider.Invalidate();
        return new ChangeOutcome(ToDto(row), errors);
    }

    private static ParameterDto ToDto(ParameterRow r) =>
        new(r.Key, r.Wiersz, Boxed(r.ValueNumber, r.ValueText), r.Unit, r.ValidFrom, r.LegalBasis, r.Status, r.Note);

    private static object? Boxed(decimal? number, string? text) => number is { } n ? n : text;
}
