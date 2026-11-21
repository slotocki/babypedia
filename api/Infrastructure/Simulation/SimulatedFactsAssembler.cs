using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed class SimulatedFactsAssembler(
    PersonaStore store,
    IIdentityProvider identity,
    IInsuranceRegistry insurance,
    IContributionAccount contributions,
    IMedicalCertificates certificates,
    ICivilRegistry civil,
    IIntegrationSettings settings,
    TimeProvider time)
{
    public async Task<FactSet?> AssembleAsync(string personaId, CancellationToken ct = default)
    {
        var persona = store.Find(personaId);
        if (persona is null)
        {
            return null;
        }

        var own = persona.Fakty.Wnioskodawca;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var onDate = PersonaFacts.EventDate(persona) ?? today;

        var login = await identity.LoginAsync(personaId, ct);
        var account = await insurance.GetAccountAsync(personaId, ct);
        var leaves = await certificates.GetSickLeavesAsync(personaId, ct);
        var debt = await contributions.GetBalanceAsync(personaId, onDate, ct);
        var child = await civil.GetChildRegistrationAsync(personaId, ct);

        int? headcount = null;
        var payerId = PersonaFacts.MainTitle(account.Tytuly, onDate)?.PlatnikId;
        if (payerId is not null)
        {
            headcount = await contributions.GetPayerHeadcountAsync(payerId, onDate.Year - 1, ct);
        }

        var applicant = new PersonFacts
        {
            OsobaDataUrodzenia = EmitStruct(FactSource.MObywatel, FactKeys.OsobaDataUrodzenia, login?.DataUrodzenia, own.OsobaDataUrodzenia?.Page),
            UbezpieczenieTytuly = Emit(FactSource.Zus, FactKeys.UbezpieczenieTytuly, account.Tytuly, own.UbezpieczenieTytuly?.Page),
            UbezpieczeniePodstawySkladek = Emit(FactSource.Zus, FactKeys.UbezpieczeniePodstawySkladek, account.PodstawySkladek, own.UbezpieczeniePodstawySkladek?.Page),
            UbezpieczeniePrzerwy = Emit(FactSource.Zus, FactKeys.UbezpieczeniePrzerwy, account.Przerwy, own.UbezpieczeniePrzerwy?.Page),
            UbezpieczeniePobraneSwiadczenia = Emit(FactSource.Zus, FactKeys.UbezpieczeniePobraneSwiadczenia, account.PobraneSwiadczenia, own.UbezpieczeniePobraneSwiadczenia?.Page),
            PlatnikLiczbaUbezpieczonych = EmitStruct(FactSource.Zus, FactKeys.PlatnikLiczbaUbezpieczonych, headcount, own.PlatnikLiczbaUbezpieczonych?.Page),
            SkladkiZadluzenie = Emit(FactSource.Zus, FactKeys.SkladkiZadluzenie, debt, own.SkladkiZadluzenie?.Page),
            EzlaZwolnienia = Emit(FactSource.Ezla, FactKeys.EzlaZwolnienia, leaves, own.EzlaZwolnienia?.Page),
        };

        var caseFacts = persona.Fakty.Sprawa;
        var caseSet = new CaseFacts
        {
            DzieckoLiczba = EmitStruct(FactSource.Usc, FactKeys.DzieckoLiczba, child?.Liczba, caseFacts.DzieckoLiczba?.Page),
            DzieckoDataUrodzenia = EmitStruct(FactSource.Usc, FactKeys.DzieckoDataUrodzenia, child?.DataUrodzenia, caseFacts.DzieckoDataUrodzenia?.Page),
        };

        return new FactSet { Sprawa = caseSet, Wnioskodawca = applicant, DrugiRodzic = await PartnerFactsAsync(persona, ct) };
    }

    private async Task<PersonFacts?> PartnerFactsAsync(Persona persona, CancellationToken ct)
    {
        var partner = store.Find(persona.DrugiRodzic);
        if (partner is null)
        {
            return null;
        }

        var account = await insurance.GetAccountAsync(partner.Id, ct);
        var titles = Emit(FactSource.Zus, FactKeys.UbezpieczenieTytuly, account.Tytuly, partner.Fakty.Wnioskodawca.UbezpieczenieTytuly?.Page);
        return titles is null ? null : new PersonFacts { UbezpieczenieTytuly = titles };
    }

    private Fact<T>? Emit<T>(FactSource source, string key, T? value, int? page) where T : class =>
        value is null || !settings.IsFieldEnabled(source, key) ? null : new Fact<T>(value, source, page);

    private Fact<T>? EmitStruct<T>(FactSource source, string key, T? value, int? page) where T : struct =>
        value is null || !settings.IsFieldEnabled(source, key) ? null : new Fact<T>(value.Value, source, page);
}
