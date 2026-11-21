using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyRadnik.Infrastructure.Parameters;

public sealed class ParameterRow
{
    public long Id { get; set; }
    public string Key { get; init; } = "";
    public string? Wiersz { get; init; }
    public decimal? ValueNumber { get; init; }
    public string? ValueText { get; init; }
    public string? Unit { get; init; }
    public DateOnly ValidFrom { get; init; }
    public string LegalBasis { get; init; } = "";
    public string Status { get; init; } = ParameterStatus.Ok;
    public string? Note { get; init; }
}

public sealed class ParameterHistoryRow
{
    public long Id { get; set; }
    public string Key { get; init; } = "";
    public decimal? ValueNumber { get; init; }
    public string? ValueText { get; init; }
    public decimal? PreviousNumber { get; init; }
    public string? PreviousText { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public string ChangedBy { get; init; } = "";
    public string? Reason { get; init; }
}

public sealed class AdminSettingRow
{
    public string Name { get; set; } = "";
    public string Json { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

public static class ParameterStatus
{
    public const string Ok = "ok";
    public const string DoWeryfikacji = "do_weryfikacji";
}

internal sealed class ParameterRowConfiguration : IEntityTypeConfiguration<ParameterRow>
{
    public void Configure(EntityTypeBuilder<ParameterRow> b)
    {
        b.ToTable("parameters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        b.Property(x => x.Wiersz).HasMaxLength(200);
        b.Property(x => x.ValueNumber).HasPrecision(18, 6);
        b.Property(x => x.ValueText).HasMaxLength(200);
        b.Property(x => x.Unit).HasMaxLength(30);
        b.Property(x => x.LegalBasis).HasMaxLength(500).IsRequired();
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.Note).HasMaxLength(1000);
        b.HasIndex(x => new { x.Key, x.ValidFrom });
    }
}

internal sealed class ParameterHistoryRowConfiguration : IEntityTypeConfiguration<ParameterHistoryRow>
{
    public void Configure(EntityTypeBuilder<ParameterHistoryRow> b)
    {
        b.ToTable("parameter_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        b.Property(x => x.ValueNumber).HasPrecision(18, 6);
        b.Property(x => x.ValueText).HasMaxLength(200);
        b.Property(x => x.PreviousNumber).HasPrecision(18, 6);
        b.Property(x => x.PreviousText).HasMaxLength(200);
        b.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasIndex(x => x.ChangedAt);
    }
}

internal sealed class AdminSettingRowConfiguration : IEntityTypeConfiguration<AdminSettingRow>
{
    public void Configure(EntityTypeBuilder<AdminSettingRow> b)
    {
        b.ToTable("admin_settings");
        b.HasKey(x => x.Name);
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Json).IsRequired();
    }
}
