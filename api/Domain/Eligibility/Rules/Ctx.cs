using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

internal static class Ctx
{
    public static PersonRef Other(this EvaluationContext context) =>
        context.Person == PersonRef.Wnioskodawca ? PersonRef.DrugiRodzic : PersonRef.Wnioskodawca;

    public static PersonFacts? OtherFacts(this EvaluationContext context) => context.Facts.For(context.Other());

    public static DateOnly? EventDate(this EvaluationContext context) => context.Facts.Sprawa.ZdarzenieData?.Value;

    public static EventKind? Event(this EvaluationContext context) => context.Facts.Sprawa.ZdarzenieRodzaj?.Value;

    public static ParentRole? Role(this EvaluationContext context) => context.PersonFacts?.OsobaRola?.Value;

    public static ParentRole? OtherRole(this EvaluationContext context) =>
        context.OtherFacts()?.OsobaRola?.Value ?? (context.Role() is { } r ? (r == ParentRole.Matka ? ParentRole.Ojciec : ParentRole.Matka) : null);

    public static decimal Param(this EvaluationContext context, string key, DateOnly date) => context.Parameters.GetDecimal(key, date);

    public static decimal? TryParam(this EvaluationContext context, string key, DateOnly date)
    {
        try
        {
            return context.Parameters.Get(key, date).Value;
        }
        catch (ParameterNotFoundException)
        {
            return null;
        }
    }
}
