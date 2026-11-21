using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Simulation;

public interface IIntegrationSettings
{
    bool IsFieldEnabled(FactSource source, string factKey);
}

public sealed class AllFieldsEnabledSettings : IIntegrationSettings
{
    public bool IsFieldEnabled(FactSource source, string factKey) => true;
}
