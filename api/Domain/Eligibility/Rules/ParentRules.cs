using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

public sealed class MotherUninsuredFatherInsuredRule : EligibilityRule
{
    public override string Id => "matka_nieubezpieczona_ojciec_ubezpieczony";
    public override string PodstawaPrawna => "ustawa art. 29a ust. 1 i 3; KP art. 182¹a, art. 182³";

    public static bool Applies(EvaluationContext context)
    {
        var date = context.EventDate();
        if (date is null || context.OtherFacts() is not { } mother)
        {
            return false;
        }

        return Insurance.View(mother, date.Value).State is InsuranceState.NotInsured or InsuranceState.DeclaredNotInsured;
    }

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Brak ubezpieczenia matki nie blokuje prawa ubezpieczonego ojca do zasiłku za urlop rodzicielski i ojcowski";
        return Met(
            "Matka nie jest objęta ubezpieczeniem chorobowym w tym dniu. Prawo ojca do zasiłku za okres urlopu rodzicielskiego i ojcowskiego zależy od jego własnego ubezpieczenia, nie od ubezpieczenia matki.",
            condition);
    }
}

public sealed class FatherTakeoverRule : EligibilityRule
{
    public const string RuleId = "przejecie_zasilku_przez_ojca";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 29 ust. 3–7 i 9";

    public static bool HasTrigger(EvaluationContext context) =>
        context.Facts.Sprawa.MatkaSytuacjaSzczegolna?.Value is not null || context.OtherFacts()?.PlanRezygnacjaZMacierzynskiegoPoTyg?.Value is not null;

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Ubezpieczony ojciec przejmuje zasiłek: rezygnacja matki po co najmniej 14 tygodniach albo sytuacja szczególna matki (próg 8 tygodni)";
        var date = context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia.", condition, Ask.EventDate());
        }

        var mother = context.OtherFacts();
        var motherRef = context.Other();
        var state = Insurance.View(mother, date.Value).State;
        if (state is InsuranceState.Unknown)
        {
            return Missing("Do oceny przejęcia zasiłku potrzebna jest informacja, czy matka jest ubezpieczona.", condition, Ask.Titles(motherRef));
        }

        if (state is InsuranceState.Concurrent)
        {
            return Undecided("Zbieg tytułów ubezpieczenia matki — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna);
        }

        var motherInsured = state is InsuranceState.Insured or InsuranceState.DeclaredInsured;
        var weeks14 = (int)context.Param(EligibilityParameterKeys.MatkaMinPoPorodzieTyg, date.Value);
        var weeks8 = (int)context.Param(EligibilityParameterKeys.MatkaMinPoPorodzieSzczegolneTyg, date.Value);
        var situation = context.Facts.Sprawa.MatkaSytuacjaSzczegolna?.Value;
        var resignation = mother?.PlanRezygnacjaZMacierzynskiegoPoTyg?.Value;

        if (situation is not null)
        {
            return AssessSituation(situation, motherInsured, resignation, date.Value, weeks8, motherRef, condition);
        }

        if (!motherInsured)
        {
            return NotMet(
                "Matka nie jest ubezpieczona, więc nie ma zasiłku, z którego mogłaby zrezygnować na rzecz ojca (art. 29 ust. 3 dotyczy ubezpieczonej matki).",
                condition,
                "Zasiłek nie może zostać przejęty po rezygnacji matki, bo matka nie jest ubezpieczona.",
                "Przejęcie byłoby możliwe, gdyby matka była ubezpieczona i zrezygnowała po co najmniej 14 tygodniach po porodzie (albo zaszła sytuacja szczególna z art. 29 ust. 9). Własne prawa ojca do zasiłku za urlop rodzicielski i ojcowski nie zależą od tego.",
                PodstawaPrawna);
        }

        if (resignation is { } after && after >= weeks14)
        {
            return Met(
                $"Matka rezygnuje po {after} tygodniach po porodzie, czyli po co najmniej {weeks14} tygodniach — resztę zasiłku może przejąć ubezpieczony ojciec.",
                condition);
        }

        return NotMet(
            $"Matka rezygnuje po {resignation} tygodniach po porodzie, a wymagane jest co najmniej {weeks14}.",
            condition,
            $"Rezygnacja matki przed upływem {weeks14} tygodni po porodzie nie pozwala ojcu przejąć zasiłku.",
            $"Ojciec może przejąć zasiłek, gdy matka wykorzysta co najmniej {weeks14} tygodni po porodzie i dopiero wtedy zrezygnuje.",
            "ustawa art. 29 ust. 3");
    }

    private RuleOutcome AssessSituation(MotherSituation situation, bool motherInsured, int? resignation, DateOnly birth, int weeks8, PersonRef motherRef, string condition)
    {
        var threshold = birth.AddDays(7 * weeks8);
        var floorNote = $"Zasiłek ojca najwcześniej za okres po {weeks8} tygodniach po porodzie (od {Text.Date(threshold)}), gdy matka była ubezpieczona i korzystała z zasiłku.";

        if (!motherInsured)
        {
            return situation.Rodzaj switch
            {
                MotherSituationKind.Zgon => Met("Zgon matki nieubezpieczonej — zasiłek przysługuje ubezpieczonemu ojcu za okres po dniu zgonu.", condition),
                MotherSituationKind.PorzucenieDziecka => Met("Porzucenie dziecka przez matkę nieubezpieczoną — zasiłek przysługuje ubezpieczonemu ojcu za okres po dniu zdarzenia.", condition),
                MotherSituationKind.NiezdolnoscDoSamodzielnejEgzystencji => Met("Matka nieubezpieczona z orzeczeniem o niezdolności do samodzielnej egzystencji — zasiłek przysługuje ubezpieczonemu ojcu za okres po dniu zdarzenia.", condition),
                _ => Undecided("Pobyt w szpitalu matki nieubezpieczonej nie jest wymieniony w art. 29 ust. 9 — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna),
            };
        }

        switch (situation.Rodzaj)
        {
            case MotherSituationKind.Zgon:
                return Met("Zgon ubezpieczonej matki — zasiłek macierzyński przysługuje ubezpieczonemu ojcu (art. 29 ust. 7).", condition);
            case MotherSituationKind.PorzucenieDziecka:
                return Met("Porzucenie dziecka przez ubezpieczoną matkę — zasiłek przysługuje ubezpieczonemu ojcu za okres po dniu porzucenia, niewcześniej niż po 8 tygodniach zasiłku matki po porodzie (art. 29 ust. 7).", condition, floorNote);
            case MotherSituationKind.PobytWSzpitalu:
                if (situation.Do is { } end && end < threshold)
                {
                    return NotMet(
                        $"Pobyt matki w szpitalu zakończył się {Text.Date(end)}, przed upływem {weeks8} tygodni po porodzie ({Text.Date(threshold)}).",
                        condition,
                        $"Pobyt matki w szpitalu zakończył się przed upływem {weeks8} tygodni po porodzie, a prawo ojca powstaje dopiero po wykorzystaniu przez matkę co najmniej {weeks8} tygodni zasiłku.",
                        $"Prawo ojca dotyczyłoby pobytu matki w szpitalu przypadającego po {weeks8} tygodniach po porodzie.",
                        "ustawa art. 29 ust. 6");
                }

                return Met($"Pobyt ubezpieczonej matki w szpitalu — ojcu przysługuje zasiłek za okres pobytu po wykorzystaniu przez matkę co najmniej {weeks8} tygodni po porodzie (art. 29 ust. 6).", condition, floorNote);
            default:
                if (resignation is null)
                {
                    return Missing("Przy niezdolności matki do samodzielnej egzystencji ojciec przejmuje zasiłek po jej rezygnacji — potrzebna informacja, po ilu tygodniach matka rezygnuje.", condition, Ask.Resignation(motherRef));
                }

                return resignation >= weeks8
                    ? Met($"Matka z orzeczeniem o niezdolności do samodzielnej egzystencji rezygnuje po {resignation} tygodniach (co najmniej {weeks8}) — zasiłek przysługuje ubezpieczonemu ojcu (art. 29 ust. 5).", condition)
                    : NotMet(
                        $"Matka rezygnuje po {resignation} tygodniach po porodzie, a wymagane jest co najmniej {weeks8}.",
                        condition,
                        $"Rezygnacja matki przed upływem {weeks8} tygodni po porodzie nie pozwala ojcu przejąć zasiłku.",
                        $"Ojciec może przejąć zasiłek, gdy matka wykorzysta co najmniej {weeks8} tygodni po porodzie i dopiero wtedy zrezygnuje.",
                        "ustawa art. 29 ust. 5");
        }
    }
}
