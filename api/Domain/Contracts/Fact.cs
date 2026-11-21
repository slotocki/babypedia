using System.Text.Json.Serialization;

namespace BabyRadnik.Domain.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<FactSource>))]
public enum FactSource
{
    [JsonStringEnumMemberName("mobywatel")] MObywatel,
    [JsonStringEnumMemberName("zus")] Zus,
    [JsonStringEnumMemberName("ezla")] Ezla,
    [JsonStringEnumMemberName("usc")] Usc,
    [JsonStringEnumMemberName("user")] User,
    [JsonStringEnumMemberName("assumption")] Assumption,
}

/// <summary>Fakt z pochodzeniem (instrukcja, sekcja 6). <c>Page</c> = numer strony w dokumencie sprawy.</summary>
public sealed record Fact<T>(
    [property: JsonPropertyName("value")] T Value,
    [property: JsonPropertyName("source")] FactSource Source,
    [property: JsonPropertyName("page")] int? Page = null);
