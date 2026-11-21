using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>Pytanie o brakujący fakt wskazuje świadczenia, których dotyczy — karta „brakuje danych” pokazuje tylko swoje pytania.</summary>
public sealed class MissingFactBenefitIdsTests
{
    [Fact]
    public void Every_missing_fact_lists_benefits_and_each_waiting_card_has_its_questions()
    {
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-20"), Person(ParentRole.Matka));

        var result = Assess(facts, engine: FullEngine());

        var titles = result.MissingFacts.Single(m => m.Fact == FactKeys.UbezpieczenieTytuly && m.Person == PersonRef.Wnioskodawca);
        Assert.Contains(BenefitId.Macierzynski, titles.BenefitIds!);
        Assert.All(result.MissingFacts, m => Assert.NotNull(m.BenefitIds));

        foreach (var card in result.Benefits.Where(b => b.Status == BenefitStatus.BrakujeDanych))
        {
            Assert.Contains(result.MissingFacts, m => m.Person == card.Person && m.BenefitIds!.Contains(card.Id));
        }
    }

    [Fact]
    public void General_question_has_no_benefit()
    {
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-20") with { ZdarzenieDlaKogo = null }, Person(ParentRole.Matka, Title()));

        var scope = Assess(facts, engine: FullEngine()).MissingFacts.Single(m => m.Fact == FactKeys.ZdarzenieDlaKogo);

        Assert.Empty(scope.BenefitIds!);
    }

    [Fact]
    public void Averaged_variant_application_date_belongs_to_maternity_and_parental()
    {
        var facts = LoadPersona("monika-demo");
        facts = facts with { Wnioskodawca = facts.Wnioskodawca with { PlanWariantWyplaty = F(PaymentVariant.Usredniony) } };

        var question = Assess(facts, engine: FullEngine()).MissingFacts.Single(m => m.Fact == FactKeys.PlanDataWnioskuUsrednionego);

        Assert.Contains(BenefitId.Macierzynski, question.BenefitIds!);
        Assert.Contains(BenefitId.Rodzicielski, question.BenefitIds!);
    }
}
