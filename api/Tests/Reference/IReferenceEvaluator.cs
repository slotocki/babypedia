namespace BabyRadnik.Tests.Reference;

/// <summary>
/// Ewaluator przypadków referencyjnych jednego modułu silnika. Gałąź dodaje własną implementację
/// w swoim lustrzanym katalogu testów (np. Tests/Domain/Periods/PeriodsReferenceEvaluator.cs);
/// ReferenceTests wykrywa ją przez refleksję. Klasa musi mieć konstruktor bezparametrowy.
/// </summary>
public interface IReferenceEvaluator
{
    /// <summary>Wartość pola "modul" z pliku referencyjnego, np. "okresy".</summary>
    string Modul { get; }

    /// <summary>false = ten ewaluator jeszcze nie obsługuje przypadku (test pominięty, nie zielony).</summary>
    bool CanEvaluate(ReferenceCase referenceCase) => true;

    /// <summary>Rzuca wyjątek asercji przy niezgodności. Zwykle: wynik silnika → ReferenceAssert.Matches.</summary>
    void Verify(ReferenceCase referenceCase);
}
