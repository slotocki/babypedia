using System.Security.Cryptography;
using System.Text;
using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace BabyRadnik.Infrastructure.Submissions;

public sealed record SubmissionRequestBody(string? Type, string? Template, Dictionary<string, string>? Fields);

public sealed record SubmissionReceiptDto(
    string Id,
    string Number,
    string Type,
    string Recipient,
    DateTimeOffset SubmittedAt,
    string? PdfUrl,
    bool Simulated,
    string Status,
    string Summary);

public sealed record SubmissionOutcome(SubmissionReceiptDto? Receipt, IReadOnlyDictionary<string, string[]> Errors, bool LimitReached = false)
{
    public bool Succeeded => Receipt is not null;
}

/// <summary>Gotowe PDF-y trzymane krótko w pamięci: baza przechowuje tylko wpis listy, bez danych osobowych z dokumentu.</summary>
public sealed class SubmissionDocumentCache : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64L * 1024 * 1024 });

    public void Put(string sessionHash, Guid id, byte[] pdf) =>
        _cache.Set(Key(sessionHash, id), pdf, new MemoryCacheEntryOptions { Size = pdf.Length, AbsoluteExpirationRelativeToNow = Lifetime });

    public byte[]? Get(string sessionHash, Guid id) => _cache.TryGetValue(Key(sessionHash, id), out byte[]? pdf) ? pdf : null;

    public void Dispose() => _cache.Dispose();

    private static string Key(string sessionHash, Guid id) => $"{sessionHash}:{id:N}";
}

public sealed class SubmissionService(
    ISubmissionGateway gateway,
    ISubmissionStore store,
    SubmissionDocumentCache documents)
{
    public const int MaxPerSession = 200;

    public static string HashSession(string sessionId) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId))).ToLowerInvariant();

    public async Task<SubmissionOutcome> CreateAsync(string sessionId, string personaId, SubmissionRequestBody body, CancellationToken ct)
    {
        var type = SubmissionNames.ParseType(body.Type);
        if (type is null)
        {
            return Failure("type", "Dozwolone typy: wniosek_urlop, dane_zam, zapytanie_ezus, odpis_aktu.");
        }

        var spec = TemplateCatalog.Resolve(type.Value, body.Template);
        if (spec is null)
        {
            return Failure("template", type == SubmissionType.WniosekUrlop
                ? "Dla wniosek_urlop podaj template: macierzynski_i_rodzicielski, rodzicielski, ojcowski albo uzupelniajacy."
                : "Nieznany szablon.");
        }

        if (type != SubmissionType.WniosekUrlop && body.Template is not null)
        {
            return Failure("template", "Pole template dotyczy tylko typu wniosek_urlop.");
        }

        var fields = body.Fields ?? [];
        var validation = SubmissionValidator.Validate(spec, fields);
        if (!validation.IsValid)
        {
            return new SubmissionOutcome(null, validation.Errors);
        }

        var sessionHash = HashSession(sessionId);
        if ((await store.ListAsync(sessionHash, MaxPerSession, ct)).Count >= MaxPerSession)
        {
            return new SubmissionOutcome(null, new Dictionary<string, string[]>(), LimitReached: true);
        }

        var clean = fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .ToDictionary(f => f.Key, f => f.Value.Trim(), StringComparer.Ordinal);
        var receipt = await gateway.SubmitAsync(new Submission(type.Value, spec.Recipient, personaId, clean), ct);

        var model = DocumentBuilder.Build(spec, clean, receipt.Number, DateOnly.FromDateTime(receipt.SubmittedAt.UtcDateTime));
        var pdf = PdfRenderer.Render(model);

        var row = new SubmissionRow
        {
            Id = Guid.NewGuid(),
            Number = receipt.Number,
            Type = SubmissionNames.ToWire(type.Value),
            Recipient = SubmissionNames.ToWire(receipt.Recipient),
            SubmittedAt = receipt.SubmittedAt,
            Summary = DocumentBuilder.Summarize(spec, clean),
            SessionHash = sessionHash,
        };
        await store.AddAsync(row, ct);
        documents.Put(sessionHash, row.Id, pdf);
        return new SubmissionOutcome(ToDto(row, hasPdf: true), new Dictionary<string, string[]>());
    }

    public async Task<IReadOnlyList<SubmissionReceiptDto>> ListAsync(string sessionId, CancellationToken ct)
    {
        var sessionHash = HashSession(sessionId);
        var rows = await store.ListAsync(sessionHash, MaxPerSession, ct);
        return rows.Select(r => ToDto(r, documents.Get(sessionHash, r.Id) is not null)).ToList();
    }

    public async Task<PdfLookup> FindPdfAsync(string sessionId, Guid id, CancellationToken ct)
    {
        var sessionHash = HashSession(sessionId);
        var row = await store.FindAsync(id, sessionHash, ct);
        if (row is null)
        {
            return new PdfLookup(null, null, Found: false);
        }

        return new PdfLookup(row.Number, documents.Get(sessionHash, id), Found: true);
    }

    private static SubmissionReceiptDto ToDto(SubmissionRow row, bool hasPdf) => new(
        row.Id.ToString(),
        row.Number,
        row.Type,
        row.Recipient,
        row.SubmittedAt,
        hasPdf ? $"/api/submissions/{row.Id}/pdf" : null,
        true,
        row.Status,
        row.Summary);

    private static SubmissionOutcome Failure(string field, string message) =>
        new(null, new Dictionary<string, string[]> { [field] = [message] });
}

public sealed record PdfLookup(string? Number, byte[]? Pdf, bool Found);
