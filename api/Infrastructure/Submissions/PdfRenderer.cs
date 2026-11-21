using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace BabyRadnik.Infrastructure.Submissions;

public static class PdfRenderer
{
    public const string FooterText = "Wygenerowano w Kompasie Rodzica — symulacja; sprawdź dane przed złożeniem";

    private const string FontName = "Roboto";
    private static readonly Color Ink = Color.FromRgb(0x1F, 0x29, 0x37);
    private static readonly Color Muted = Color.FromRgb(0x5B, 0x65, 0x73);
    private static readonly Color Accent = Color.FromRgb(0x0B, 0x5C, 0x7A);
    private static readonly Color Line = Color.FromRgb(0xC9, 0xD1, 0xD9);
    private static readonly Color BannerFill = Color.FromRgb(0xFF, 0xF3, 0xC4);
    private static readonly Color BannerLine = Color.FromRgb(0x8A, 0x6D, 0x00);

    public static byte[] Render(DocumentModel model)
    {
        EmbeddedFontResolver.Register();

        var doc = new Document();
        doc.Info.Title = model.Title;
        doc.Info.Subject = $"Symulacja — Kompas Rodzica, nr {model.Number}";
        doc.Info.Author = "Kompas Rodzica (symulacja)";
        DefineStyles(doc);

        var section = doc.AddSection();
        var page = section.PageSetup;
        page.PageFormat = PageFormat.A4;
        page.LeftMargin = Unit.FromCentimeter(2);
        page.RightMargin = Unit.FromCentimeter(2);
        page.TopMargin = Unit.FromCentimeter(1.8);
        page.BottomMargin = Unit.FromCentimeter(2.2);
        page.FooterDistance = Unit.FromCentimeter(0.9);

        AddFooter(section);
        AddHeader(section, model);
        AddBanner(section, model.Banner);

        foreach (var part in model.Sections)
        {
            AddSection(section, part);
        }

        if (model.Attachments.Count > 0)
        {
            Heading(section, "Załączniki do wniosku");
            foreach (var attachment in model.Attachments)
            {
                var p = section.AddParagraph();
                p.Format.LeftIndent = Unit.FromCentimeter(0.6);
                p.Format.FirstLineIndent = Unit.FromCentimeter(-0.6);
                p.Format.SpaceAfter = Unit.FromPoint(3);
                p.AddText("[  ]  " + attachment.Text);
                if (attachment.Basis is not null)
                {
                    var basis = p.AddFormattedText(" (" + attachment.Basis + ")");
                    basis.Font.Size = 7.5;
                    basis.Font.Color = Muted;
                }
            }
        }

        if (model.SignatureLine)
        {
            var space = section.AddParagraph();
            space.Format.SpaceBefore = Unit.FromCentimeter(1.4);
            space.Format.Alignment = ParagraphAlignment.Right;
            space.AddText("………………………………………");
            var caption = section.AddParagraph(model.SignatureCaption);
            caption.Format.Alignment = ParagraphAlignment.Right;
            caption.Format.Font.Size = 8;
            caption.Format.Font.Color = Muted;
        }

        if (model.Notes.Count > 0)
        {
            Heading(section, "Uwagi");
            foreach (var note in model.Notes)
            {
                var p = section.AddParagraph();
                p.Format.Font.Size = 8.5;
                p.Format.SpaceAfter = Unit.FromPoint(3);
                p.AddText(note.Text);
                if (note.Basis is not null)
                {
                    var basis = p.AddFormattedText(" (" + note.Basis + ")");
                    basis.Font.Color = Muted;
                }
            }
        }

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static void DefineStyles(Document doc)
    {
        var normal = doc.Styles[StyleNames.Normal]!;
        normal.Font.Name = FontName;
        normal.Font.Size = 9.5;
        normal.Font.Color = Ink;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(2);
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 8;
        footer.Format.Font.Color = Muted;
        footer.Format.Borders.Top = new Border { Width = 0.5, Color = Line };
        footer.Format.Borders.DistanceFromTop = Unit.FromPoint(4);
        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(17), TabAlignment.Right);
        footer.AddText(FooterText);
        footer.AddTab();
        footer.AddText("Strona ");
        footer.AddPageField();
        footer.AddText(" z ");
        footer.AddNumPagesField();
    }

    private static void AddHeader(Section section, DocumentModel model)
    {
        var top = section.AddParagraph();
        top.Format.Font.Size = 8.5;
        top.Format.Font.Color = Muted;
        top.Format.TabStops.AddTabStop(Unit.FromCentimeter(17), TabAlignment.Right);
        top.AddText("Kompas Rodzica — SYMULACJA");
        top.AddTab();
        top.AddText("Nr potwierdzenia: " + model.Number);

        var place = section.AddParagraph();
        place.Format.Alignment = ParagraphAlignment.Right;
        place.Format.SpaceBefore = Unit.FromPoint(10);
        place.AddText(string.IsNullOrEmpty(model.Place)
            ? DocumentBuilder.FormatDate(model.Date)
            : $"{model.Place}, {DocumentBuilder.FormatDate(model.Date)}");

        if (!string.IsNullOrEmpty(model.Addressee))
        {
            var to = section.AddParagraph();
            to.Format.SpaceBefore = Unit.FromPoint(6);
            to.AddFormattedText("Do: ", TextFormat.Bold);
            to.AddText(model.Addressee);
        }

        var title = section.AddParagraph(model.Title);
        title.Format.Font.Size = 15;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Accent;
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.SpaceBefore = Unit.FromPoint(14);
        title.Format.SpaceAfter = Unit.FromPoint(8);
    }

    private static void AddBanner(Section section, string text)
    {
        var banner = section.AddParagraph(text);
        banner.Format.Font.Bold = true;
        banner.Format.Font.Size = 9.5;
        banner.Format.Shading.Color = BannerFill;
        banner.Format.Borders.Width = 0.75;
        banner.Format.Borders.Color = BannerLine;
        banner.Format.Borders.Distance = Unit.FromPoint(5);
        banner.Format.SpaceAfter = Unit.FromPoint(8);
    }

    private static void Heading(Section section, string text)
    {
        var heading = section.AddParagraph(text);
        heading.Format.Font.Size = 11;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Accent;
        heading.Format.SpaceBefore = Unit.FromPoint(10);
        heading.Format.SpaceAfter = Unit.FromPoint(4);
        heading.Format.KeepWithNext = true;
    }

    private static void AddSection(Section section, DocumentSection part)
    {
        Heading(section, part.Title);

        var withBasis = part.Rows.Any(r => r.Basis is not null);
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Line;
        table.Rows.LeftIndent = 0;
        table.LeftPadding = Unit.FromPoint(4);
        table.RightPadding = Unit.FromPoint(4);
        table.TopPadding = Unit.FromPoint(3);
        table.BottomPadding = Unit.FromPoint(3);
        table.AddColumn(Unit.FromCentimeter(withBasis ? 6.4 : 7.5));
        table.AddColumn(Unit.FromCentimeter(withBasis ? 7.1 : 9.5));
        if (withBasis)
        {
            table.AddColumn(Unit.FromCentimeter(3.5));
        }

        foreach (var data in part.Rows)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(data.Label).Format.Font.Color = Muted;
            row.Cells[1].AddParagraph(data.Value).Format.Font.Bold = true;
            if (withBasis)
            {
                var basis = row.Cells[2].AddParagraph(data.Basis ?? string.Empty);
                basis.Format.Font.Size = 7.5;
                basis.Format.Font.Color = Muted;
            }
        }
    }
}
