using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Parameters;

public sealed class CachedParameterProvider(IParameterStore store) : IParameterProvider
{
    private volatile ILookup<string, ParameterRow>? snapshot;

    public Parameter Get(string key, DateOnly date)
    {
        var rows = (snapshot ??= Load())[key];
        var row = ParameterSelector.Select(rows, key, date) ?? throw new ParameterNotFoundException(key, date);
        return ParameterSelector.ToParameter(row);
    }

    public void Invalidate() => snapshot = null;

    private ILookup<string, ParameterRow> Load() => store.LoadAll().ToLookup(r => r.Key);
}
