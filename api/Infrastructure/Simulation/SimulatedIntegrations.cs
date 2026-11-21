using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed record ChildRegistration(DateOnly DataUrodzenia, int Liczba);

public interface ICivilRegistry
{
    Task<ChildRegistration?> GetChildRegistrationAsync(string personaId, CancellationToken ct = default);
}

public sealed class SimulatedIdentityProvider(PersonaStore store) : IIdentityProvider
{
    public Task<IReadOnlyList<PersonaSummary>> ListPersonasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PersonaSummary>>(store.All.Select(p => new PersonaSummary(p.Id, p.Opis, p.Sprawa)).ToList());

    public Task<Identity?> LoginAsync(string personaId, CancellationToken ct = default) =>
        Task.FromResult(store.Find(personaId)?.Tozsamosc);
}

public sealed class SimulatedInsuranceRegistry(PersonaStore store) : IInsuranceRegistry
{
    public Task<InsuranceAccount> GetAccountAsync(string personaId, CancellationToken ct = default)
    {
        var facts = store.Find(personaId)?.Fakty.Wnioskodawca;
        return Task.FromResult(new InsuranceAccount(
            facts?.UbezpieczenieTytuly?.Value ?? [],
            facts?.UbezpieczeniePodstawySkladek?.Value ?? [],
            facts?.UbezpieczeniePrzerwy?.Value ?? [],
            facts?.UbezpieczeniePobraneSwiadczenia?.Value ?? []));
    }
}

public sealed class SimulatedContributionAccount(PersonaStore store) : IContributionAccount
{
    public Task<ContributionDebt?> GetBalanceAsync(string personaId, DateOnly onDate, CancellationToken ct = default)
    {
        var persona = store.Find(personaId);
        if (persona is null)
        {
            return Task.FromResult<ContributionDebt?>(null);
        }

        var declared = persona.Fakty.Wnioskodawca.SkladkiZadluzenie?.Value;
        if (declared is not null)
        {
            // Saldo znane najpóźniej na dzień z konta płatnika — nie „na dzień” przyszłego zdarzenia.
            var asOf = declared.NaDzien < onDate ? declared.NaDzien : onDate;
            return Task.FromResult<ContributionDebt?>(new ContributionDebt(asOf, declared.Kwota));
        }

        var selfPaying = persona.Fakty.Wnioskodawca.UbezpieczenieTytuly?.Value
            .Any(t => t.RodzajTytulu is TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca) == true;
        return Task.FromResult<ContributionDebt?>(selfPaying ? new ContributionDebt(onDate, 0m) : null);
    }

    public Task<int?> GetPayerHeadcountAsync(string payerId, int year, CancellationToken ct = default) =>
        Task.FromResult(store.PayerHeadcount(payerId));
}

public sealed class SimulatedMedicalCertificates(PersonaStore store) : IMedicalCertificates
{
    public Task<IReadOnlyList<SickLeave>> GetSickLeavesAsync(string personaId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SickLeave>>(store.Find(personaId)?.Fakty.Wnioskodawca.EzlaZwolnienia?.Value ?? []);
}

public sealed class SimulatedCivilRegistry(PersonaStore store) : ICivilRegistry
{
    public Task<ChildRegistration?> GetChildRegistrationAsync(string personaId, CancellationToken ct = default)
    {
        var facts = store.Find(personaId)?.Fakty.Sprawa;
        if (facts?.ZdarzenieRodzaj?.Value != EventKind.Urodzenie || facts.DzieckoDataUrodzenia is null)
        {
            return Task.FromResult<ChildRegistration?>(null);
        }

        return Task.FromResult<ChildRegistration?>(
            new ChildRegistration(facts.DzieckoDataUrodzenia.Value, facts.DzieckoLiczba?.Value ?? 1));
    }
}
