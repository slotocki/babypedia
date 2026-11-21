using System.Reflection;
using PdfSharp.Fonts;

namespace BabyRadnik.Infrastructure.Submissions;

/// <summary>
/// Czcionka Roboto (Apache-2.0) osadzona w zestawie; obraz aspnet nie ma fontów systemowych.
/// Każda rodzina jest mapowana na Roboto, kursywa nie jest używana.
/// </summary>
public sealed class EmbeddedFontResolver : IFontResolver
{
    private const string Regular = "Roboto-Regular";
    private const string Bold = "Roboto-Bold";
    private static readonly object Gate = new();
    private static readonly Lazy<byte[]> RegularBytes = new(() => Load(Regular));
    private static readonly Lazy<byte[]> BoldBytes = new(() => Load(Bold));

    public static void Register()
    {
        lock (Gate)
        {
            if (GlobalFontSettings.FontResolver is not EmbeddedFontResolver)
            {
                GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
            }
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? Bold : Regular, mustSimulateBold: false, mustSimulateItalic: isItalic);

    public byte[]? GetFont(string faceName) => faceName switch
    {
        Bold => BoldBytes.Value,
        _ => RegularBytes.Value,
    };

    private static byte[] Load(string face)
    {
        var assembly = typeof(EmbeddedFontResolver).Assembly;
        using var stream = assembly.GetManifestResourceStream($"BabyRadnik.Fonts.{face}.ttf")
            ?? throw new InvalidOperationException($"Brak osadzonego fontu {face}.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
