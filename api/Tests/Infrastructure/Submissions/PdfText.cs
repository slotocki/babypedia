using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

/// <summary>Wyciąga tekst z PDF-a wygenerowanego przez PDFsharp (czcionki Type0/Identity-H + ToUnicode).</summary>
internal static partial class PdfText
{
    private static readonly Encoding WinAnsi = CreateWinAnsi();

    private static Encoding CreateWinAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }

    public static string Extract(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        var text = new StringBuilder();
        foreach (var page in document.Pages)
        {
            var maps = FontMaps(page);
            Dictionary<ushort, string>? current = null;
            Walk(ContentReader.ReadContent(page), maps, ref current, text);
            text.AppendLine();
        }

        return text.ToString();
    }

    public static string Compact(string text) => AnyWhiteSpace().Replace(text, "");

    public static void AssertHas(string pdfText, string expected) => Assert.Contains(Compact(expected), Compact(pdfText));

    public static int PageCount(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return document.PageCount;
    }

    public static string Title(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return document.Info.Title;
    }

    private static void Walk(CSequence sequence, Dictionary<string, Dictionary<ushort, string>> maps, ref Dictionary<ushort, string>? current, StringBuilder text)
    {
        foreach (var item in sequence)
        {
            if (item is not COperator op)
            {
                continue;
            }

            switch (op.OpCode.OpCodeName)
            {
                case OpCodeName.Tf when op.Operands.Count > 0 && op.Operands[0] is CName name:
                    current = maps.GetValueOrDefault(name.Name.TrimStart('/'));
                    break;
                case OpCodeName.Tj when op.Operands.Count > 0 && op.Operands[0] is CString s:
                    Decode(s, current, text);
                    text.Append(' ');
                    break;
                case OpCodeName.TJ when op.Operands.Count > 0 && op.Operands[0] is CArray array:
                    foreach (var element in array.OfType<CString>())
                    {
                        Decode(element, current, text);
                    }

                    text.Append(' ');
                    break;
            }
        }
    }

    private static void Decode(CString value, Dictionary<ushort, string>? map, StringBuilder text)
    {
        var raw = value.Value;
        if (map is null)
        {
            text.Append(WinAnsi.GetString(raw.Select(c => (byte)c).ToArray()));
            return;
        }

        for (var i = 0; i + 1 < raw.Length; i += 2)
        {
            var glyph = (ushort)((raw[i] << 8) | raw[i + 1]);
            if (map.TryGetValue(glyph, out var unicode))
            {
                text.Append(unicode);
            }
        }
    }

    private static Dictionary<string, Dictionary<ushort, string>> FontMaps(PdfPage page)
    {
        var maps = new Dictionary<string, Dictionary<ushort, string>>();
        var fonts = page.Resources.Elements.GetDictionary("/Font");
        if (fonts is null)
        {
            return maps;
        }

        foreach (var key in fonts.Elements.Keys)
        {
            var font = fonts.Elements.GetDictionary(key);
            var toUnicode = font?.Elements.GetDictionary("/ToUnicode");
            if (toUnicode?.Stream is null)
            {
                continue;
            }

            maps[key.TrimStart('/')] = ParseCMap(Encoding.ASCII.GetString(toUnicode.Stream.UnfilteredValue));
        }

        return maps;
    }

    private static Dictionary<ushort, string> ParseCMap(string cmap)
    {
        var map = new Dictionary<ushort, string>();
        foreach (Match m in BfChar().Matches(BfCharBlocks().Matches(cmap).Aggregate("", (acc, b) => acc + b.Groups[1].Value)))
        {
            map[Hex16(m.Groups[1].Value)] = Utf16(m.Groups[2].Value);
        }

        foreach (Match m in BfRange().Matches(BfRangeBlocks().Matches(cmap).Aggregate("", (acc, b) => acc + b.Groups[1].Value)))
        {
            var from = Hex16(m.Groups[1].Value);
            var to = Hex16(m.Groups[2].Value);
            var start = Utf16(m.Groups[3].Value);
            for (var code = from; code <= to; code++)
            {
                map[code] = char.ConvertFromUtf32(char.ConvertToUtf32(start, 0) + (code - from));
                if (code == ushort.MaxValue)
                {
                    break;
                }
            }
        }

        return map;
    }

    private static ushort Hex16(string hex) => ushort.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static string Utf16(string hex) => Encoding.BigEndianUnicode.GetString(Convert.FromHexString(hex));

    [GeneratedRegex(@"\s+")]
    private static partial Regex AnyWhiteSpace();

    [GeneratedRegex(@"beginbfchar(.*?)endbfchar", RegexOptions.Singleline)]
    private static partial Regex BfCharBlocks();

    [GeneratedRegex(@"beginbfrange(.*?)endbfrange", RegexOptions.Singleline)]
    private static partial Regex BfRangeBlocks();

    [GeneratedRegex(@"<([0-9A-Fa-f]{4})>\s*<([0-9A-Fa-f]{4,8})>")]
    private static partial Regex BfChar();

    [GeneratedRegex(@"<([0-9A-Fa-f]{4})>\s*<([0-9A-Fa-f]{4})>\s*<([0-9A-Fa-f]{4,8})>")]
    private static partial Regex BfRange();
}
