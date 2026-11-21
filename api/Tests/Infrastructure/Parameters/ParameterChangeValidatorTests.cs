using System.Text.Json;
using BabyRadnik.Infrastructure.Parameters;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

public sealed class ParameterChangeValidatorTests
{
    private static readonly IReadOnlyList<ParameterRow> Existing =
    [
        TestRows.Number("stawka", 1m, "2000-01-01", "ulamek", 1),
        TestRows.Number("tygodnie", 20m, "2000-01-01", "tygodnie", 2),
        TestRows.Number("tryb", 0m, "2000-01-01", "ustawienie", 3),
    ];

    private static Dictionary<string, string[]> Errors(string? key, string valueJson, string? validFrom = "2027-01-01") =>
        ParameterChangeValidator.Validate(
            new ParameterChangeRequest(key, JsonDocument.Parse(valueJson).RootElement, validFrom, null, null), Existing).Errors;

    [Fact]
    public void Valid_change_inherits_unit_and_legal_basis()
    {
        var (row, errors) = ParameterChangeValidator.Validate(
            new ParameterChangeRequest("stawka", JsonDocument.Parse("0.75").RootElement, "2027-01-01", null, null), Existing);

        Assert.Empty(errors);
        Assert.NotNull(row);
        Assert.Equal("ulamek", row.Unit);
        Assert.Equal("podstawa testowa", row.LegalBasis);
        Assert.Equal(0.75m, row.ValueNumber);
    }

    [Fact]
    public void New_row_inherits_status_of_previous_row_unless_request_sets_it()
    {
        IReadOnlyList<ParameterRow> existing =
        [
            TestRows.Number("stawka", 1m, "2000-01-01", "ulamek", 1),
            new ParameterRow
            {
                Id = 2, Key = "stawka", ValueNumber = 0.9m, Unit = "ulamek", ValidFrom = new DateOnly(2026, 1, 1),
                LegalBasis = "podstawa testowa", Status = ParameterStatus.DoWeryfikacji,
            },
        ];
        var value = JsonDocument.Parse("0.75").RootElement;

        var inherited = ParameterChangeValidator.Validate(new ParameterChangeRequest("stawka", value, "2027-01-01", null, null), existing).Row;
        var confirmed = ParameterChangeValidator.Validate(new ParameterChangeRequest("stawka", value, "2027-01-01", null, null, ParameterStatus.Ok), existing).Row;
        var marked = ParameterChangeValidator.Validate(new ParameterChangeRequest("stawka", value, "2027-01-01", null, null, ParameterStatus.DoWeryfikacji), Existing).Row;
        var invalid = ParameterChangeValidator.Validate(new ParameterChangeRequest("stawka", value, "2027-01-01", null, null, "zatwierdzone"), existing).Errors;

        Assert.Equal(ParameterStatus.DoWeryfikacji, inherited!.Status);
        Assert.Equal(ParameterStatus.Ok, confirmed!.Status);
        Assert.Equal(ParameterStatus.DoWeryfikacji, marked!.Status);
        Assert.Contains("status", invalid.Keys);
    }

    [Theory]
    [InlineData(null, "1", "2027-01-01", "key")]
    [InlineData("Zly Klucz", "1", "2027-01-01", "key")]
    [InlineData("nieistnieje", "1", "2027-01-01", "key")]
    [InlineData("stawka", "1", null, "validFrom")]
    [InlineData("stawka", "1", "01.01.2027", "validFrom")]
    [InlineData("stawka", "1", "1800-01-01", "validFrom")]
    [InlineData("stawka", "1.5", "2027-01-01", "value")]
    [InlineData("stawka", "-0.1", "2027-01-01", "value")]
    [InlineData("stawka", "\"abc\"", "2027-01-01", "value")]
    [InlineData("stawka", "true", "2027-01-01", "value")]
    [InlineData("tygodnie", "20.5", "2027-01-01", "value")]
    [InlineData("tryb", "5", "2027-01-01", "value")]
    public void Invalid_input_is_reported_per_field(string? key, string valueJson, string? validFrom, string field)
    {
        Assert.Contains(field, Errors(key, valueJson, validFrom).Keys);
    }

    [Fact]
    public void Missing_value_is_rejected_but_null_is_allowed()
    {
        var missing = ParameterChangeValidator.Validate(
            new ParameterChangeRequest("stawka", default, "2027-01-01", null, null), Existing);
        Assert.Contains("value", missing.Errors.Keys);

        Assert.Empty(Errors("stawka", "null"));
    }

    [Fact]
    public void Text_setting_accepts_string()
    {
        Assert.Empty(Errors("tryb", "\"brutto\""));
    }
}
