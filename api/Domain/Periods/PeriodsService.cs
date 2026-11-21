using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed record PeriodsAssessment(
    IReadOnlyList<BenefitResult> Benefits,
    IReadOnlyList<MissingFact> MissingFacts,
    IReadOnlyList<ValidationIssue> ValidationErrors)
{
    public AssessmentResult ToAssessmentResult(string rulesetVersion) => new()
    {
        RulesetVersion = rulesetVersion,
        Benefits = Benefits,
        MissingFacts = MissingFacts,
        ValidationErrors = ValidationErrors,
    };
}

public interface IPeriodsService
{
    PeriodsAssessment Assess(FactSet facts, DateOnly asOf);

    PeriodsAssessment Assess(FactSet facts, DateOnly asOf, PersonRef person);
}

public sealed class PeriodsService(IParameterProvider parameters) : IPeriodsService
{
    public PeriodsAssessment Assess(FactSet facts, DateOnly asOf)
    {
        var persons = new List<PersonRef> { PersonRef.Wnioskodawca };
        if (facts.DrugiRodzic is not null)
        {
            persons.Add(PersonRef.DrugiRodzic);
        }

        return Assess(facts, asOf, persons);
    }

    public PeriodsAssessment Assess(FactSet facts, DateOnly asOf, PersonRef person) => Assess(facts, asOf, [person]);

    private PeriodsAssessment Assess(FactSet facts, DateOnly asOf, IReadOnlyList<PersonRef> persons)
    {
        var benefits = new List<BenefitResult>();
        var missing = new List<MissingFact>();
        var issues = new List<ValidationIssue>();

        if (facts.Sprawa.ZdarzenieDlaKogo?.Value == CalculationScope.JaIDrugiRodzic && facts.DrugiRodzic is null && persons.Contains(PersonRef.Wnioskodawca))
        {
            missing.Add(PeriodMissing.Role(PersonRef.DrugiRodzic));
        }

        foreach (var person in persons.Where(p => facts.For(p) is not null))
        {
            var inputs = new PeriodsInputs(new EvaluationContext(facts, person, asOf, parameters));
            if (inputs.Kind is null)
            {
                missing.Add(PeriodMissing.EventKind());
                continue;
            }

            foreach (var benefit in BenefitsFor(inputs))
            {
                benefits.Add(benefit.Result);
                missing.AddRange(benefit.Missing);
                issues.AddRange(benefit.Issues);
            }
        }

        return new PeriodsAssessment(
            benefits,
            missing.DistinctBy(m => (m.Fact, m.Person)).ToList(),
            issues.DistinctBy(v => (v.Code, v.Person)).ToList());
    }

    private static IEnumerable<Draft> BenefitsFor(PeriodsInputs i)
    {
        var role = i.Role;
        if (i.IsAdoption)
        {
            if (role != ParentRole.Ojciec)
            {
                yield return PrzyjecieDziecka(i);
            }

            if (role != ParentRole.Matka)
            {
                yield return Ojcowski(i);
            }

            yield return Rodzicielski(i);
            yield break;
        }

        if (role != ParentRole.Ojciec)
        {
            yield return Macierzynski(i);
            yield return Uzupelniajacy(i);
        }

        if (role != ParentRole.Matka)
        {
            yield return Ojcowski(i);
        }

        yield return Rodzicielski(i);
    }

    private static Draft RoleMissing(PeriodsInputs i, BenefitId id)
    {
        var draft = new Draft(id, i.Person);
        draft.Run(Step.Missing<bool>(PeriodRules.RodzicielskiPoczatek, PeriodMissing.Role(i.Person)));
        return draft;
    }

    private static Draft Macierzynski(PeriodsInputs i)
    {
        var draft = new Draft(BenefitId.Macierzynski, i.Person);
        if (i.Role is null)
        {
            return RoleMissing(i, BenefitId.Macierzynski);
        }

        if (i.ChildDeceased)
        {
            if (!draft.Run(i.MacierzynskiPrzedPorodem) || !draft.Run(i.MartweUrodzenieZgon))
            {
                return draft;
            }
        }
        else if (!draft.Run(i.MacierzynskiWymiar) || !draft.Run(i.MacierzynskiPrzedPorodem) || !draft.Run(i.MacierzynskiOkres))
        {
            return draft;
        }

        if (i.Me.PlanRezygnacjaZMacierzynskiegoPoTyg is not null && !draft.Run(i.MacierzynskiRezygnacja))
        {
            return draft;
        }

        draft.Period = i.Me.PlanRezygnacjaZMacierzynskiegoPoTyg is not null
            ? i.MacierzynskiRezygnacja.Value
            : i.MacierzynskiBlok.Value;
        return draft;
    }

    private static Draft Uzupelniajacy(PeriodsInputs i)
    {
        var draft = new Draft(BenefitId.MacierzynskiUzupelniajacy, i.Person);
        if (i.Role is null)
        {
            return RoleMissing(i, BenefitId.MacierzynskiUzupelniajacy);
        }

        if (!draft.Run(i.UzupelniajacyPrzypadek) || !draft.Run(i.UzupelniajacyWymiar) || !draft.Run(i.MacierzynskiBlok)
            || !draft.Run(i.UzupelniajacyOkres))
        {
            return draft;
        }

        draft.Period = i.UzupelniajacyOkres.Value;
        return draft;
    }

    private static Draft Rodzicielski(PeriodsInputs i)
    {
        var draft = new Draft(BenefitId.Rodzicielski, i.Person);
        if (i.Role is null)
        {
            return RoleMissing(i, BenefitId.Rodzicielski);
        }

        if (i.ChildDeceased)
        {
            draft.Run(PeriodRules.PoZgonieDziecka.Calculate(i));
            return draft;
        }

        if (i.IsAdoption && (!draft.Run(i.PrzyjecieSciezka) || !draft.Run(i.PrzyjecieWiek) || !draft.Run(i.PrzyjecieOkres)))
        {
            return draft;
        }

        if (!draft.Run(i.RodzicielskiWymiar) || !draft.Run(i.RodzicielskiCzesci) || !draft.Run(i.RodzicielskiNieprzenoszalne)
            || !draft.Run(i.RodzicielskiTermin) || !draft.Run(i.RodzicielskiPraca) || !draft.Run(i.RodzicielskiPoczatek)
            || !draft.Run(i.RodzicielskiOkres))
        {
            return draft;
        }

        var outcome = i.RodzicielskiOkres.Value!;
        draft.Period = outcome.Period;
        draft.Parts = outcome.Parts;
        draft.Notes.AddRange(outcome.Notes);
        if (i.RodzicielskiTermin.Value is { } deadline)
        {
            draft.Deadlines.Add(new Deadline(
                "wykorzystanie_rodzicielskiego", DeadlineType.Okno, deadline,
                $"Urlop rodzicielski wykorzystaj najpóźniej do {PeriodMath.Format(deadline)}.",
                "prawo do urlopu rodzicielskiego wygasa", i.RodzicielskiTermin.Rule.PodstawaPrawna));
        }

        return draft;
    }

    private static Draft Ojcowski(PeriodsInputs i)
    {
        var draft = new Draft(BenefitId.Ojcowski, i.Person);
        if (i.Role is null)
        {
            return RoleMissing(i, BenefitId.Ojcowski);
        }

        if (i.ChildDeceased)
        {
            draft.Run(PeriodRules.PoZgonieDziecka.Calculate(i));
            return draft;
        }

        if (!draft.Run(i.OjcowskiOkno) || !draft.Run(i.OjcowskiCzesci) || !draft.Run(i.OjcowskiOkres))
        {
            return draft;
        }

        var outcome = i.OjcowskiOkres.Value!;
        draft.Period = outcome.Period;
        draft.Parts = outcome.Parts;
        draft.Notes.AddRange(outcome.Notes);
        var window = i.OjcowskiOkno.Value;
        draft.Deadlines.Add(new Deadline(
            "wykorzystanie_ojcowskiego", DeadlineType.Okno, window,
            $"Urlop ojcowski wykorzystaj najpóźniej do {PeriodMath.Format(window)}.",
            "prawo do urlopu ojcowskiego wygasa", i.OjcowskiOkno.Rule.PodstawaPrawna));
        return draft;
    }

    private static Draft PrzyjecieDziecka(PeriodsInputs i)
    {
        var draft = new Draft(BenefitId.PrzyjecieDziecka, i.Person);
        if (i.Role is null)
        {
            return RoleMissing(i, BenefitId.PrzyjecieDziecka);
        }

        if (!draft.Run(i.PrzyjecieSciezka) || !draft.Run(i.PrzyjecieWiek) || !draft.Run(i.PrzyjecieOkres))
        {
            return draft;
        }

        draft.Period = i.PrzyjecieOkres.Value!.Period;
        return draft;
    }

    private sealed class Draft(BenefitId id, PersonRef person)
    {
        private readonly List<RuleStep> steps = [];

        public Period? Period { get; set; }

        public IReadOnlyList<BenefitPart> Parts { get; set; } = [];

        public List<string> Notes { get; } = [];

        public List<Deadline> Deadlines { get; } = [];

        public bool Run(RuleStep step)
        {
            steps.Add(step);
            return step.CanProceed;
        }

        public IReadOnlyList<ValidationIssue> Issues => steps.SelectMany(s => s.Issues).ToList();

        public IReadOnlyList<MissingFact> Missing => steps
            .Where(s => s.Result.Verdict == RuleVerdict.MissingData && !s.NotApplicable)
            .SelectMany(s => s.Result.Missing ?? [])
            .ToList();

        public BenefitResult Result
        {
            get
            {
                var blocker = steps.FirstOrDefault(s => !s.CanProceed);
                var trace = steps.Select(s => s.Trace).ToList();
                if (blocker is null)
                {
                    return new BenefitResult
                    {
                        Id = id,
                        Person = person,
                        Status = BenefitStatus.Przysluguje,
                        Period = Period,
                        Parts = Parts,
                        Deadlines = Deadlines,
                        Notes = Notes,
                        Trace = trace,
                    };
                }

                var text = blocker.Result.Text;
                var legalBasis = blocker.Rule.PodstawaPrawna;
                if (blocker.NotApplicable)
                {
                    return new BenefitResult { Id = id, Person = person, Status = BenefitStatus.NieDotyczy, Reason = text, LegalBasis = legalBasis, Trace = trace };
                }

                return blocker.Result.Verdict switch
                {
                    RuleVerdict.NotMet => new BenefitResult { Id = id, Person = person, Status = BenefitStatus.NiePrzysluguje, Reason = text, LegalBasis = legalBasis, Trace = trace },
                    RuleVerdict.Undecided => new BenefitResult { Id = id, Person = person, Status = BenefitStatus.NieRozstrzygam, Reason = text, LegalBasis = legalBasis, Trace = trace },
                    _ => new BenefitResult { Id = id, Person = person, Status = BenefitStatus.BrakujeDanych, Trace = trace },
                };
            }
        }
    }
}
