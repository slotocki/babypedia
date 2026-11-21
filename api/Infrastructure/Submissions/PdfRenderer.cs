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
    private static readonly Color Accent = Color.FromRgb(0x0D, 0x47, 0x7D);
    private static readonly Color Teal = Color.FromRgb(0x00, 0x7A, 0x67);
    private static readonly Color Line = Color.FromRgb(0xDD, 0xE5, 0xED);
    private static readonly Color LabelFill = Color.FromRgb(0xF3, 0xF7, 0xFB);
    private static readonly Color NoticeFill = Color.FromRgb(0xEC, 0xF5, 0xF2);

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
            space.Format.SpaceBefore = Unit.FromCentimeter(1.1);
            space.Format.Alignment = ParagraphAlignment.Right;
            space.Format.Font.Color = Muted;
            space.Format.KeepWithNext = true;
            space.AddText("________________________________");
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
        normal.Font.Size = 10;
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
        top.Format.Font.Size = 9;
        top.Format.Font.Color = Muted;
        top.Format.SpaceAfter = Unit.FromPoint(13);
        top.Format.TabStops.AddTabStop(Unit.FromCentimeter(17), TabAlignment.Right);
        top.Format.Borders.Bottom = new Border { Width = 1.4, Color = Teal };
        top.Format.Borders.DistanceFromBottom = Unit.FromPoint(7);
        var wordmark = top.AddFormattedText("Kompas Rodzica", TextFormat.Bold);
        wordmark.Font.Color = Accent;
        top.AddTab();
        top.AddText("Nr potwierdzenia: " + model.Number);

        var title = section.AddParagraph(model.Title);
        title.Format.Font.Size = 19;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Accent;
        title.Format.SpaceBefore = Unit.FromPoint(7);
        title.Format.SpaceAfter = Unit.FromPoint(10);
        title.Format.KeepWithNext = true;

        var place = section.AddParagraph();
        place.Format.Font.Color = Muted;
        place.Format.SpaceAfter = Unit.FromPoint(5);
        place.AddFormattedText("Miejsce i data: ", TextFormat.Bold);
        place.AddText(string.IsNullOrEmpty(model.Place)
            ? DocumentBuilder.FormatDate(model.Date)
            : $"{model.Place}, {DocumentBuilder.FormatDate(model.Date)}");

        if (!string.IsNullOrEmpty(model.Addressee))
        {
            var to = section.AddParagraph();
            to.Format.SpaceAfter = Unit.FromPoint(5);
            to.AddFormattedText("Do: ", TextFormat.Bold);
            to.AddText(model.Addressee);
        }
    }

    private static void AddBanner(Section section, string text)
    {
        var banner = section.AddParagraph(text);
        banner.Format.Font.Size = 8.5;
        banner.Format.Font.Color = Teal;
        banner.Format.Shading.Color = NoticeFill;
        banner.Format.LeftIndent = Unit.FromPoint(7);
        banner.Format.Borders.Left = new Border { Width = 1.25, Color = Teal };
        banner.Format.Borders.DistanceFromLeft = Unit.FromPoint(5);
        banner.Format.SpaceBefore = Unit.FromPoint(4);
        banner.Format.SpaceAfter = Unit.FromPoint(9);
    }

    private static void Heading(Section section, string text)
    {
        var heading = section.AddParagraph(text);
        heading.Format.Font.Size = 11;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Accent;
        heading.Format.SpaceBefore = Unit.FromPoint(12);
        heading.Format.SpaceAfter = Unit.FromPoint(5);
        heading.Format.KeepWithNext = true;
    }

    private static void AddSection(Section section, DocumentSection part)
    {
        Heading(section, part.Title);

        var withBasis = part.Rows.Any(r => r.Basis is not null);
        var table = section.AddTable();
        table.Rows.LeftIndent = 0;
        table.LeftPadding = Unit.FromPoint(6);
        table.RightPadding = Unit.FromPoint(6);
        table.TopPadding = Unit.FromPoint(4.5);
        table.BottomPadding = Unit.FromPoint(4.5);
        table.AddColumn(Unit.FromCentimeter(withBasis ? 6.6 : 7.5));
        table.AddColumn(Unit.FromCentimeter(withBasis ? 7.2 : 9.5));
        if (withBasis)
        {
            table.AddColumn(Unit.FromCentimeter(3.2));
        }

        foreach (var data in part.Rows)
        {
            var row = table.AddRow();
            row.Borders.Bottom = new Border { Width = 0.5, Color = Line };
            row.Cells[0].Shading.Color = LabelFill;
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
