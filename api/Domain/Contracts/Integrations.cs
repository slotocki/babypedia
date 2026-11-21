namespace BabyRadnik.Domain.Contracts;

/// <summary>Odpowiedź symulowanego logowania mObywatel. PESEL jest fikcyjny i nie trafia do sprawy, sesji, logów ani silnika (zasada 2.5).</summary>
public sealed record Identity(string Imie, string Nazwisko, string? NazwiskoRodowe, string Pesel, DateOnly DataUrodzenia);

public sealed record PersonaSummary(string Id, string Opis, string? CaseId);

public interface IIdentityProvider
{
    Task<IReadOnlyList<PersonaSummary>> ListPersonasAsync(CancellationToken ct = default);
    Task<Identity?> LoginAsync(string personaId, CancellationToken ct = default);
}

public sealed record InsuranceAccount(
    IReadOnlyList<InsuranceTitle> Tytuly,
    IReadOnlyList<ContributionBase> PodstawySkladek,
    IReadOnlyList<InsuranceBreak> Przerwy,
    IReadOnlyList<ReceivedBenefit> PobraneSwiadczenia);

public interface IInsuranceRegistry
{
    Task<InsuranceAccount> GetAccountAsync(string personaId, CancellationToken ct = default);
}

public interface IContributionAccount
{
    Task<ContributionDebt?> GetBalanceAsync(string personaId, DateOnly onDate, CancellationToken ct = default);
    Task<int?> GetPayerHeadcountAsync(string payerId, int year, CancellationToken ct = default);
}

public interface IMedicalCertificates
{
    Task<IReadOnlyList<SickLeave>> GetSickLeavesAsync(string personaId, CancellationToken ct = default);
}

public enum SubmissionType
{
    WniosekUrlop,
    DaneZam,
    ZapytanieEzus,
    OdpisAktu,
}

public enum SubmissionRecipient
{
    Pracodawca,
    Zus,
    Ezus,
    UrzadStanuCywilnego,
}

public sealed record Submission(SubmissionType Type, SubmissionRecipient Recipient, string PersonaId, IReadOnlyDictionary<string, string> Fields);

public sealed record SubmissionReceipt(string Number, DateTimeOffset SubmittedAt, SubmissionRecipient Recipient);

public interface ISubmissionGateway
{
    Task<SubmissionReceipt> SubmitAsync(Submission submission, CancellationToken ct = default);
}
