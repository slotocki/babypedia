namespace BabyRadnik.Domain.Basis;

public static class BasisParameterKeys
{
    public const string OdliczenieSkladek = "odliczenie_skladek";
    public const string PodstawaMinimalnaPelnyEtat = "podstawa_minimalna_pelny_etat";
    public const string MaxPodstawaDobrowolnejSkladki = "max_podstawa_dobrowolnej_skladki";
    public const string LimitPodstawyZasilku = "max_podstawa_dobrowolnej_skladki.limit_podstawy_zasilku";
    public const string NajnizszaPodstawaDzialalnosc = "najnizsza_podstawa_skladki_chorobowej_dzialalnosc";

    public static readonly IReadOnlyList<string> NotInSeed = [NajnizszaPodstawaDzialalnosc];
}
