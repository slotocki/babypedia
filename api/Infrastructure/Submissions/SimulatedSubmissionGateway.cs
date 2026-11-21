using System.Security.Cryptography;
using System.Text;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public sealed class SimulatedSubmissionGateway(TimeProvider time) : ISubmissionGateway
{
    public Task<SubmissionReceipt> SubmitAsync(Submission submission, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (SubmissionNames.RecipientFor(submission.Type) != submission.Recipient)
        {
            throw new ArgumentException("Kanał nie pasuje do typu zgłoszenia.", nameof(submission));
        }

        var at = time.GetUtcNow();
        var seed = new StringBuilder()
            .Append(submission.PersonaId).Append('|')
            .Append(at.UtcTicks).Append('|')
            .Append(SubmissionNames.ToWire(submission.Type)).Append('|')
            .Append(SubmissionNames.ToWire(submission.Recipient));
        foreach (var (key, value) in submission.Fields.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            seed.Append('|').Append(key).Append('=').Append(value);
        }

        var number = ReceiptNumber.Create(ReceiptNumber.CodeFor(submission.Type), at, seed.ToString());
        return Task.FromResult(new SubmissionReceipt(number, at, submission.Recipient));
    }
}

/// <summary>Numer potwierdzenia: BR-SYM-{rodzaj}-{RRRRMMDD}-{8 znaków}, wyliczany z danych (ten sam wsad = ten sam numer).</summary>
public static class ReceiptNumber
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string CodeFor(SubmissionType type) => type switch
    {
        SubmissionType.WniosekUrlop => "URL",
        SubmissionType.DaneZam => "ZAM",
        SubmissionType.ZapytanieEzus => "EZUS",
        SubmissionType.OdpisAktu => "ODP",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static string Create(string code, DateTimeOffset at, string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var chars = new char[8];
        ulong bits = 0;
        for (var i = 0; i < 5; i++)
        {
            bits = (bits << 8) | hash[i];
        }

        for (var i = 7; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(bits & 31)];
            bits >>= 5;
        }

        return $"BR-SYM-{code}-{at.UtcDateTime:yyyyMMdd}-{new string(chars)}";
    }
}
