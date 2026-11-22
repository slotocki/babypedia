using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

internal sealed record SicknessUsage(DateOnly? FirstDay, DateOnly? BlockStart, DateOnly? BlockEnd, int UsedDays, bool HasLeaves);

internal static class SicknessCalc
{
    public static IReadOnlyList<SickLeave> PregnancyLeaves(EvaluationContext context)
    {
        var leaves = context.PersonFacts?.EzlaZwolnienia?.Value;
        if (leaves is null)
        {
            return [];
        }

        var birth = context.EventDate();
        return leaves
            .Where(l => string.Equals(l.Kod, "B", StringComparison.OrdinalIgnoreCase) || (birth is { } d && l.Od < d))
            .OrderBy(l => l.Od)
            .ToList();
    }

    public static SicknessUsage Usage(EvaluationContext context)
    {
        var leaves = PregnancyLeaves(context);
        if (leaves.Count == 0)
        {
            return new SicknessUsage(null, null, null, 0, false);
        }

        var intervals = leaves.Select(l => (S: l.Od, E: l.Do)).ToList();
        var received = context.PersonFacts?.UbezpieczeniePobraneSwiadczenia?.Value;
        if (received is not null)
        {
            intervals.AddRange(received
                .Where(r => r.Rodzaj is ReceivedBenefitKind.WynagrodzenieChorobowe or ReceivedBenefitKind.Chorobowy)
                .Select(r => (S: r.Od, E: r.Do)));
        }

        var blocks = Merge(intervals);
        var withLeave = blocks.Where(b => leaves.Any(l => l.Od >= b.S && l.Od <= b.E)).ToList();
        var current = withLeave.MaxBy(b => b.S);
        var used = context.AsOf < current.S ? 0 : Insurance.DaysBetween(current.S, current.E < context.AsOf ? current.E : context.AsOf) + 1;
        return new SicknessUsage(leaves[0].Od, current.S, current.E, used, true);
    }

    public static List<(DateOnly S, DateOnly E)> Merge(IEnumerable<(DateOnly S, DateOnly E)> intervals)
    {
        var result = new List<(DateOnly S, DateOnly E)>();
        foreach (var i in intervals.OrderBy(x => x.S))
        {
            if (result.Count > 0 && i.S.DayNumber <= result[^1].E.DayNumber + 1)
            {
                result[^1] = (result[^1].S, i.E > result[^1].E ? i.E : result[^1].E);
            }
            else
            {
                result.Add(i);
            }
        }

        return result;
    }

    public static int AgeOn(DateOnly birth, DateOnly day)
    {
        var age = day.Year - birth.Year;
        return birth.AddYears(age) > day ? age - 1 : age;
    }
}

public sealed class WaitingPeriodRule(DateOnly firstDay) : EligibilityRule
{
    public const string RuleId = "okres_wyczekiwania";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 4 ust. 1–2";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Upłynął okres wyczekiwania (30 dni ubezpieczenia obowiązkowego, 90 dni dobrowolnego), z wliczeniem wcześniejszych okresów przy przerwie do 30 dni";
        var person = context.PersonFacts;
        var view = Insurance.View(person, firstDay);
        if (view.State is not (InsuranceState.Insured or InsuranceState.Concurrent) || Insurance.Titles(person) is not { } titles)
        {
            return Met("Okres wyczekiwania nie jest oceniany bez rozpoznanego tytułu ubezpieczenia.", condition);
        }

        // Okresy ubezpieczenia chorobowego liczą się łącznie (art. 4 ust. 2), a wymagana liczba dni zależy od tytułu:
        // przy zbiegu każdy tytuł ma swój próg (30 dni obowiązkowe, 90 dni dobrowolne).
        var requirements = view.SicknessTitles
            .Select(t => (Title: t, Days: (int)context.Param(TitleFeatures.Of(t).WaitingPeriodKey!, firstDay)))
            .ToList();
        var title = requirements.MinBy(r => r.Days).Title;
        var required = requirements.Min(r => r.Days);
        var maxGap = (int)context.Param(EligibilityParameterKeys.WyczekiwanieMaxPrzerwaDni, firstDay);

        var components = SicknessCalc.Merge(titles
            .Where(t => Insurance.IsSickness(t) && t.Od <= firstDay)
            .Select(t => (S: t.Od, E: t.Do ?? DateOnly.MaxValue)));
        var currentIndex = components.FindIndex(c => c.S <= firstDay && c.E >= firstDay);
        var have = Insurance.DaysBetween(components[currentIndex].S, firstDay);
        var bridged = new List<string>();
        var index = currentIndex;
        var unclassifiedPotential = 0;
        while (index > 0)
        {
            var previous = components[index - 1];
            var gap = Insurance.DaysBetween(previous.E, components[index].S) - 1;
            var previousDays = Insurance.DaysBetween(previous.S, previous.E) + 1;
            if (gap <= maxGap)
            {
                have += previousDays;
                bridged.Add($"{Text.Date(previous.S)}–{Text.Date(previous.E)}");
                index--;
                continue;
            }

            var overlapsBreak = person?.UbezpieczeniePrzerwy?.Value?.Any(b => b.Od <= components[index].S.AddDays(-1) && b.Do >= previous.E.AddDays(1)) == true;
            if (overlapsBreak)
            {
                unclassifiedPotential = previousDays;
            }

            break;
        }

        var kindWord = title.Chorobowe == SicknessInsurance.Obowiazkowe ? "obowiązkowego" : "dobrowolnego";
        if (view.State == InsuranceState.Concurrent && have >= required)
        {
            var list = string.Join(", ", requirements.Select(r => $"{Insurance.KindName(r.Title.RodzajTytulu)} — wymagane {r.Days}"));
            var notYet = requirements.Where(r => have < r.Days).ToList();
            var text = $"Ubezpieczenie chorobowe trwa {have} dni przed pierwszym dniem niezdolności ({list}).";
            return notYet.Count == 0
                ? Met(text, condition)
                : Met(text, condition,
                    $"Z tytułu {string.Join(", ", notYet.Select(r => $"„{Insurance.KindName(r.Title.RodzajTytulu)}” (chorobowe {Insurance.SicknessName(r.Title.Chorobowe)}, wymagane {r.Days} dni)"))} okres wyczekiwania jeszcze nie upłynął — z tego tytułu zasiłek chorobowy nie przysługuje (art. 4 ust. 1); przysługuje z pozostałych.");
        }

        if (have >= required)
        {
            return Met($"Ubezpieczenie chorobowe ({kindWord}) trwa {have} dni przed pierwszym dniem niezdolności (wymagane {required}).", condition);
        }

        if (unclassifiedPotential > 0 && have + unclassifiedPotential >= required)
        {
            return Undecided(
                $"Wcześniejszy okres ubezpieczenia jest oddzielony przerwą dłuższą niż {maxGap} dni; wlicza się go tylko, gdy przerwa wynikała z urlopu wychowawczego, bezpłatnego albo czynnej służby wojskowej (art. 4 ust. 2) — tego nie rozpoznaję, nie rozstrzygam.",
                condition,
                PodstawaPrawna);
        }

        var earliest = firstDay.AddDays(required - have);
        return NotMet(
            $"Ubezpieczenie chorobowe ({kindWord}) trwa {have} dni przed pierwszym dniem niezdolności, a wymagane jest {required}.",
            condition,
            $"Okres wyczekiwania nie upłynął: ubezpieczenie chorobowe trwa {have} dni, a do zasiłku chorobowego potrzeba {required} dni nieprzerwanego ubezpieczenia.",
            $"Zasiłek chorobowy przysługiwałby przy niezdolności powstałej od dnia {Text.Date(earliest)} (po {required} dniach ubezpieczenia) albo przy spełnieniu wyjątku z art. 4 ust. 3 (np. 10 lat wcześniejszego obowiązkowego ubezpieczenia).",
            PodstawaPrawna);
    }
}

public sealed class WaitingPeriodExceptionRule(DateOnly firstDay) : EligibilityRule
{
    public const string RuleId = "wyjatek_od_okresu_wyczekiwania";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 4 ust. 3";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Wyjątek od okresu wyczekiwania: co najmniej 10 lat wcześniejszego obowiązkowego ubezpieczenia albo absolwent objęty ubezpieczeniem w ciągu 90 dni od ukończenia szkoły";
        var person = context.PersonFacts!;
        var view = Insurance.View(person, firstDay);
        var missing = new List<MissingFact>();

        var yearsNeeded = context.Param(EligibilityParameterKeys.WyczekiwanieWyjatekLatObowiazkowego, firstDay);
        var years = person.UbezpieczenieLatWczesniejszegoObowiazkowego?.Value;
        if (years is { } y)
        {
            if (y >= yearsNeeded && view.SicknessTitles.Any(t => t.Chorobowe == SicknessInsurance.Obowiazkowe))
            {
                return Met($"Wcześniejszy okres obowiązkowego ubezpieczenia chorobowego wynosi {Text.Number(y)} lat (wymagane co najmniej {Text.Number(yearsNeeded)}) — zasiłek od pierwszego dnia ubezpieczenia.", condition);
            }
        }
        else
        {
            missing.Add(new MissingFact(
                FactKeys.UbezpieczenieLatWczesniejszegoObowiazkowego,
                "Ile lat trwał wcześniejszy okres obowiązkowego ubezpieczenia chorobowego?",
                MissingFactType.Number,
                context.Person));
        }

        var graduation = person.OsobaDataUkonczeniaSzkoly?.Value;
        if (graduation is { } finished && Insurance.Titles(person) is { } titles)
        {
            var maxDays = (int)context.Param(EligibilityParameterKeys.WyczekiwanieAbsolwentMaxDni, firstDay);
            var firstInsurance = titles.Where(Insurance.IsSickness).Select(t => t.Od).DefaultIfEmpty(firstDay).Min();
            if (Insurance.DaysBetween(finished, firstInsurance) <= maxDays)
            {
                return Met($"Ubezpieczenie chorobowe od {Text.Date(firstInsurance)}, w ciągu {maxDays} dni od ukończenia szkoły ({Text.Date(finished)}) — zasiłek od pierwszego dnia ubezpieczenia.", condition);
            }
        }
        else if (graduation is null)
        {
            missing.Add(new MissingFact(
                FactKeys.OsobaDataUkonczeniaSzkoly,
                "Kiedy ta osoba ukończyła szkołę lub uczelnię (jeśli ubezpieczenie zaczęło się krótko potem)?",
                MissingFactType.Date,
                context.Person));
        }

        return new RuleOutcome(new RuleResult(
            RuleVerdict.NotMet,
            "Z danych nie wynika wyjątek z art. 4 ust. 3 (10 lat obowiązkowego ubezpieczenia, absolwent). Wypadek w drodze do pracy nie jest uwzględniany.",
            condition,
            missing.Count == 0 ? null : missing));
    }
}

public sealed class SicknessPeriodRule : EligibilityRule
{
    public const string RuleId = "okres_zasilkowy_ciaza";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 8 ust. 1, art. 9, art. 11 ust. 2 pkt 1";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Nie wyczerpano okresu zasiłkowego (270 dni w okresie ciąży)";
        var usage = SicknessCalc.Usage(context);
        var date = usage.BlockStart ?? context.AsOf;
        var max = (int)context.Param(EligibilityParameterKeys.ChorobowyCiazaMaxDni, date);

        if (!usage.HasLeaves)
        {
            return Met($"Brak zwolnień w ciąży — do wykorzystania pełne {max} dni.", condition);
        }

        var remaining = max - usage.UsedDays;
        if (remaining > 0)
        {
            return Met($"Wykorzystano {usage.UsedDays} z {max} dni okresu zasiłkowego w ciąży (zostało {remaining}).", condition);
        }

        return NotMet(
            $"Wykorzystano {usage.UsedDays} dni, a okres zasiłkowy w ciąży wynosi {max} dni.",
            condition,
            $"Okres zasiłkowy w ciąży ({max} dni) został wyczerpany.",
            "Po wyczerpaniu zasiłku chorobowego można ubiegać się o świadczenie rehabilitacyjne (o prawie orzeka lekarz orzecznik ZUS).",
            PodstawaPrawna);
    }
}

public sealed class SicknessWagesRule(DateOnly firstDay) : EligibilityRule
{
    public override string Id => "wynagrodzenie_chorobowe_pracownika";
    public override string PodstawaPrawna => "Załącznik ZUS (wynagrodzenie chorobowe); KP art. 92";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Pracownik: najpierw wynagrodzenie chorobowe od pracodawcy (33 dni, od 50. roku życia 14 dni w roku kalendarzowym), potem zasiłek";
        var person = context.PersonFacts;
        var view = Insurance.View(person, firstDay);
        if (view.State == InsuranceState.Concurrent)
        {
            return Met(view.SicknessTitles.Any(t => t.RodzajTytulu == TitleKind.Pracownik)
                    ? "Zbieg tytułów: z tytułu pracownika najpierw wynagrodzenie chorobowe od pracodawcy (art. 92 KP), potem zasiłek; z pozostałych tytułów zasiłek od pierwszego dnia niezdolności (po okresie wyczekiwania)."
                    : "Zbieg tytułów bez zatrudnienia pracowniczego — wynagrodzenie chorobowe od pracodawcy nie przysługuje, zasiłek wypłaca się od pierwszego dnia po okresie wyczekiwania.",
                condition);
        }

        if (view.Title is not { RodzajTytulu: TitleKind.Pracownik })
        {
            return Met("Tytuł inny niż pracownik — wynagrodzenie chorobowe od pracodawcy nie przysługuje, zasiłek wypłaca się od pierwszego dnia po okresie wyczekiwania.", condition);
        }

        var ageLimit = (int)context.Param(EligibilityParameterKeys.WynagrodzenieChorobowoWiekLat, firstDay);
        var birth = person?.OsobaDataUrodzenia?.Value;
        if (birth is null)
        {
            var limitYoung = (int)context.Param(EligibilityParameterKeys.WynagrodzenieChorobowoDoPieciudziesieciu, firstDay);
            var limitOld = (int)context.Param(EligibilityParameterKeys.WynagrodzenieChorobowoOdPieciudziesieciu, firstDay);
            return new RuleOutcome(new RuleResult(
                RuleVerdict.Met,
                $"Pracodawca wypłaca wynagrodzenie chorobowe za pierwsze {limitYoung} dni w roku kalendarzowym ({limitOld} dni od {ageLimit}. roku życia); potem zasiłek. Podaj datę urodzenia, żeby ustalić limit.",
                condition,
                [Ask.BirthDate(context.Person)]));
        }

        var age = SicknessCalc.AgeOn(birth.Value, firstDay);
        var key = age >= ageLimit
            ? EligibilityParameterKeys.WynagrodzenieChorobowoOdPieciudziesieciu
            : EligibilityParameterKeys.WynagrodzenieChorobowoDoPieciudziesieciu;
        var limit = (int)context.Param(key, firstDay);

        var usedBefore = 0;
        if (person?.UbezpieczeniePobraneSwiadczenia?.Value is { } received)
        {
            var yearStart = new DateOnly(firstDay.Year, 1, 1);
            foreach (var r in received.Where(r => r.Rodzaj == ReceivedBenefitKind.WynagrodzenieChorobowe && r.Do < firstDay))
            {
                var from = r.Od < yearStart ? yearStart : r.Od;
                if (r.Do >= from)
                {
                    usedBefore += Insurance.DaysBetween(from, r.Do) + 1;
                }
            }
        }

        var remaining = Math.Max(0, limit - usedBefore);
        return Met(
            $"Pracownik w wieku {age} lat (limit {limit} dni w roku kalendarzowym): pierwsze {remaining} dni niezdolności w tym roku to wynagrodzenie chorobowe od pracodawcy, dopiero potem zasiłek.",
            condition);
    }
}

public sealed class SicknessAfterInsuranceEndRule(DateOnly firstDay) : EligibilityRule
{
    public override string Id => "zasilek_chorobowy_po_ustaniu_ubezpieczenia";
    public override string PodstawaPrawna => "ustawa art. 6–7, art. 8, art. 13";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Niezdolność powstała po ustaniu ubezpieczenia: trwa co najmniej 30 dni i powstała w ciągu 14 dni od ustania (art. 7), brak wyłączeń z art. 13";
        var ended = InsuranceEndExceptionRule.LastEndedTitle(context.PersonFacts, firstDay.AddDays(1));
        if (ended is null)
        {
            return NotMet(
                "Brak ubezpieczenia chorobowego w pierwszym dniu niezdolności i brak wcześniejszego tytułu, od którego liczyłoby się prawo po ustaniu.",
                condition,
                "W pierwszym dniu niezdolności do pracy nie było ubezpieczenia chorobowego, a z danych nie wynika wcześniejszy tytuł, który dawałby prawo po jego ustaniu.",
                "Prawo przysługiwałoby, gdyby niezdolność powstała w czasie ubezpieczenia chorobowego.",
                PodstawaPrawna);
        }

        var end = ended.Do!.Value;
        var startDays = (int)context.Param(EligibilityParameterKeys.ChorobowyPoUstaniuStartDni, firstDay);
        var minDays = (int)context.Param(EligibilityParameterKeys.ChorobowyPoUstaniuMinDni, firstDay);
        var infectiousMonths = (int)context.Param(EligibilityParameterKeys.ChorobowyPoUstaniuZakaznaMiesiace, firstDay);
        var delta = Insurance.DaysBetween(end, firstDay);

        if (firstDay > end.AddMonths(infectiousMonths))
        {
            return NotMet(
                $"Niezdolność powstała {delta} dni po ustaniu ubezpieczenia ({Text.Date(end)}), czyli po upływie {infectiousMonths} miesięcy.",
                condition,
                $"Niezdolność do pracy powstała później niż {infectiousMonths} miesiące po ustaniu ubezpieczenia chorobowego, a prawo po ustaniu tytułu wymaga początku niezdolności najpóźniej w tym terminie.",
                "Prawo przysługiwałoby, gdyby niezdolność powstała w czasie ubezpieczenia albo w terminie z art. 7.",
                "ustawa art. 7");
        }

        if (delta > startDays)
        {
            return Undecided(
                $"Niezdolność powstała {delta} dni po ustaniu ubezpieczenia — wydłużony termin z art. 7 pkt 2 dotyczy tylko chorób o długim okresie wylęgania lub późno ujawniających się objawach, czego nie rozpoznaję — nie rozstrzygam, skontaktuj się z ZUS.",
                condition,
                "ustawa art. 7");
        }

        var usage = SicknessCalc.Usage(context);
        var length = usage.BlockStart is { } s && usage.BlockEnd is { } e ? Insurance.DaysBetween(s, e) + 1 : 0;
        if (length < minDays && usage.BlockEnd is { } blockEnd && blockEnd < context.AsOf)
        {
            return NotMet(
                $"Niezdolność trwała {length} dni, a art. 7 wymaga co najmniej {minDays} dni bez przerwy.",
                condition,
                $"Niezdolność do pracy po ustaniu ubezpieczenia trwała krócej niż {minDays} dni bez przerwy.",
                null,
                "ustawa art. 7");
        }

        return Undecided(
            $"Niezdolność powstała {delta} dni po ustaniu ubezpieczenia ({Text.Date(end)}); art. 7 może być spełniony, ale wyłączeń z art. 13 (emerytura, renta, zasiłek dla bezrobotnych, podjęta działalność, ubezpieczenie rolnicze) nie da się ocenić z posiadanych danych — nie rozstrzygam, skontaktuj się z ZUS.",
            condition,
            "ustawa art. 7, art. 13");
    }
}

public sealed class RehabilitationRule : EligibilityRule
{
    public override string Id => "swiadczenie_rehabilitacyjne_po_wyczerpaniu_zasilku";
    public override string PodstawaPrawna => "ustawa art. 18 ust. 1–3, art. 19 ust. 1";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Po wyczerpaniu zasiłku chorobowego osoba jest nadal niezdolna do pracy, a leczenie rokuje odzyskanie zdolności (orzeka lekarz orzecznik ZUS)";
        var date = SicknessCalc.Usage(context).BlockStart ?? context.AsOf;
        var months = (int)context.Param(EligibilityParameterKeys.RehabilitacyjneMaxMiesiecy, date);
        return Undecided(
            $"Okres zasiłkowy został wyczerpany. Prawo do świadczenia rehabilitacyjnego (do {months} miesięcy, w okresie ciąży 100% podstawy) zależy od orzeczenia lekarza orzecznika ZUS — nie rozstrzygam, złóż wniosek do ZUS.",
            condition,
            PodstawaPrawna);
    }
}
