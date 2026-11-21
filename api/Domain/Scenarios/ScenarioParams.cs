using System.Globalization;
using System.Text.Json;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Scenarios;

public sealed class ScenarioParams
{
    private readonly JsonElement? values;
    private readonly PersonRef person;
    private readonly List<ValidationIssue> issues = [];

    public ScenarioParams(JsonElement? raw, PersonRef person, params string[] allowed)
    {
        this.person = person;
        if (raw is { ValueKind: JsonValueKind.Object } obj)
        {
            values = obj;
            foreach (var property in obj.EnumerateObject().Where(p => !allowed.Contains(p.Name)))
            {
                Add("scenariusz_nieznany_parametr", $"Scenariusz nie przyjmuje parametru „{property.Name}”.");
            }
        }
        else if (raw is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
        {
            Add("scenariusz_zly_parametr", "Parametry scenariusza muszą być obiektem JSON.");
        }
    }

    public IReadOnlyList<ValidationIssue> Issues => issues;

    public bool Valid => issues.Count == 0;

    public DateOnly? Date(string name)
    {
        if (!TryGet(name, out var element, "RRRR-MM-DD"))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(element.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        Add("scenariusz_zly_parametr", $"Parametr „{name}” musi być datą w formacie RRRR-MM-DD.");
        return null;
    }

    public bool? Bool(string name)
    {
        if (!TryGet(name, out var element, "tak albo nie"))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        Add("scenariusz_zly_parametr", $"Parametr „{name}” musi mieć wartość true albo false.");
        return null;
    }

    public int? Integer(string name)
    {
        if (!TryGet(name, out var element, "liczba całkowita"))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number;
        }

        Add("scenariusz_zly_parametr", $"Parametr „{name}” musi być liczbą całkowitą.");
        return null;
    }

    public decimal? Number(string name)
    {
        if (!TryGet(name, out var element, "liczba"))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        Add("scenariusz_zly_parametr", $"Parametr „{name}” musi być liczbą.");
        return null;
    }

    public string? Text(string name, params string[] options)
    {
        if (!TryGet(name, out var element, string.Join(" albo ", options)))
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String && element.GetString() is { } text && options.Contains(text))
        {
            return text;
        }

        Add("scenariusz_zly_parametr", $"Parametr „{name}” musi mieć wartość: {string.Join(" albo ", options)}.");
        return null;
    }

    private bool TryGet(string name, out JsonElement element, string expected)
    {
        element = default;
        if (values is { } obj && obj.TryGetProperty(name, out element) && element.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        Add("scenariusz_brak_parametru", $"Scenariusz wymaga parametru „{name}” ({expected}).");
        return false;
    }

    private void Add(string code, string message) => issues.Add(new ValidationIssue(code, message, null, person));
}
