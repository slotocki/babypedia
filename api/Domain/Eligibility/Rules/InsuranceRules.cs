using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

public sealed class InsuranceOnDateRule(DateOnly? onDate = null, string? subject = null) : EligibilityRule
{
    public const string RuleId = "ubezpieczenie_w_dniu_zdarzenia";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 29 ust. 1–2";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        var date = onDate ?? context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia, nie da się sprawdzić ubezpieczenia.", "Ubezpieczenie chorobowe w dniu zdarzenia", Ask.EventDate());
        }

        var when = subject ?? $"w dniu {Text.EventWord(context.Event())}";
        var condition = $"Ubezpieczenie chorobowe {when} ({Text.Date(date.Value)})";
        var view = Insurance.View(context.PersonFacts, date.Value);

        switch (view.State)
        {
            case InsuranceState.Unknown:
                return Missing("Brak danych o tytułach ubezpieczenia.", condition, Ask.Titles(context.Person));
            case InsuranceState.Concurrent:
                return Undecided(
                    "W tym dniu obowiązuje więcej niż jeden tytuł ubezpieczenia (zbieg tytułów) albo działalność obok zatrudnienia. Przepisy z wyciągu tego nie rozstrzygają — nie rozstrzygam, skontaktuj się z ZUS.",
                    condition,
                    PodstawaPrawna);
            case InsuranceState.DeclaredInsured:
                return Met(
                    "Z deklaracji wynika, że osoba podlega ubezpieczeniu chorobowemu; rodzaj tytułu nie jest znany.",
                    condition,
                    "Ocena opiera się na deklaracji (brak danych o tytule): nie sprawdzono kontroli zadłużenia (art. 2a) ani płatnika zasiłku.");
            case InsuranceState.DeclaredNotInsured:
                return NotMet(
                    $"Z deklaracji wynika, że osoba nie podlega ubezpieczeniu chorobowemu {when}.",
                    condition,
                    $"Brak ubezpieczenia chorobowego {when} ({Text.Date(date.Value)}).",
                    "Prawo przysługiwałoby, gdyby w tym dniu istniał tytuł ubezpieczenia chorobowego.",
                    PodstawaPrawna);
            case InsuranceState.Insured:
                var title = view.Title!;
                return Met(
                    $"Tytuł: {Insurance.KindName(title.RodzajTytulu)}, ubezpieczenie chorobowe {Insurance.SicknessName(title.Chorobowe)} — obejmuje ten dzień.",
                    condition);
            default:
                var extra = view.ActiveWithoutSickness.Count > 0
                    ? $" Tytuł „{Insurance.KindName(view.ActiveWithoutSickness[0].RodzajTytulu)}” w tym dniu nie obejmuje ubezpieczenia chorobowego."
                    : "";
                return NotMet(
                    $"Żaden tytuł z ubezpieczeniem chorobowym nie obejmuje tego dnia.{extra}",
                    condition,
                    $"Brak ubezpieczenia chorobowego {when} ({Text.Date(date.Value)}).",
                    "Prawo przysługiwałoby, gdyby w tym dniu istniał tytuł ubezpieczenia chorobowego (np. zatrudnienie albo dobrowolne ubezpieczenie chorobowe) albo zachodził wyjątek z art. 30 ustawy.",
                    PodstawaPrawna);
        }
    }
}

public sealed class TitleFeaturesRule(DateOnly? onDate = null) : EligibilityRule
{
    public override string Id => "cechy_tytulu_ubezpieczenia";
    public override string PodstawaPrawna => "instrukcja 8.2; ustawa art. 3 pkt 1, art. 4, art. 2a; KP art. 177";

    public override RuleOutcome Assess(EvaluationContext context)
    {
        var date = onDate ?? context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia.", "Cechy tytułu ubezpieczenia", Ask.EventDate());
        }

        var view = Insurance.View(context.PersonFacts, date.Value);
        if (view.Title is null)
        {
            return Met("Brak jednego rozpoznanego tytułu w tym dniu — cechy tytułu nie są ustalane.", "Cechy tytułu ubezpieczenia");
        }

        var f = TitleFeatures.Of(view.Title);
        var text = $"Tytuł „{Insurance.KindName(f.Kind)}”, chorobowe {Insurance.SicknessName(f.Sickness)}. " +
                   $"Wynagrodzenie chorobowe od pracodawcy przed zasiłkiem: {(f.WagesFromEmployerBeforeBenefit ? "tak" : "nie")}. " +
                   $"Wniosek o urlop do pracodawcy: {(f.LeaveApplicationToEmployer ? "tak" : "nie")}. " +
                   $"Kontrola zadłużenia składkowego (art. 2a): {(f.DebtControl ? "tak" : "nie")}. " +
                   $"Ochrona stosunku pracy (KP art. 177): {(f.EmploymentProtection ? "tak" : "nie")}.";
        return Met(text, "Cechy tytułu ubezpieczenia (okres zasiłku niezależnie od wniosku o urlop)");
    }
}

public sealed class InsuranceEndExceptionRule : EligibilityRule
{
    public const string RuleId = "wyjatek_ustanie_ubezpieczenia_w_ciazy";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 30 ust. 1 i 4; KP art. 177 § 3";

    public static InsuranceTitle? LastEndedTitle(PersonFacts? person, DateOnly date) =>
        Insurance.Titles(person)?
            .Where(t => Insurance.IsSickness(t) && t.Do is { } end && end < date)
            .OrderByDescending(t => t.Do)
            .FirstOrDefault();

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Ubezpieczenie ustało w okresie ciąży z przyczyny objętej wyjątkiem z art. 30";
        var date = context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia.", condition, Ask.EventDate());
        }

        var person = context.PersonFacts;
        if (Insurance.Titles(person) is null)
        {
            return Missing("Brak historii tytułów ubezpieczenia, nie da się ustalić przyczyny ustania.", condition, Ask.Titles(context.Person));
        }

        var ended = LastEndedTitle(person, date.Value);
        if (ended is null)
        {
            return NotMet(
                "Żaden tytuł z ubezpieczeniem chorobowym nie ustał przed dniem zdarzenia — wyjątki z art. 30 nie mają zastosowania.",
                condition,
                "Brak ubezpieczenia chorobowego w dniu zdarzenia i brak ustania ubezpieczenia, które dawałoby wyjątek z art. 30.",
                "Prawo przysługiwałoby, gdyby w dniu zdarzenia istniał tytuł ubezpieczenia chorobowego.",
                PodstawaPrawna);
        }

        var reason = person?.UbezpieczenieUstaniePrzyczyna?.Value;
        if (reason is null)
        {
            return Missing("Tytuł ustał przed dniem zdarzenia; potrzebna przyczyna ustania, bo od niej zależą wyjątki z art. 30.", condition, Ask.EndReason(context.Person));
        }

        const string pregnancyNote = "Wyjątek dotyczy ubezpieczenia, które ustało w okresie ciąży.";
        switch (reason)
        {
            case InsuranceEndReason.LikwidacjaPracodawcy:
            case InsuranceEndReason.UpadloscPracodawcy:
            case InsuranceEndReason.SmiercPracodawcy:
                return ended.RodzajTytulu == TitleKind.Pracownik
                    ? Met("Ubezpieczenie ustało wskutek śmierci, upadłości lub likwidacji pracodawcy — zasiłek macierzyński przysługuje mimo ustania ubezpieczenia.", condition, pregnancyNote)
                    : Undecided("Przepis mówi o śmierci, upadłości lub likwidacji pracodawcy; ten tytuł nie jest zatrudnieniem pracowniczym — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna);
            case InsuranceEndReason.NaruszeniePrawaOrzeczonePrzezSad:
                return Met("Ubezpieczenie ustało z naruszeniem prawa stwierdzonym orzeczeniem sądu — zasiłek macierzyński przysługuje po porodzie. Przyjęto, że orzeczenie jest prawomocne (tak wymaga art. 30 ust. 1 pkt 2).", condition, pregnancyNote);
            case InsuranceEndReason.KoniecUmowyNaCzasOkreslony:
                return AssessFixedTerm(context, date.Value, ended, condition);
            case InsuranceEndReason.WypowiedzeniePrzezPracownika:
                return NotMet(
                    "Własne wypowiedzenie nie jest wyjątkiem z art. 30 ust. 1.",
                    condition,
                    "Ubezpieczenie ustało wskutek własnego wypowiedzenia, które nie daje wyjątku z art. 30, a w dniu zdarzenia nie było ubezpieczenia chorobowego.",
                    "Prawo przysługiwałoby, gdyby w dniu zdarzenia istniał tytuł ubezpieczenia chorobowego (np. nowe zatrudnienie albo dobrowolne ubezpieczenie chorobowe).",
                    PodstawaPrawna);
            case InsuranceEndReason.WypowiedzeniePrzezPracodawce:
                return NotMet(
                    "Wypowiedzenie przez pracodawcę daje wyjątek tylko, gdy prawomocne orzeczenie sądu stwierdza naruszenie prawa (art. 30 ust. 1 pkt 2).",
                    condition,
                    "Ubezpieczenie ustało wskutek wypowiedzenia przez pracodawcę bez prawomocnego orzeczenia sądu o naruszeniu prawa, a w dniu zdarzenia nie było ubezpieczenia chorobowego.",
                    "Prawo przysługiwałoby, gdyby sąd prawomocnie stwierdził, że ustanie ubezpieczenia nastąpiło z naruszeniem prawa, albo gdyby w dniu zdarzenia istniał tytuł ubezpieczenia chorobowego.",
                    PodstawaPrawna);
            default:
                return NotMet(
                    "Przyczyna ustania ubezpieczenia nie jest objęta wyjątkami z art. 30 ust. 1 i 4.",
                    condition,
                    "Ubezpieczenie ustało z przyczyny, która nie daje wyjątku z art. 30, a w dniu zdarzenia nie było ubezpieczenia chorobowego.",
                    "Prawo przysługiwałoby, gdyby w dniu zdarzenia istniał tytuł ubezpieczenia chorobowego.",
                    PodstawaPrawna);
        }
    }

    private RuleOutcome AssessFixedTerm(EvaluationContext context, DateOnly birth, InsuranceTitle ended, string condition)
    {
        var person = context.PersonFacts!;
        if (person.UmowaNaCzasOkreslony?.Value is not { } fixedTerm)
        {
            return Missing("Potrzebna informacja, czy umowa była na czas określony.", condition, Ask.FixedTerm(context.Person));
        }

        if (!fixedTerm)
        {
            return NotMet(
                "Umowa nie była zawarta na czas określony — art. 177 § 3 KP nie ma zastosowania.",
                condition,
                "Ubezpieczenie ustało z przyczyny, która nie daje wyjątku z art. 30, a w dniu zdarzenia nie było ubezpieczenia chorobowego.",
                "Prawo przysługiwałoby, gdyby w dniu zdarzenia istniał tytuł ubezpieczenia chorobowego.",
                "KP art. 177 § 3; ustawa art. 30 ust. 4");
        }

        if (ended.RodzajTytulu != TitleKind.Pracownik)
        {
            return Undecided("Art. 177 § 3 KP i art. 30 ust. 4 dotyczą pracownicy zatrudnionej na umowie o pracę; ten tytuł nią nie jest — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna);
        }

        var end = person.UmowaDataKonca?.Value ?? ended.Do!.Value;
        int weeks;
        string weeksSource;
        if (context.Facts.Sprawa.DzieckoTydzienCiazyPrzyPorodzie?.Value is { } known)
        {
            weeks = known;
            weeksSource = "tydzień ciąży przy porodzie z danych";
        }
        else if (context.TryParam(EligibilityParameterKeys.CiazaTrwanieTyg, birth) is { } typical)
        {
            weeks = (int)typical;
            weeksSource = "parametr ciaza_trwanie_tyg (brak tygodnia ciąży w danych)";
        }
        else
        {
            return Missing("Do oceny warunku „po upływie trzeciego miesiąca ciąży” potrzebny jest tydzień ciąży przy porodzie.", condition, Ask.PregnancyWeek());
        }

        var months = (int)context.Param(EligibilityParameterKeys.CiazaUmowaPrzedluzenieMiesiace, birth);
        var pregnancyStart = birth.AddDays(-7 * weeks);
        var boundary = pregnancyStart.AddMonths(months);

        if (end > boundary)
        {
            return Met(
                $"Umowa na czas określony kończyła się {Text.Date(end)}, czyli po upływie trzeciego miesiąca ciąży (granica {Text.Date(boundary)}; {weeksSource}), więc ulegała przedłużeniu do dnia porodu (KP art. 177 § 3). Zasiłek macierzyński przysługuje po ustaniu ubezpieczenia (art. 30 ust. 4).",
                condition,
                "Przedłużenie nie następuje, gdy umowa została zawarta w celu zastępstwa nieobecnego pracownika (KP art. 177 § 3¹) — tego faktu nie znamy.");
        }

        return NotMet(
            $"Umowa na czas określony kończyła się {Text.Date(end)}, czyli nie później niż z upływem trzeciego miesiąca ciąży (granica {Text.Date(boundary)}; {weeksSource}), więc nie została przedłużona do dnia porodu.",
            condition,
            "Umowa na czas określony zakończyła się przed upływem trzeciego miesiąca ciąży, więc nie przedłużyła się do dnia porodu (KP art. 177 § 3), a w dniu zdarzenia nie było ubezpieczenia chorobowego.",
            "Prawo przysługiwałoby, gdyby umowa kończyła się po upływie trzeciego miesiąca ciąży (wtedy przedłuża się do dnia porodu) albo gdyby w dniu zdarzenia istniał inny tytuł ubezpieczenia chorobowego.",
            "KP art. 177 § 3; ustawa art. 30 ust. 4");
    }
}

public sealed class BenefitUntilBirthRule : EligibilityRule
{
    public const string RuleId = "zasilek_do_dnia_porodu_pracownica";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 30 ust. 3";

    public static bool IsEligibleReason(InsuranceEndReason? reason) =>
        reason is InsuranceEndReason.LikwidacjaPracodawcy or InsuranceEndReason.UpadloscPracodawcy or InsuranceEndReason.SmiercPracodawcy;

    public override RuleOutcome Assess(EvaluationContext context)
    {
        const string condition = "Pracownica; umowa wygasła z powodu śmierci pracodawcy albo rozwiązana w ciąży z powodu upadłości lub likwidacji pracodawcy; nie zapewniono innego zatrudnienia";
        var date = context.EventDate();
        if (date is null)
        {
            return Missing("Brak daty zdarzenia.", condition, Ask.EventDate());
        }

        var ended = InsuranceEndExceptionRule.LastEndedTitle(context.PersonFacts, date.Value);
        if (ended is null || !IsEligibleReason(context.PersonFacts?.UbezpieczenieUstaniePrzyczyna?.Value))
        {
            return NotMet("Warunki art. 30 ust. 3 nie są spełnione.", condition, "Warunki zasiłku w wysokości zasiłku macierzyńskiego do dnia porodu nie są spełnione.", null, PodstawaPrawna);
        }

        if (ended.RodzajTytulu != TitleKind.Pracownik)
        {
            return Undecided("Art. 30 ust. 3 dotyczy pracownicy; ten tytuł nią nie jest — nie rozstrzygam, skontaktuj się z ZUS.", condition, PodstawaPrawna);
        }

        var view = Insurance.View(context.PersonFacts, date.Value);
        if (view.ActiveWithoutSickness.Any(t => t.RodzajTytulu == TitleKind.Pracownik))
        {
            return Undecided("W dniu zdarzenia trwa inne zatrudnienie pracownicze — nie da się ocenić warunku „nie zapewniono innego zatrudnienia” — nie rozstrzygam.", condition, PodstawaPrawna);
        }

        return Met(
            "Zatrudnienie ustało z powodu śmierci, upadłości lub likwidacji pracodawcy, a w danych nie ma innego zatrudnienia — do dnia porodu przysługuje zasiłek w wysokości zasiłku macierzyńskiego.",
            condition,
            "Warunek „nie zapewniono innego zatrudnienia” oceniono po braku innego tytułu w danych; wyjątek dotyczy umowy rozwiązanej w okresie ciąży.");
    }
}
