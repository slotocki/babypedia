using Microsoft.Extensions.Configuration;

namespace BabyRadnik.Infrastructure.Common;

/// <summary>Położenie katalogów contracts/ i fixtures/ oraz prywatnego katalogu spraw ZUS (poza repo, sekcja 14).</summary>
public sealed class RepoPaths
{
    public required string Root { get; init; }
    public string Contracts => Path.Combine(Root, "contracts");
    public string Fixtures => Path.Combine(Root, "fixtures");
    public string? CasesPrivateDir { get; init; }

    public static RepoPaths Resolve(IConfiguration configuration, string startDirectory)
    {
        var root = configuration["Paths:Root"] ?? FindRoot(startDirectory) ?? FindRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Nie znaleziono katalogu z contracts/. Ustaw Paths__Root.");
        var casesDir = configuration["CASES_PRIVATE_DIR"];
        return new RepoPaths { Root = root, CasesPrivateDir = string.IsNullOrWhiteSpace(casesDir) ? null : casesDir };
    }

    private static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "contracts", "openapi.yaml")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
