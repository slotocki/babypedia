using System.Text.Json;
using System.Text.Json.Serialization;

namespace BabyRadnik.Domain.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<ScenarioId>))]
public enum ScenarioId
{
    [JsonStringEnumMemberName("odchodze_z_pracy")] OdchodzeZPracy,
    [JsonStringEnumMemberName("umowa_nie_zostanie_przedluzona")] UmowaNieZostaniePrzedluzona,
    [JsonStringEnumMemberName("przechodze_na_dzialalnosc")] PrzechodzeNaDzialalnosc,
    [JsonStringEnumMemberName("drugi_rodzic_bierze")] DrugiRodzicBierze,
    [JsonStringEnumMemberName("praca_na_czesc_etatu")] PracaNaCzescEtatu,
    [JsonStringEnumMemberName("wariant_wyplaty")] WariantWyplaty,
    [JsonStringEnumMemberName("splacam_zadluzenie")] SplacamZadluzenie,
}

/// <summary>
/// Scenariusz „co jeśli" (sekcja 9). Parametry wg scenariusza:
/// odchodze_z_pracy {data}; umowa_nie_zostanie_przedluzona {}; przechodze_na_dzialalnosc {data, dobrowolneChorobowe};
/// drugi_rodzic_bierze {tygodnie} (Person = osoba, która bierze podane tygodnie; druga osoba dostaje resztę wymiaru); praca_na_czesc_etatu {wymiar, tygodnie}; wariant_wyplaty {wariant}; splacam_zadluzenie {data}.
/// </summary>
public sealed record ScenarioChange(
    [property: JsonPropertyName("id")] ScenarioId Id,
    [property: JsonPropertyName("person")] PersonRef Person = PersonRef.Wnioskodawca,
    [property: JsonPropertyName("params")] JsonElement? Params = null);

public sealed record CompareRequest(
    [property: JsonPropertyName("facts")] FactSet Facts,
    [property: JsonPropertyName("scenarios")] IReadOnlyList<ScenarioChange> Scenarios,
    [property: JsonPropertyName("asOf")] DateOnly? AsOf = null);

public sealed record BenefitDiff(
    [property: JsonPropertyName("id")] BenefitId Id,
    [property: JsonPropertyName("person")] PersonRef Person,
    [property: JsonPropertyName("statusBase")] BenefitStatus StatusBase,
    [property: JsonPropertyName("statusVariant")] BenefitStatus StatusVariant,
    [property: JsonPropertyName("weeksDelta")] decimal? WeeksDelta,
    [property: JsonPropertyName("amountDelta")] decimal? AmountDelta);

/// <summary>
/// Wynik porównania. <see cref="VariantFacts"/> = pełny zbiór faktów po nakładce scenariuszy (ocena tego zbioru daje
/// dokładnie <see cref="Variant"/>); null, gdy scenariusza nie dało się zastosować. <see cref="Changes"/> = opis zmian prostym językiem.
/// </summary>
public sealed record CompareResult(
    [property: JsonPropertyName("base")] AssessmentResult Base,
    [property: JsonPropertyName("variant")] AssessmentResult Variant,
    [property: JsonPropertyName("diff")] IReadOnlyList<BenefitDiff> Diff,
    [property: JsonPropertyName("validationErrors")] IReadOnlyList<ValidationIssue> ValidationErrors,
    [property: JsonPropertyName("variantFacts")] FactSet? VariantFacts = null,
    [property: JsonPropertyName("changes")] IReadOnlyList<string>? Changes = null);

public interface IScenarioComparer
{
    CompareResult Compare(CompareRequest request);
}
