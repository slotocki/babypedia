using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

internal sealed class Run(EvaluationContext context)
{
    private (EligibilityRule Rule, RuleOutcome Outcome)? _decisive;

    public EvaluationContext Context { get; } = context;
    public List<TraceStep> Trace { get; } = [];
    public List<MissingFact> Missing { get; } = [];
    public List<string> Notes { get; } = [];
    public bool Denied => _decisive?.Outcome.Verdict == RuleVerdict.NotMet;
    public RuleVerdict? Pending => _decisive?.Outcome.Verdict;

    public RuleOutcome Do(EligibilityRule rule, bool affectsDecision = true)
    {
        RuleOutcome outcome;
        try
        {
            outcome = rule.Assess(Context);
        }
        catch (ParameterNotFoundException e)
        {
            outcome = new RuleOutcome(
                new RuleResult(RuleVerdict.Undecided, $"Brak parametru „{e.Key}” obowiązującego w dniu {e.Date:yyyy-MM-dd} — nie rozstrzygam."),
                $"Brak parametru „{e.Key}” obowiązującego w dniu {e.Date:yyyy-MM-dd} — nie rozstrzygam.");
        }

        Trace.Add(rule.ToTrace(outcome.Result));
        if (outcome.Result.Missing is { } missing)
        {
            foreach (var m in missing.Where(m => !Missing.Any(x => x.Fact == m.Fact && x.Person == m.Person)))
            {
                Missing.Add(m);
            }
        }

        if (outcome.Notes is { } notes)
        {
            Notes.AddRange(notes);
        }

        if (affectsDecision)
        {
            Record(rule, outcome);
        }

        return outcome;
    }

    public void Record(EligibilityRule rule, RuleOutcome outcome)
    {
        var rank = Rank(outcome.Verdict);
        if (rank > 0 && (_decisive is null || rank > Rank(_decisive.Value.Outcome.Verdict)))
        {
            _decisive = (rule, outcome);
        }
    }

    private static int Rank(RuleVerdict verdict) => verdict switch
    {
        RuleVerdict.NotMet => 3,
        RuleVerdict.Undecided => 2,
        RuleVerdict.MissingData => 1,
        _ => 0,
    };

    public BenefitEligibility Finish(BenefitId id, PersonRef person, PaymentRoute? route = null)
    {
        var contact = new List<Link> { new("Kontakt z ZUS", "https://www.zus.pl/kontakt") };
        if (_decisive is not { } d)
        {
            var notes = Notes.Concat(route?.Outcome.Notes ?? []).Distinct().ToList();
            if (route is not null && route.Outcome.Verdict is RuleVerdict.MissingData or RuleVerdict.Undecided && route.Payer is null)
            {
                notes.Add($"Płatnika zasiłku nie ustalono: {route.Outcome.Result.Text}");
            }

            if (route?.Outcome.Result.Missing is { } routeMissing)
            {
                foreach (var m in routeMissing.Where(m => !Missing.Any(x => x.Fact == m.Fact && x.Person == m.Person)))
                {
                    Missing.Add(m);
                }
            }

            if (route is not null)
            {
                Trace.Add(new PaymentRouteRule().ToTrace(route.Outcome.Result));
            }

            return Build(new BenefitResult
            {
                Id = id,
                Person = person,
                Status = BenefitStatus.Przysluguje,
                Payer = route?.Payer,
                WhereToApply = route?.WhereToApply,
                Documents = route?.Documents ?? [],
                Notes = notes,
                Trace = Trace,
            });
        }

        var (rule, outcome) = d;
        var allNotes = Notes.Distinct().ToList();
        switch (outcome.Verdict)
        {
            case RuleVerdict.NotMet:
                return Build(new BenefitResult
                {
                    Id = id,
                    Person = person,
                    Status = BenefitStatus.NiePrzysluguje,
                    Reason = outcome.Reason ?? outcome.Result.Text,
                    LegalBasis = outcome.LegalBasis ?? rule.PodstawaPrawna,
                    WhatWouldChange = outcome.WhatWouldChange,
                    Notes = allNotes,
                    Trace = Trace,
                });
            case RuleVerdict.Undecided:
                allNotes.Add("Skontaktuj się z ZUS: infolinia 22 560 16 00, www.zus.pl/kontakt, albo placówka ZUS.");
                return Build(new BenefitResult
                {
                    Id = id,
                    Person = person,
                    Status = BenefitStatus.NieRozstrzygam,
                    Reason = outcome.Reason ?? outcome.Result.Text,
                    LegalBasis = outcome.LegalBasis ?? rule.PodstawaPrawna,
                    Notes = allNotes,
                    Links = contact,
                    Trace = Trace,
                });
            default:
                return Build(new BenefitResult
                {
                    Id = id,
                    Person = person,
                    Status = BenefitStatus.BrakujeDanych,
                    Notes = allNotes,
                    Trace = Trace,
                });
        }
    }

    private BenefitEligibility Build(BenefitResult result) => new(result, Missing.ToList());
}

public sealed class EligibilityService(IParameterProvider parameters) : IEligibilityService
{
    private static readonly BenefitId[] AssessedBenefits =
    [
        BenefitId.Macierzynski,
        BenefitId.PrzyjecieDziecka,
        BenefitId.ZasilekDoDniaPorodu,
        BenefitId.ChorobowyWCiazy,
        BenefitId.Rehabilitacyjne,
        BenefitId.Rodzicielski,
        BenefitId.Ojcowski,
        BenefitId.PrzejeciePrzezOjca,
    ];

    public EligibilityAssessment Assess(FactSet facts, DateOnly asOf) => Assess(facts, asOf, parameters);

    public EligibilityAssessment Assess(FactSet facts, DateOnly asOf, IParameterProvider provider)
    {
        var benefits = new List<BenefitResult>();
        var missing = new List<MissingFact>();
        var persons = new List<PersonRef> { PersonRef.Wnioskodawca };
        if (facts.DrugiRodzic is not null)
        {
            persons.Add(PersonRef.DrugiRodzic);
        }

        foreach (var person in persons)
        {
            foreach (var id in AssessedBenefits)
            {
                var evaluation = Evaluate(id, facts, person, asOf, provider);
                benefits.Add(evaluation.Result);
                foreach (var m in evaluation.MissingFacts.Where(m => !missing.Any(x => x.Fact == m.Fact && x.Person == m.Person)))
                {
                    missing.Add(m);
                }
            }
        }

        return new EligibilityAssessment(benefits, missing, RiskCatalog.For(facts, asOf, provider, persons));
    }

    public BenefitEligibility Evaluate(BenefitId benefit, FactSet facts, PersonRef person, DateOnly asOf) =>
        Evaluate(benefit, facts, person, asOf, parameters);

    public BenefitEligibility Evaluate(BenefitId benefit, FactSet facts, PersonRef person, DateOnly asOf, IParameterProvider provider)
    {
        var context = new EvaluationContext(facts, person, asOf, provider);
        if (facts.For(person) is null)
        {
            return NotApplicable(benefit, person, "Brak faktów o tej osobie.");
        }

        if (context.Event() is null)
        {
            return Missing(benefit, person, Ask.Event());
        }

        var role = context.Role();
        var isAdoption = context.Event() == EventKind.Przyjecie;

        switch (benefit)
        {
            case BenefitId.Macierzynski or BenefitId.MacierzynskiUzupelniajacy:
                if (isAdoption)
                {
                    return NotApplicable(benefit, person, "Przy przyjęciu dziecka zasiłek dotyczy świadczenia „przyjecie_dziecka”.");
                }

                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Ojciec
                    ? NotApplicable(benefit, person, "Zasiłek macierzyński za okres urlopu macierzyńskiego dotyczy matki; ojciec może przejąć go w sytuacjach szczególnych (świadczenie „przejecie_przez_ojca”).")
                    : MotherRight(benefit, context, allowEndExceptions: true);

            case BenefitId.PrzyjecieDziecka:
                return isAdoption ? OwnInsuranceRight(benefit, context, null) : NotApplicable(benefit, person, "Dotyczy tylko przyjęcia dziecka na wychowanie.");

            case BenefitId.Rodzicielski:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Matka && !isAdoption
                    ? MotherRight(benefit, context, allowEndExceptions: true)
                    : OwnInsuranceRight(benefit, context, LeaveStart(context, FactKeys.PlanRodzicielskiCzesci));

            case BenefitId.Ojcowski:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Ojciec
                    ? OwnInsuranceRight(benefit, context, LeaveStart(context, FactKeys.PlanOjcowskiCzesci))
                    : NotApplicable(benefit, person, "Urlop ojcowski dotyczy ojca dziecka.");

            case BenefitId.PrzejeciePrzezOjca:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Ojciec && FatherTakeoverRule.HasTrigger(context)
                    ? Takeover(context)
                    : NotApplicable(benefit, person, "Brak rezygnacji matki ani sytuacji szczególnej matki, albo osoba nie jest ojcem.");

            case BenefitId.ZasilekDoDniaPorodu:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Matka && !isAdoption ? BenefitUntilBirth(context) : NotApplicable(benefit, person, "Dotyczy pracownicy w ciąży, której zatrudnienie ustało z przyczyn z art. 30 ust. 3.");

            case BenefitId.ChorobowyWCiazy:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Matka && !isAdoption ? Sickness(context, rehabilitation: false) : NotApplicable(benefit, person, "Dotyczy niezdolności do pracy w okresie ciąży.");

            case BenefitId.Rehabilitacyjne:
                if (role is null)
                {
                    return Missing(benefit, person, Ask.Role(person));
                }

                return role == ParentRole.Matka && !isAdoption ? Sickness(context, rehabilitation: true) : NotApplicable(benefit, person, "Dotyczy ciąży po wyczerpaniu zasiłku chorobowego.");

            default:
                return NotApplicable(benefit, person, "Świadczenie nie jest oceniane w module uprawnień.");
        }
    }

    private static BenefitEligibility NotApplicable(BenefitId id, PersonRef person, string note) =>
        new(new BenefitResult { Id = id, Person = person, Status = BenefitStatus.NieDotyczy, Notes = [note] }, []);

    private static BenefitEligibility Missing(BenefitId id, PersonRef person, MissingFact fact) =>
        new(new BenefitResult { Id = id, Person = person, Status = BenefitStatus.BrakujeDanych }, [fact]);

    private static DateOnly? LeaveStart(EvaluationContext context, string planKey)
    {
        var person = context.PersonFacts;
        IReadOnlyList<LeavePart>? parts = planKey == FactKeys.PlanOjcowskiCzesci
            ? person?.PlanOjcowskiCzesci?.Value
            : person?.PlanRodzicielskiCzesci?.Value;
        var starts = parts?.Where(p => p.Od is not null).Select(p => p.Od!.Value).ToList();
        return starts is { Count: > 0 } ? starts.Min() : null;
    }

    private static BenefitEligibility MotherRight(BenefitId id, EvaluationContext context, bool allowEndExceptions)
    {
        var run = new Run(context);
        var date = context.EventDate();
        var insuranceRule = new InsuranceOnDateRule();
        var insurance = run.Do(insuranceRule, affectsDecision: false);
        var afterEnd = false;

        if (insurance.Verdict == RuleVerdict.NotMet && allowEndExceptions && Insurance.Titles(context.PersonFacts) is not null)
        {
            var exceptionRule = new InsuranceEndExceptionRule();
            var exception = run.Do(exceptionRule, affectsDecision: false);
            if (exception.Verdict == RuleVerdict.Met)
            {
                afterEnd = true;
            }
            else
            {
                var final = exception.Verdict == RuleVerdict.NotMet && (exception.LegalBasis is null || exception.LegalBasis == exceptionRule.PodstawaPrawna)
                    ? exception with { LegalBasis = "ustawa art. 29 ust. 1, art. 30 ust. 1" }
                    : exception;
                run.Record(exceptionRule, final);
                return run.Finish(id, context.Person);
            }
        }
        else
        {
            run.Record(insuranceRule, insurance);
            if (run.Denied)
            {
                if (allowEndExceptions)
                {
                    run.Notes.Add("Bez historii tytułów ubezpieczenia nie sprawdzono wyjątków z art. 30 (ustanie ubezpieczenia w ciąży).");
                }

                return run.Finish(id, context.Person);
            }
        }

        if (!afterEnd)
        {
            run.Do(new TitleFeaturesRule());
            run.Do(new ContributionDebtRule());
            if (run.Denied)
            {
                return run.Finish(id, context.Person);
            }
        }

        if (run.Pending is not null || date is null)
        {
            return run.Finish(id, context.Person);
        }

        var view = Insurance.View(context.PersonFacts, date.Value);
        var endsBefore = view.Title?.Do is { } end && end <= date.Value;
        var route = new PaymentRouteRule(null, BenefitKindForDocuments.Maternity, afterEnd || endsBefore).Resolve(context);
        if (afterEnd)
        {
            run.Notes.Add("Ubezpieczenie ustało przed porodem — zasiłek przysługuje na podstawie art. 30 i wypłaca go ZUS.");
        }
        else if (endsBefore)
        {
            run.Notes.Add("Ubezpieczenie kończy się w dniu porodu lub wcześniej — za okres po jego ustaniu zasiłek wypłaca ZUS (art. 61 ust. 1 pkt 2 lit. d).");
        }

        return run.Finish(id, context.Person, route);
    }

    private static BenefitEligibility OwnInsuranceRight(BenefitId id, EvaluationContext context, DateOnly? leaveStart)
    {
        var run = new Run(context);
        var date = leaveStart ?? context.EventDate();
        var subject = leaveStart is null ? null : "w dniu rozpoczęcia urlopu";
        run.Do(new InsuranceOnDateRule(date, subject));
        if (run.Denied)
        {
            return run.Finish(id, context.Person);
        }

        run.Do(new TitleFeaturesRule(date));
        if (context.Role() == ParentRole.Ojciec && id != BenefitId.PrzyjecieDziecka && MotherUninsuredFatherInsuredRule.Applies(context))
        {
            run.Do(new MotherUninsuredFatherInsuredRule(), affectsDecision: false);
        }

        run.Do(new ContributionDebtRule(date));
        if (run.Denied || run.Pending is not null || date is null)
        {
            return run.Finish(id, context.Person);
        }

        var view = Insurance.View(context.PersonFacts, date.Value);
        var endsBefore = view.Title?.Do is { } end && end <= date.Value;
        var route = new PaymentRouteRule(date, BenefitKindForDocuments.Maternity, endsBefore).Resolve(context);
        return run.Finish(id, context.Person, route);
    }

    private static BenefitEligibility Takeover(EvaluationContext context)
    {
        var run = new Run(context);
        run.Do(new InsuranceOnDateRule(null, "ojca w dniu zdarzenia"));
        if (run.Denied)
        {
            return run.Finish(BenefitId.PrzejeciePrzezOjca, context.Person);
        }

        run.Do(new ContributionDebtRule());
        if (run.Denied)
        {
            return run.Finish(BenefitId.PrzejeciePrzezOjca, context.Person);
        }

        run.Do(new FatherTakeoverRule());
        if (run.Denied || run.Pending is not null || context.EventDate() is not { } date)
        {
            return run.Finish(BenefitId.PrzejeciePrzezOjca, context.Person);
        }

        var route = new PaymentRouteRule(date, BenefitKindForDocuments.Maternity).Resolve(context);
        run.Notes.Add("Ojciec musi uzyskać prawo do urlopu macierzyńskiego (pracownik) albo przerwać działalność w celu opieki nad dzieckiem (art. 29 ust. 3).");
        return run.Finish(BenefitId.PrzejeciePrzezOjca, context.Person, route);
    }

    private static BenefitEligibility BenefitUntilBirth(EvaluationContext context)
    {
        var id = BenefitId.ZasilekDoDniaPorodu;
        if (context.EventDate() is not { } date)
        {
            return Missing(id, context.Person, Ask.EventDate());
        }

        var person = context.PersonFacts;
        var ended = InsuranceEndExceptionRule.LastEndedTitle(person, date);
        var view = Insurance.View(person, date);
        if (view.State != InsuranceState.NotInsured || ended is null || !BenefitUntilBirthRule.IsEligibleReason(person?.UbezpieczenieUstaniePrzyczyna?.Value))
        {
            return NotApplicable(id, context.Person, "Zasiłek do dnia porodu przysługuje pracownicy, której zatrudnienie ustało w ciąży wskutek śmierci, upadłości lub likwidacji pracodawcy.");
        }

        var run = new Run(context);
        run.Do(new BenefitUntilBirthRule());
        if (run.Pending is not null)
        {
            return run.Finish(id, context.Person);
        }

        var route = new PaymentRouteRule(date, BenefitKindForDocuments.Maternity, afterInsuranceEnd: true).Resolve(context);
        run.Notes.Add($"Zasiłek przysługuje do dnia porodu, za okres od {Text.Date(ended.Do!.Value.AddDays(1))}.");
        return run.Finish(id, context.Person, route);
    }

    private static BenefitEligibility Sickness(EvaluationContext context, bool rehabilitation)
    {
        var id = rehabilitation ? BenefitId.Rehabilitacyjne : BenefitId.ChorobowyWCiazy;
        var kind = context.Event();
        var usage = SicknessCalc.Usage(context);
        if (kind == EventKind.Planuje || (kind != EventKind.Ciaza && !usage.HasLeaves) || (rehabilitation && !usage.HasLeaves))
        {
            return NotApplicable(id, context.Person, "Brak zwolnień lekarskich w ciąży.");
        }

        var firstDay = usage.BlockStart ?? context.AsOf;
        var run = new Run(context);

        if (rehabilitation)
        {
            var period = run.Do(new SicknessPeriodRule(), affectsDecision: false);
            if (period.Verdict != RuleVerdict.NotMet)
            {
                return NotApplicable(id, context.Person, "Świadczenie rehabilitacyjne może przysługiwać dopiero po wyczerpaniu okresu zasiłkowego.");
            }

            var insuranceRule = new InsuranceOnDateRule(firstDay, "w pierwszym dniu niezdolności do pracy");
            var insured = run.Do(insuranceRule, affectsDecision: false);
            if (insured.Verdict != RuleVerdict.Met)
            {
                run.Do(new RehabilitationRule());
                return run.Finish(id, context.Person);
            }

            run.Do(new ContributionDebtRule(firstDay));
            run.Do(new RehabilitationRule());
            return run.Finish(id, context.Person);
        }

        if (!usage.HasLeaves)
        {
            run.Notes.Add($"Brak zwolnień w danych — ocena dotyczy niezdolności do pracy powstałej w dniu oceny ({Text.Date(firstDay)}).");
        }

        var firstInsuranceRule = new InsuranceOnDateRule(firstDay, "w pierwszym dniu niezdolności do pracy");
        var insurance = run.Do(firstInsuranceRule, affectsDecision: false);
        if (insurance.Verdict == RuleVerdict.NotMet)
        {
            run.Do(new SicknessAfterInsuranceEndRule(firstDay));
            return run.Finish(id, context.Person);
        }

        run.Record(firstInsuranceRule, insurance);
        if (run.Pending is not null)
        {
            return run.Finish(id, context.Person);
        }

        run.Do(new TitleFeaturesRule(firstDay));

        var waitingRule = new WaitingPeriodRule(firstDay);
        var waiting = run.Do(waitingRule, affectsDecision: false);
        if (waiting.Verdict == RuleVerdict.NotMet)
        {
            var exception = run.Do(new WaitingPeriodExceptionRule(firstDay), affectsDecision: false);
            if (exception.Verdict != RuleVerdict.Met)
            {
                run.Record(waitingRule, waiting);
                return run.Finish(id, context.Person);
            }
        }
        else
        {
            run.Record(waitingRule, waiting);
        }

        run.Do(new SicknessPeriodRule());
        if (run.Denied)
        {
            return run.Finish(id, context.Person);
        }

        run.Do(new SicknessWagesRule(firstDay), affectsDecision: false);
        run.Do(new ContributionDebtRule(firstDay));
        if (run.Denied || run.Pending is not null)
        {
            return run.Finish(id, context.Person);
        }

        var view = Insurance.View(context.PersonFacts, firstDay);
        if (view.Title?.Do is { } end && usage.BlockEnd is { } blockEnd && blockEnd > end)
        {
            run.Notes.Add($"Zwolnienie trwa po ustaniu tytułu ubezpieczenia ({Text.Date(end)}) — za okres po ustaniu o zasiłku decydują art. 13 i art. 8 ust. 2 ustawy; tej części nie rozstrzygam.");
        }

        var route = new PaymentRouteRule(firstDay, BenefitKindForDocuments.Sickness).Resolve(context);
        run.Notes.Add("Okres zasiłkowy w ciąży to do 270 dni, zasiłek 100% podstawy wymiaru; o wykorzystaniu zwolnienia niezgodnie z celem albo pracy w czasie zwolnienia decyduje ZUS (art. 17).");
        return run.Finish(id, context.Person, route);
    }
}

internal static class RiskCatalog
{
    public static IReadOnlyList<Risk> For(FactSet facts, DateOnly asOf, IParameterProvider parameters, IReadOnlyList<PersonRef> persons)
    {
        var risks = new Dictionary<string, Risk>();
        foreach (var person in persons)
        {
            var context = new EvaluationContext(facts, person, asOf, parameters);
            var personFacts = facts.For(person);
            var date = context.EventDate();
            var titles = Insurance.Titles(personFacts);

            if (titles is not null && date is { } d)
            {
                if (titles.Any(t => Insurance.IsSickness(t) && t.Do is { } end && end < d))
                {
                    risks.TryAdd("koniec_ubezpieczenia_przed_porodem", new Risk(
                        "koniec_ubezpieczenia_przed_porodem",
                        "Ubezpieczenie chorobowe kończy się przed dniem porodu lub przyjęcia dziecka. Od tego zależy prawo do zasiłku macierzyńskiego; wyjątki są w art. 30 ustawy.",
                        "ustawa art. 29 ust. 1, art. 30"));
                }

                var active = titles.Where(t => Insurance.ActiveOn(t, d)).ToList();
                if (active.Any(t => !Insurance.IsSickness(t) && t.RodzajTytulu is TitleKind.Zleceniobiorca or TitleKind.Dzialalnosc))
                {
                    risks.TryAdd("brak_dobrowolnego_chorobowego", new Risk(
                        "brak_dobrowolnego_chorobowego",
                        "Ten tytuł nie obejmuje ubezpieczenia chorobowego (np. zlecenie bez dobrowolnego chorobowego albo ulga na start) — bez niego zasiłek macierzyński ani chorobowy nie przysługuje.",
                        "ustawa art. 29 ust. 1; instrukcja 8.2"));
                }
            }

            if (personFacts?.SkladkiZadluzenie?.Value is { } debt)
            {
                try
                {
                    var threshold = parameters.GetDecimal(EligibilityParameterKeys.ProgZadluzenia, debt.NaDzien);
                    if (debt.Kwota > threshold)
                    {
                        risks.TryAdd("zadluzenie_skladkowe", new Risk(
                            "zadluzenie_skladkowe",
                            $"Zadłużenie z tytułu składek ({Text.Money(debt.Kwota)}) przekracza próg {Text.Money(threshold)} — dla prowadzących działalność i osób współpracujących blokuje to zasiłek do czasu spłaty, a po 6 miesiącach prawo się przedawnia.",
                            "ustawa art. 2a"));
                    }
                }
                catch (ParameterNotFoundException)
                {
                }
            }

            if (personFacts?.EzlaZwolnienia?.Value is { Count: > 0 })
            {
                risks.TryAdd("praca_w_czasie_zwolnienia", new Risk(
                    "praca_w_czasie_zwolnienia",
                    "Praca zarobkowa w czasie zwolnienia lub działania niezgodne z jego celem mogą skutkować utratą prawa do zasiłku za cały okres zwolnienia. To informacja — ocenia to ZUS w konkretnej sprawie.",
                    "ustawa art. 17 ust. 1"));
            }
        }

        return [.. risks.Values];
    }
}
