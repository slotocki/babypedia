using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Amounts.AmountMath;

namespace BabyRadnik.Domain.Amounts;

/// <summary>Tytuł ubezpieczenia istotny dla minimalnej podstawy i limitu: rodzaj, ubezpieczenie chorobowe, wymiar etatu (1 = pełny).</summary>
public sealed record BasisProfile(TitleKind Title, SicknessInsurance Sickness, decimal EtatFraction = 1m);

public sealed record AdjustedBasis(decimal Amount, BasisSource Source, IReadOnlyList<TraceStep> Trace, IReadOnlyList<string> Notes);

internal sealed class OdliczenieSkladekRule : IAmountRule
{
    public string Id => "odliczenie_skladek";

    public string PodstawaPrawna => "ustawa art. 3 pkt 3–4; Załącznik ZUS (Podstawa wymiaru zasiłków)";
}

internal sealed class MinimalnaPodstawaRule : IAmountRule
{
    public string Id => "minimalna_podstawa";

    public string PodstawaPrawna => "ustawa art. 45 ust. 1 w zw. z art. 47";
}

internal sealed class LimitPodstawyRule : IAmountRule
{
    public string Id => "limit_podstawy_dobrowolne";

    public string PodstawaPrawna => "Załącznik ZUS (dla osób niebędących pracownikami)";
}

/// <summary>
/// Funkcje pomocnicze do podstawy wymiaru: odliczenie 13,71% od przychodu, minimalna podstawa pracownika i limit podstawy
/// dla dobrowolnie ubezpieczonych. Pełne ustalanie podstawy z 12 miesięcy należy do modułu Basis.
/// </summary>
public sealed class BasisAdjuster(IParameterProvider parameters)
{
    private static readonly OdliczenieSkladekRule Odliczenie = new();
    private static readonly MinimalnaPodstawaRule Minimalna = new();
    private static readonly LimitPodstawyRule Limit = new();

    /// <param name="monthlyRevenue">Przeciętny miesięczny przychód (podstawa składek na ubezpieczenie chorobowe) przed odliczeniem 13,71%.</param>
    /// <param name="date">Dzień, wg którego wybieram parametry (płaca minimalna, limity): pierwszy dzień zasiłku.</param>
    public AdjustedBasis FromRevenue(decimal monthlyRevenue, DateOnly date, BasisProfile profile)
    {
        var trace = new List<TraceStep>();
        var notes = new List<string>();
        var deduction = parameters.GetDecimal(AmountsParameterKeys.OdliczenieSkladek, date);
        var net = Round(monthlyRevenue * (1 - deduction), 2);
        trace.Add(AmountTrace.Of(Odliczenie, true,
            $"Przychód {Money(monthlyRevenue)} − {Percent(deduction)} (składki) = {Money(net)}.",
            "podstawa = przychód po odliczeniu 13,71%"));

        if (profile.Title == TitleKind.Pracownik)
        {
            var fullMinimum = parameters.GetDecimal(AmountsParameterKeys.PodstawaMinimalnaPelnyEtat, date);
            var minimum = Round(fullMinimum * profile.EtatFraction, 2);
            if (profile.EtatFraction < 1)
            {
                notes.Add("Przy niepełnym etacie minimalną podstawę liczymy proporcjonalnie do wymiaru czasu pracy. To przyjęte założenie: art. 45 ust. 1 ustawy mówi o pracy w pełnym wymiarze czasu pracy.");
            }

            if (net < minimum)
            {
                trace.Add(AmountTrace.Of(Minimalna, true,
                    $"Podstawa {Money(net)} jest niższa niż minimalna {Money(minimum)} (płaca minimalna po odliczeniu 13,71%{(profile.EtatFraction < 1 ? $", × {Number(profile.EtatFraction)} etatu" : "")}) — przyjmuję minimalną.",
                    "podstawa nie niższa niż minimalna dla pracownika"));
                return new AdjustedBasis(minimum, BasisSource.Minimalna, trace, notes);
            }

            trace.Add(AmountTrace.Of(Minimalna, false,
                $"Podstawa {Money(net)} nie jest niższa niż minimalna {Money(minimum)}.",
                "podstawa nie niższa niż minimalna dla pracownika"));
            return new AdjustedBasis(net, BasisSource.SzacunekZus, trace, notes);
        }

        if (profile.Sickness == SicknessInsurance.Dobrowolne)
        {
            var maxRevenue = parameters.GetDecimal(AmountsParameterKeys.MaxPodstawaDobrowolnej, date);
            var limit = parameters.GetDecimal(AmountsParameterKeys.LimitPodstawyZasilku, date);
            if (monthlyRevenue > maxRevenue)
            {
                var basis = Math.Min(net, limit);
                trace.Add(AmountTrace.Of(Limit, true,
                    $"Przychód {Money(monthlyRevenue)} przekracza maksymalną podstawę dobrowolnej składki chorobowej {Money(maxRevenue)} — podstawa zasiłku nie może być wyższa niż {Money(limit)}.",
                    "podstawa zasiłku nie wyższa niż limit dla dobrowolnie ubezpieczonych"));
                return new AdjustedBasis(basis, BasisSource.SzacunekZus, trace, notes);
            }

            trace.Add(AmountTrace.Of(Limit, false,
                $"Przychód {Money(monthlyRevenue)} nie przekracza maksymalnej podstawy {Money(maxRevenue)} — limit {Money(limit)} nie obowiązuje.",
                "podstawa zasiłku nie wyższa niż limit dla dobrowolnie ubezpieczonych"));
        }

        return new AdjustedBasis(net, BasisSource.SzacunekZus, trace, notes);
    }
}
