namespace BabyRadnik.Tests.Reference;

public sealed class ReferenceTests
{
    private static readonly IReadOnlyList<IReferenceEvaluator> Evaluators = typeof(ReferenceTests).Assembly
        .GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IReferenceEvaluator).IsAssignableFrom(t))
        .Select(t => (IReferenceEvaluator)Activator.CreateInstance(t)!)
        .ToList();

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(TestPaths.Fixtures, "reference"), "*.json").Order())
        {
            data.Add(Path.GetFileName(file));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Reference(string file)
    {
        var referenceCase = ReferenceCase.Load(Path.Combine(TestPaths.Fixtures, "reference", file));

        if (referenceCase.DoUzupelnienia)
        {
            Assert.Skip("status: do_uzupelnienia — czeka na dane z dokumentów sprawy");
        }

        var evaluator = Evaluators.FirstOrDefault(e => e.Modul == referenceCase.Modul && e.CanEvaluate(referenceCase));
        if (evaluator is null)
        {
            Assert.Skip($"brak silnika dla modułu '{referenceCase.Modul}'");
        }

        evaluator.Verify(referenceCase);
    }
}
