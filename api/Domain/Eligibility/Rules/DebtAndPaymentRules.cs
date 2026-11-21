using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

public sealed class ContributionDebtRule(DateOnly? onDate = null) : EligibilityRule
{
    public const string RuleId = "zadluzenie_skladkowe";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 2a ust. 1–2";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Zadłużenie z tytułu składek nie przekracza progu w dniu powstania prawa (dotyczy prowadzących działalność i osób współpracujących)";
        var date = onDate ?? context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia.", condition, Ask.EventDate());
        }

        var person = context.PersonFacts;
        var view = Insurance.View(person, date.Value);
        switch (view.State)
        {
            case InsuranceState.Concurrent:
                return Undecided("Zbieg tytułów ubezpieczenia — nie da się ocenić, czy art. 2a ma zastosowanie — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna);
            case InsuranceState.Insured:
                break;
            default:
                return Met("Kontrola zadłużenia nie została przeprowadzona (brak rozpoznanego tytułu ubezpieczenia w tym dniu).", condition);
        }

        var title = view.Title!;
        if (title.RodzajTytulu == TitleKind.Inny)
        {
            return Undecided(
                "Tytuł „inny” może być objęty art. 2a (np. duchowny, marynarz) albo nie — nie rozstrzygam, skontaktuj się z ZUS.",
                condition,
                PodstawaPrawna);
        }

        if (!Insurance.IsBusiness(title))
        {
            return Met($"Tytuł „{Insurance.KindName(title.RodzajTytulu)}” nie podlega kontroli zadłużenia z art. 2a.", condition);
        }

        if (person?.SkladkiZadluzenie?.Value is not { } debt)
        {
            return Missing("Dla prowadzących działalność i osób współpracujących potrzebne jest saldo zadłużenia składkowego na dzień powstania prawa.", condition, Ask.Debt(context.Person));
        }

        var threshold = context.Param(EligibilityParameterKeys.ProgZadluzenia, debt.NaDzien);
        var months = (int)context.Param(EligibilityParameterKeys.TerminSplatyZadluzeniaMies, debt.NaDzien);
        var deadline = debt.NaDzien.AddMonths(months);

        if (debt.Kwota <= threshold)
        {
            return Met(
                $"Zadłużenie {Text.Money(debt.Kwota)} nie przekracza progu {Text.Money(threshold)} w dniu powstania prawa ({Text.Date(debt.NaDzien)}).",
                condition);
        }

        var repaid = person.SkladkiDataSplaty?.Value;
        if (repaid is { } repaidDate && repaidDate <= deadline)
        {
            var payoutDays = context.TryParam(EligibilityParameterKeys.TerminWyplatyPoSplacieDni, debt.NaDzien);
            var payout = payoutDays is { } d
                ? $"Zasiłek wypłaca się nie później niż w ciągu {Text.Number(d)} dni od dnia spłaty całości zadłużenia (art. 64 ust. 3)."
                : "Termin wypłaty po spłacie zadłużenia określa art. 64 ust. 3 ustawy.";
            return Met(
                $"Zadłużenie {Text.Money(debt.Kwota)} przekraczało próg {Text.Money(threshold)}, ale zostało spłacone {Text.Date(repaidDate)}, w terminie do {Text.Date(deadline)} — prawo przysługuje po spłacie.",
                condition,
                payout);
        }

        string reason;
        string whatWouldChange;
        if (repaid is { } late)
        {
            reason = $"Zadłużenie z tytułu składek ({Text.Money(debt.Kwota)}) przekraczało próg {Text.Money(threshold)} w dniu powstania prawa, a spłata ({Text.Date(late)}) nastąpiła po terminie {Text.Date(deadline)}, więc prawo do świadczenia się przedawniło.";
            whatWouldChange = $"Termin spłaty zadłużenia ({Text.Money(debt.Kwota)}) upłynął {Text.Date(deadline)}; skontaktuj się z ZUS.";
        }
        else if (context.AsOf > deadline)
        {
            reason = $"Zadłużenie z tytułu składek ({Text.Money(debt.Kwota)}) przekraczało próg {Text.Money(threshold)} w dniu powstania prawa i nie zostało spłacone w ciągu {Text.Number(months)} miesięcy, więc prawo do świadczenia się przedawniło.";
            whatWouldChange = $"Termin spłaty zadłużenia ({Text.Money(debt.Kwota)}) upłynął {Text.Date(deadline)}; skontaktuj się z ZUS.";
        }
        else
        {
            reason = $"Zadłużenie z tytułu składek ({Text.Money(debt.Kwota)}) przekracza próg {Text.Money(threshold)} w dniu powstania prawa, więc zasiłek nie przysługuje do czasu spłaty całości zadłużenia.";
            whatWouldChange = $"Spłać całość zadłużenia ({Text.Money(debt.Kwota)}) do dnia {Text.Date(deadline)}; po tym terminie prawo się przedawnia.";
        }

        var outcome = NotMet(
            $"Zadłużenie {Text.Money(debt.Kwota)} jest wyższe niż próg {Text.Money(threshold)} (1% minimalnego wynagrodzenia) w dniu powstania prawa {Text.Date(debt.NaDzien)}.",
            condition,
            reason,
            whatWouldChange,
            PodstawaPrawna);
        if (repaid is null && context.AsOf <= deadline)
        {
            outcome = outcome with
            {
                Deadlines =
                [
                    new Deadline(
                        "splata_zadluzenia",
                        DeadlineType.NajpozniejDo,
                        deadline,
                        $"Spłać całość zadłużenia ({Text.Money(debt.Kwota)}) do {Text.Date(deadline)}.",
                        "Po tym terminie prawo do świadczenia się przedawnia.",
                        PodstawaPrawna),
                ],
            };
        }

        return outcome;
    }
}

public sealed class PaymentRouteRule(DateOnly? onDate = null, BenefitKindForDocuments documents = BenefitKindForDocuments.Maternity, bool afterInsuranceEnd = false) : EligibilityRule
{
    public const string RuleId = "platnik_zasilku_i_miejsce_zlozenia";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 61 ust. 1–2";

    public override RuleOutcome Assess(EvaluationContext context) => Resolve(context).Outcome;

    public PaymentRoute Resolve(EvaluationContext context)
    {
        const string condition = "Kto wypłaca zasiłek i gdzie złożyć wniosek (próg liczby ubezpieczonych płatnika)";
        var date = onDate ?? context.EventDate();
        if (date is null)
        {
            return Unknown(Missing("Brak daty zdarzenia.", condition, Ask.EventDate()));
        }

        var info = new List<string> { InfoNote(context, date.Value) };

        if (afterInsuranceEnd)
        {
            return Route(
                Payer.Zus,
                ApplicationPlace.Zus,
                "Zasiłek za okres po ustaniu ubezpieczenia wypłaca ZUS (art. 61 ust. 1 pkt 2 lit. d); wniosek składa się do ZUS.",
                condition,
                BaseDocuments(documents),
                info);
        }

        var view = Insurance.View(context.PersonFacts, date.Value);
        switch (view.State)
        {
            case InsuranceState.Unknown or InsuranceState.DeclaredInsured:
                return Unknown(Missing("Bez rodzaju tytułu ubezpieczenia nie da się ustalić płatnika zasiłku.", condition, Ask.Titles(context.Person)));
            case InsuranceState.Concurrent:
                return Unknown(Undecided("Zbieg tytułów ubezpieczenia — nie ustalam płatnika zasiłku, skontaktuj się z ZUS.", condition, PodstawaPrawna));
            case InsuranceState.NotInsured or InsuranceState.DeclaredNotInsured:
                return Route(Payer.Zus, ApplicationPlace.Zus, "Brak aktywnego tytułu ubezpieczenia w tym dniu — zasiłek wypłaca ZUS (art. 61 ust. 1 pkt 2 lit. d).", condition, BaseDocuments(documents), info);
        }

        var title = view.Title!;
        switch (title.RodzajTytulu)
        {
            case TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca:
                var businessDocs = BaseDocuments(documents);
                if (documents == BenefitKindForDocuments.Sickness && title.RodzajTytulu == TitleKind.Dzialalnosc)
                {
                    businessDocs.Add("ZAS-53 — zaświadczenie dla osoby prowadzącej działalność (zasiłek chorobowy), składane do ZUS");
                }

                return Route(Payer.Zus, ApplicationPlace.Zus, "Prowadzący działalność i osoby współpracujące — zasiłek zawsze wypłaca ZUS (art. 61 ust. 1 pkt 2 lit. b); wniosek do ZUS.", condition, businessDocs, info);
            case TitleKind.Pracownik or TitleKind.Zleceniobiorca:
                if (context.PersonFacts?.PlatnikLiczbaUbezpieczonych?.Value is not { } headcount)
                {
                    return Unknown(Missing("Do ustalenia płatnika zasiłku potrzebna jest liczba ubezpieczonych zgłoszonych przez płatnika.", condition, Ask.Headcount(context.Person)));
                }

                var threshold = (int)context.Param(EligibilityParameterKeys.ProgPlatnikaUbezpieczonych, date.Value);
                if (headcount > threshold)
                {
                    return Route(
                        Payer.PlatnikSkladek,
                        ApplicationPlace.PlatnikSkladek,
                        $"Płatnik zgłasza {headcount} ubezpieczonych (więcej niż {threshold}, stan na 30 listopada poprzedniego roku) — zasiłek wypłaca płatnik składek; wniosek do płatnika (art. 61 ust. 1 pkt 1).",
                        condition,
                        BaseDocuments(documents),
                        info);
                }

                var docs = BaseDocuments(documents);
                var employee = title.RodzajTytulu == TitleKind.Pracownik;
                docs.Add(employee
                    ? "Z-3 — zaświadczenie płatnika składek (pracodawca przekazuje do ZUS)"
                    : "Z-3a — zaświadczenie płatnika składek (zleceniodawca przekazuje do ZUS)");
                return Route(
                    Payer.Zus,
                    employee ? ApplicationPlace.Pracodawca : ApplicationPlace.PlatnikSkladek,
                    $"Płatnik zgłasza {headcount} ubezpieczonych (nie więcej niż {threshold}) — zasiłek wypłaca ZUS (art. 61 ust. 1 pkt 2 lit. a); wniosek {(employee ? "składasz pracodawcy, który przekazuje do ZUS zaświadczenie Z-3" : "składasz płatnikowi składek, który przekazuje do ZUS zaświadczenie Z-3a")}.",
                    condition,
                    docs,
                    info);
            default:
                return Unknown(Undecided("Tytuł „inny” albo zasiłkowy — płatnika zasiłku nie ustalam, skontaktuj się z ZUS.", condition, PodstawaPrawna));
        }
    }

    private static PaymentRoute Unknown(RuleOutcome outcome) => new(null, null, [], outcome);

    private static PaymentRoute Route(Payer payer, ApplicationPlace place, string text, string condition, List<string> documents, List<string> notes) =>
        new(payer, place, documents, new RuleOutcome(RuleResult.Met(text, condition), Notes: notes));

    private static List<string> BaseDocuments(BenefitKindForDocuments kind) => kind switch
    {
        BenefitKindForDocuments.Sickness => ["e-ZLA — zwolnienie lekarskie (zaświadczenie lekarskie o niezdolności do pracy)"],
        BenefitKindForDocuments.Rehabilitation => ["Wniosek o świadczenie rehabilitacyjne (do ZUS; o prawie orzeka lekarz orzecznik ZUS)"],
        _ => ["ZAM — wniosek o zasiłek macierzyński"],
    };

    private static string InfoNote(EvaluationContext context, DateOnly date)
    {
        var days = context.TryParam(EligibilityParameterKeys.TerminWyplatyDni, date);
        var term = days is { } d
            ? $"w ciągu {Text.Number(d)} dni od dnia wyjaśnienia ostatniej okoliczności niezbędnej do ustalenia prawa"
            : "w terminie z art. 64 ust. 1 ustawy";
        return $"Zasiłek wypłaca się najpóźniej {term} (art. 64 ust. 1). Nazwy druków (ZAM, Z-3, Z-3a, ZAS-53) podano wg instrukcji projektu — Z-3, Z-3a i ZAS-53 nie występują w wyciągach przepisów; sprawdź aktualny druk w ZUS.";
    }
}

public static class ZusForms
{
    private static readonly Dictionary<string, (string Name, bool Proposed)> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ZAM"] = ("Wniosek o zasiłek macierzyński", true),
        ["Z-3"] = ("Zaświadczenie płatnika składek (pracownik)", true),
        ["Z-3a"] = ("Zaświadczenie płatnika składek (zleceniobiorca)", true),
        ["ZAS-53"] = ("Zaświadczenie dla osoby prowadzącej działalność (zasiłek chorobowy)", true),
        ["Z-12"] = ("Wniosek o zasiłek pogrzebowy", true),
        ["Z-3b"] = ("Dawne zaświadczenie płatnika składek (zlikwidowane; występuje w starszych sprawach)", false),
    };

    /// <summary>Rozpoznaje nazwę druku w dokumentach sprawy. Z-3b jest rozpoznawany, ale nigdy nie jest proponowany użytkownikowi.</summary>
    public static (bool Recognized, string? Name, bool ProposedToUser) Recognize(string code) =>
        Known.TryGetValue(code.Trim(), out var form) ? (true, form.Name, form.Proposed) : (false, null, false);
}
