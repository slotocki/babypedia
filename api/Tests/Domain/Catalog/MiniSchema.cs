using System.Text.Json;
using System.Text.RegularExpressions;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>Mały walidator podzbioru JSON Schema użytego w contracts/assessment.schema.json (lokalne $ref, type, enum, required, properties, additionalProperties, items, anyOf, pattern, minimum/maximum).</summary>
public static class MiniSchema
{
    public static List<string> Validate(JsonElement root, string definition, JsonElement instance)
    {
        var errors = new List<string>();
        Check(root, root.GetProperty("$defs").GetProperty(definition), instance, "$", errors);
        return errors;
    }

    private static JsonElement Resolve(JsonElement root, JsonElement schema)
    {
        while (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("$ref", out var reference))
        {
            var pointer = reference.GetString()!;
            if (!pointer.StartsWith("#/"))
            {
                throw new NotSupportedException($"Nieobsługiwany $ref: {pointer}");
            }

            var node = root;
            foreach (var part in pointer[2..].Split('/'))
            {
                node = node.GetProperty(part);
            }

            schema = node;
        }

        return schema;
    }

    private static void Check(JsonElement root, JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        schema = Resolve(root, schema);
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("anyOf", out var anyOf))
        {
            var attempts = anyOf.EnumerateArray().Select(option =>
            {
                var local = new List<string>();
                Check(root, option, value, path, local);
                return local;
            }).ToList();
            if (attempts.All(a => a.Count > 0))
            {
                errors.Add($"{path}: żaden wariant anyOf nie pasuje ({string.Join(" | ", attempts.Select(a => a[0]))})");
            }

            return;
        }

        if (schema.TryGetProperty("enum", out var allowed)
            && !allowed.EnumerateArray().Any(a => a.ValueKind == value.ValueKind && a.ToString() == value.ToString()))
        {
            errors.Add($"{path}: wartość {value} spoza enum");
            return;
        }

        if (schema.TryGetProperty("type", out var type) && !TypeMatches(type, value))
        {
            errors.Add($"{path}: typ {value.ValueKind} nie pasuje do {type}");
            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                if (schema.TryGetProperty("pattern", out var pattern) && !Regex.IsMatch(value.GetString()!, pattern.GetString()!))
                {
                    errors.Add($"{path}: „{value}” nie pasuje do wzorca {pattern}");
                }

                break;
            case JsonValueKind.Number:
                var number = value.GetDecimal();
                if (schema.TryGetProperty("minimum", out var min) && number < min.GetDecimal())
                {
                    errors.Add($"{path}: {number} < minimum {min}");
                }

                if (schema.TryGetProperty("maximum", out var max) && number > max.GetDecimal())
                {
                    errors.Add($"{path}: {number} > maximum {max}");
                }

                if (schema.TryGetProperty("exclusiveMinimum", out var exMin) && number <= exMin.GetDecimal())
                {
                    errors.Add($"{path}: {number} <= exclusiveMinimum {exMin}");
                }

                if (schema.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "integer" && number != decimal.Truncate(number))
                {
                    errors.Add($"{path}: {number} nie jest liczbą całkowitą");
                }

                break;
            case JsonValueKind.Array:
                if (schema.TryGetProperty("items", out var items))
                {
                    var index = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        Check(root, items, item, $"{path}[{index++}]", errors);
                    }
                }

                break;
            case JsonValueKind.Object:
                CheckObject(root, schema, value, path, errors);
                break;
        }
    }

    private static void CheckObject(JsonElement root, JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray().Select(r => r.GetString()!))
            {
                if (!value.TryGetProperty(name, out _))
                {
                    errors.Add($"{path}: brak wymaganego pola {name}");
                }
            }
        }

        var properties = schema.TryGetProperty("properties", out var p) ? p : default;
        var closed = schema.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.False;
        foreach (var property in value.EnumerateObject())
        {
            if (properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(property.Name, out var propertySchema))
            {
                Check(root, propertySchema, property.Value, $"{path}.{property.Name}", errors);
            }
            else if (closed)
            {
                errors.Add($"{path}: nieznane pole {property.Name}");
            }
        }
    }

    private static bool TypeMatches(JsonElement type, JsonElement value)
    {
        var types = type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(t => t.GetString()!).ToList() : [type.GetString()!];
        return types.Any(t => t switch
        {
            "string" => value.ValueKind == JsonValueKind.String,
            "number" or "integer" => value.ValueKind == JsonValueKind.Number,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "array" => value.ValueKind == JsonValueKind.Array,
            "object" => value.ValueKind == JsonValueKind.Object,
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => false,
        });
    }
}
