namespace BabyRadnik.Tests;

public static class TestPaths
{
    public static readonly string Root = FindRoot();
    public static string Contracts => Path.Combine(Root, "contracts");
    public static string Fixtures => Path.Combine(Root, "fixtures");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "contracts", "openapi.yaml")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Nie znaleziono katalogu repozytorium (contracts/openapi.yaml).");
    }
}
