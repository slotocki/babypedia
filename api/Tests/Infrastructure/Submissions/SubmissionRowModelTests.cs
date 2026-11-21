using BabyRadnik.Infrastructure.Persistence;
using BabyRadnik.Infrastructure.Submissions;
using Microsoft.EntityFrameworkCore;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

public sealed class SubmissionRowModelTests
{
    [Fact]
    public void Submissions_table_is_discovered_and_holds_only_list_columns()
    {
        var options = new DbContextOptionsBuilder<BabyRadnikDbContext>().UseNpgsql("Host=localhost;Database=model-only").Options;
        using var db = new BabyRadnikDbContext(options);

        var entity = db.Model.FindEntityType(typeof(SubmissionRow));

        Assert.NotNull(entity);
        Assert.Equal("submissions", entity.GetTableName());
        Assert.Equal(
            ["Id", "Number", "Recipient", "SessionHash", "Status", "SubmittedAt", "Summary", "Type"],
            entity.GetProperties().Select(p => p.Name).Order(StringComparer.Ordinal));
    }
}
