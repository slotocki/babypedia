using BabyRadnik.Infrastructure.Submissions;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

internal sealed class InMemorySubmissionStore : ISubmissionStore
{
    private readonly List<SubmissionRow> _rows = [];

    public IReadOnlyList<SubmissionRow> Rows
    {
        get
        {
            lock (_rows)
            {
                return _rows.ToList();
            }
        }
    }

    public Task AddAsync(SubmissionRow row, CancellationToken ct)
    {
        lock (_rows)
        {
            _rows.Add(row);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubmissionRow>> ListAsync(string sessionHash, int limit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SubmissionRow>>(Rows
            .Where(r => r.SessionHash == sessionHash)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(limit)
            .ToList());

    public Task<SubmissionRow?> FindAsync(Guid id, string sessionHash, CancellationToken ct) =>
        Task.FromResult(Rows.FirstOrDefault(r => r.Id == id && r.SessionHash == sessionHash));
}
