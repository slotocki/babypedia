namespace BabyRadnik.Domain.Contracts;

/// <summary>Wartość parametru prawnego obowiązująca w danym dniu. Liczbowe w <c>Value</c>, ustawienia tekstowe w <c>Text</c>; pusta = null.</summary>
public sealed record Parameter(
    string Key,
    decimal? Value,
    string? Text,
    DateOnly ValidFrom,
    string LegalBasis);

public interface IParameterProvider
{
    /// <summary>Parametr obowiązujący w dniu <paramref name="date"/>; brak wiersza dla tej daty → <see cref="ParameterNotFoundException"/>.</summary>
    Parameter Get(string key, DateOnly date);
}

public sealed class ParameterNotFoundException(string key, DateOnly date)
    : Exception($"Brak parametru '{key}' obowiązującego w dniu {date:dd.MM.yyyy}.")
{
    public string Key { get; } = key;
    public DateOnly Date { get; } = date;
}

public static class ParameterProviderExtensions
{
    public static decimal GetDecimal(this IParameterProvider provider, string key, DateOnly date) =>
        provider.Get(key, date).Value ?? throw new ParameterNotFoundException(key, date);

    public static int GetInt(this IParameterProvider provider, string key, DateOnly date) =>
        (int)provider.GetDecimal(key, date);

    public static decimal? GetOptionalDecimal(this IParameterProvider provider, string key, DateOnly date) =>
        provider.Get(key, date).Value;
}
