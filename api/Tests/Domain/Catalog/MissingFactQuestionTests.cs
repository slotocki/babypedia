using System.Reflection;
using System.Text.Json;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class MissingFactQuestionTests
{
    private static IEnumerable<string> AllFactKeys() => typeof(FactKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral)
        .Select(f => (string)f.GetRawConstantValue()!);

    private static List<string> Values<TEnum>() where TEnum : struct, Enum =>
        [.. Enum.GetValues<TEnum>().Select(v => JsonSerializer.Serialize(v).Trim('"'))];

    private static MissingFact? Asked(AssessmentResult result, string fact, PersonRef? person = null) =>
        result.MissingFacts.SingleOrDefault(m => m.Fact == fact && m.Person == person);

    [Fact]
    public void Every_fact_key_has_a_polish_question_with_a_type()
    {
        foreach (var key in AllFactKeys())
        {
            Assert.True(FactQuestionBook.All.ContainsKey(key), $"brak pytania dla {key}");
            var question = FactQuestionBook.All[key];
            Assert.True(question.Question.EndsWith('?'), $"{key}: pytanie powinno kończyć się znakiem zapytania");
            Assert.True(question.Question.Length > 12, key);
        }

        Assert.Equal(AllFactKeys().Count(), FactQuestionBook.All.Count);
    }

    [Fact]
    public void Enum_questions_offer_exactly_the_contract_values()
    {
        var expected = new Dictionary<string, List<string>>
        {
            [FactKeys.ZdarzenieRodzaj] = Values<EventKind>(),
            [FactKeys.ZdarzenieDlaKogo] = Values<CalculationScope>(),
            [FactKeys.PrzyjecieSciezka] = Values<AdoptionPath>(),
            [FactKeys.MatkaSytuacjaSzczegolna] = Values<MotherSituationKind>(),
            [FactKeys.OsobaRola] = Values<ParentRole>(),
            [FactKeys.UbezpieczenieUstaniePrzyczyna] = Values<InsuranceEndReason>(),
            [FactKeys.PlanWariantWyplaty] = Values<PaymentVariant>(),
        };

        foreach (var (key, values) in expected)
        {
            var question = FactQuestionBook.All[key];
            Assert.Equal(MissingFactType.Enum, question.Type);
            Assert.Equal(values.Order(), question.Options!.Order());
        }

        Assert.All(FactQuestionBook.All.Values.Where(q => q.Type != MissingFactType.Enum), q => Assert.Null(q.Options));
    }

    [Fact]
    public void Question_types_match_the_fact_value_types()
    {
        var types = FactQuestionBook.All;

        Assert.Equal(MissingFactType.Date, types[FactKeys.ZdarzenieData].Type);
        Assert.Equal(MissingFactType.Integer, types[FactKeys.DzieckoLiczba].Type);
        Assert.Equal(MissingFactType.Boolean, types[FactKeys.DzieckoZaZyciem].Type);
        Assert.Equal(MissingFactType.Number, types[FactKeys.UbezpieczenieLatWczesniejszegoObowiazkowego].Type);
        Assert.Equal(MissingFactType.Money, types[FactKeys.SkladkiZadluzenie].Type);
        Assert.Equal(MissingFactType.Money, types[FactKeys.PodstawaUstalonaPrzezPlatnika].Type);
        Assert.Equal(MissingFactType.List, types[FactKeys.UbezpieczenieTytuly].Type);
        Assert.Equal(MissingFactType.List, types[FactKeys.EzlaZwolnienia].Type);
    }

    [Fact]
    public void Questions_about_the_other_parent_are_worded_differently_and_case_facts_have_no_person()
    {
        var self = FactQuestionBook.Ask(FactKeys.UmowaDataKonca, PersonRef.Wnioskodawca);
        var other = FactQuestionBook.Ask(FactKeys.UmowaDataKonca, PersonRef.DrugiRodzic);
        var caseFact = FactQuestionBook.Ask(FactKeys.DzieckoLiczba, PersonRef.Wnioskodawca);

        Assert.Equal("Do kiedy trwa Twoja umowa?", self.Question);
        Assert.Equal("Do kiedy trwa umowa drugiego rodzica?", other.Question);
        Assert.Equal(PersonRef.DrugiRodzic, other.Person);
        Assert.Null(caseFact.Person);
    }

    [Fact]
    public void Missing_child_count_comes_back_as_a_question_and_disappears_once_given()
    {
        var mother = Person(ParentRole.Matka, Title(), 150);
        var without = Facts(Case(EventKind.Ciaza, "2027-03-15") with { DzieckoLiczba = null }, mother);
        var with = Facts(Case(EventKind.Ciaza, "2027-03-15"), mother);

        var asked = Assess(without);
        var answered = Assess(with);

        var question = Asked(asked, FactKeys.DzieckoLiczba);
        Assert.NotNull(question);
        Assert.Equal("Ile dzieci rodzi się (albo zostaje przyjętych) jednocześnie?", question.Question);
        Assert.Equal(MissingFactType.Integer, question.Type);
        Assert.Contains(asked.Benefits, b => b.Id == BenefitId.Macierzynski && b.Status == BenefitStatus.BrakujeDanych);
        Assert.Null(Asked(answered, FactKeys.DzieckoLiczba));
        Assert.Equal(BenefitStatus.Przysluguje, Get(answered, BenefitId.Macierzynski).Status);
    }

    [Fact]
    public void Missing_insurance_titles_come_back_as_a_list_question_and_disappear_once_given()
    {
        var bare = new PersonFactsBuilder { Role = ParentRole.Matka }.Build();
        var given = new PersonFactsBuilder { Role = ParentRole.Matka, Titles = [Title()], Headcount = 150 }.Build();

        var asked = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), bare));
        var answered = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), given));

        var question = Asked(asked, FactKeys.UbezpieczenieTytuly, PersonRef.Wnioskodawca);
        Assert.NotNull(question);
        Assert.Equal(MissingFactType.List, question.Type);
        Assert.Null(Asked(answered, FactKeys.UbezpieczenieTytuly, PersonRef.Wnioskodawca));
    }

    [Fact]
    public void Missing_role_is_an_enum_question_with_both_options()
    {
        var noRole = new PersonFactsBuilder { Titles = [Title()], Headcount = 150 }.Build();

        var result = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), noRole));

        var question = Asked(result, FactKeys.OsobaRola, PersonRef.Wnioskodawca);
        Assert.NotNull(question);
        Assert.Equal(MissingFactType.Enum, question.Type);
        Assert.Equal(["matka", "ojciec"], question.Options);
    }

    [Fact]
    public void Missing_payer_headcount_is_asked_and_disappears_once_given()
    {
        var asked = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), null)));
        var answered = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150)));

        Assert.NotNull(Asked(asked, FactKeys.PlatnikLiczbaUbezpieczonych, PersonRef.Wnioskodawca));
        Assert.Null(Asked(answered, FactKeys.PlatnikLiczbaUbezpieczonych, PersonRef.Wnioskodawca));
    }

    [Fact]
    public void Contract_questions_are_asked_before_birth_for_employees_in_order_and_then_disappear()
    {
        Assert.NotNull(Asked(Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150))), FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));

        var fixedTerm = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150, b => b.FixedTerm = true)));
        Assert.Null(Asked(fixedTerm, FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));
        Assert.NotNull(Asked(fixedTerm, FactKeys.UmowaDataKonca, PersonRef.Wnioskodawca));

        var withEnd = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150, b => { b.FixedTerm = true; b.ContractEnd = D("2027-06-30"); })));
        Assert.Null(Asked(withEnd, FactKeys.UmowaDataKonca, PersonRef.Wnioskodawca));

        var permanent = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150, b => b.FixedTerm = false)));
        Assert.Null(Asked(permanent, FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));
        Assert.Null(Asked(permanent, FactKeys.UmowaDataKonca, PersonRef.Wnioskodawca));
    }

    [Fact]
    public void Contract_questions_are_asked_only_when_they_matter()
    {
        var afterBirth = Assess(Facts(Case(EventKind.Urodzenie, "2026-10-08"), Person(ParentRole.Matka, Title(), 150)));
        var business = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(TitleKind.Dzialalnosc, sickness: SicknessInsurance.Dobrowolne), null, b => b.Debt = new ContributionDebt(D("2026-11-01"), 0m))));
        var contractor = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(TitleKind.Zleceniobiorca, sickness: SicknessInsurance.Dobrowolne), 150)));

        Assert.Null(Asked(afterBirth, FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));
        Assert.Null(Asked(business, FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));
        Assert.NotNull(Asked(contractor, FactKeys.UmowaNaCzasOkreslony, PersonRef.Wnioskodawca));
    }

    [Fact]
    public void Facts_with_nothing_filled_in_ask_for_the_event_and_who_to_calculate_for()
    {
        var result = Assess(new FactSet());

        Assert.NotNull(Asked(result, FactKeys.ZdarzenieRodzaj));
        Assert.NotNull(Asked(result, FactKeys.ZdarzenieDlaKogo));
        Assert.All(result.Benefits.Where(b => b.Id != BenefitId.Pogrzebowy), b => Assert.Equal(BenefitStatus.BrakujeDanych, b.Status));
        Assert.Equal(BenefitStatus.NieDotyczy, Get(result, BenefitId.Pogrzebowy).Status);
    }

    [Fact]
    public void Who_to_calculate_for_is_asked_once_and_disappears_once_given()
    {
        var mother = Person(ParentRole.Matka, Title(), 150);
        var withoutScope = Facts(Case(EventKind.Ciaza, "2027-03-15") with { ZdarzenieDlaKogo = null }, mother);

        Assert.NotNull(Asked(Assess(withoutScope), FactKeys.ZdarzenieDlaKogo));
        Assert.Null(Asked(Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), mother)), FactKeys.ZdarzenieDlaKogo));
    }

    [Fact]
    public void Second_parent_without_facts_is_asked_for_their_role_and_questions_about_them_are_worded_that_way()
    {
        var mother = Person(ParentRole.Matka, Title(), 150);
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-15") with { ZdarzenieDlaKogo = F(CalculationScope.JaIDrugiRodzic) }, mother);

        var asked = Assess(facts);
        var withRole = Assess(facts with { DrugiRodzic = new PersonFactsBuilder { Role = ParentRole.Ojciec }.Build() });

        var role = Asked(asked, FactKeys.OsobaRola, PersonRef.DrugiRodzic);
        Assert.NotNull(role);
        Assert.Equal("Czy drugi rodzic jest matką, czy ojcem dziecka?", role.Question);
        Assert.Null(Asked(withRole, FactKeys.OsobaRola, PersonRef.DrugiRodzic));
        var titles = Asked(withRole, FactKeys.UbezpieczenieTytuly, PersonRef.DrugiRodzic);
        Assert.NotNull(titles);
        Assert.Contains("drugi rodzic", titles.Question);
    }

    [Fact]
    public void Questions_are_not_asked_for_benefits_that_are_already_denied()
    {
        var uninsured = Person(ParentRole.Matka, null, null, b => b.Titles = []);

        var result = Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), uninsured));

        Assert.DoesNotContain(result.MissingFacts, m => m.Fact == FactKeys.PlatnikLiczbaUbezpieczonych);
        Assert.DoesNotContain(result.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieTytuly);
    }

    [Fact]
    public void Every_missing_fact_uses_the_dictionary_wording_and_is_unique()
    {
        var results = new[]
        {
            Assess(new FactSet()),
            Assess(Facts(Case(EventKind.Ciaza, "2027-03-15") with { DzieckoLiczba = null }, new PersonFactsBuilder { Role = ParentRole.Matka }.Build())),
            Assess(Facts(Case(EventKind.Przyjecie, "2027-02-10"), Person(ParentRole.Matka, Title(), 150))),
        };

        foreach (var result in results)
        {
            Assert.Equal(result.MissingFacts.Count, result.MissingFacts.Select(m => (m.Fact, m.Person)).Distinct().Count());
            Assert.All(result.MissingFacts, m =>
            {
                var expected = FactQuestionBook.Ask(m.Fact, m.Person);
                Assert.Equal(expected.Question, m.Question);
                Assert.Equal(expected.Type, m.Type);
                Assert.Equal(expected.Options, m.Options);
            });
        }
    }

    [Fact]
    public void Adoption_asks_for_the_path_and_the_childs_birth_date_until_answered()
    {
        var mother = Person(ParentRole.Matka, Title(), 150);
        var asked = Assess(Facts(Case(EventKind.Przyjecie, "2027-02-10"), mother));
        var answered = Assess(Facts(
            Case(EventKind.Przyjecie, "2027-02-10") with
            {
                PrzyjecieSciezka = F(AdoptionPath.Przysposobienie),
                PrzyjecieWniosekDoSadu = F(true),
                DzieckoDataUrodzenia = F(D("2022-05-01")),
            },
            mother));

        Assert.NotNull(Asked(asked, FactKeys.PrzyjecieSciezka));
        Assert.Equal(["przysposobienie", "rodzina_zastepcza_niezawodowa", "rodzina_zastepcza_zawodowa"], Asked(asked, FactKeys.PrzyjecieSciezka)!.Options);
        Assert.Null(Asked(answered, FactKeys.PrzyjecieSciezka));
        Assert.Equal(BenefitStatus.Przysluguje, Get(answered, BenefitId.PrzyjecieDziecka).Status);
    }
}
