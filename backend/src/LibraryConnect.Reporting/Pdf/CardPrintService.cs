using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// In phích thư mục ra PDF (II.10).
///
/// Two paper modes, because libraries print cards two different ways. Printing straight onto
/// pre-cut card stock needs one card per page at exactly the card's size; printing onto plain A4 to
/// cut up afterwards needs as many cards as fit, laid out in a grid with a hairline border to cut
/// along. Everything is measured in millimetres, which is what the designer screen and the guillotine
/// both use.
/// </summary>
public class CardPrintService : ICardPrintService
{
    /// <summary>Lề trang khi xếp nhiều phích trên một tờ A4.</summary>
    private const float SheetMarginMm = 10;

    /// <summary>Khoảng cách giữa hai phích trên tờ A4, đủ để cắt.</summary>
    private const float GapMm = 4;

    private const float A4WidthMm = 210;
    private const float A4HeightMm = 297;

    static CardPrintService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(CardTemplateDto template, IReadOnlyList<CardToPrint> cards, bool multiplePerPage)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(cards);

        return multiplePerPage
            ? RenderSheets(template, cards)
            : RenderIndividualCards(template, cards);
    }

    /// <summary>Mỗi phích một trang đúng khổ phích, để in lên bìa phích in sẵn.</summary>
    private static byte[] RenderIndividualCards(CardTemplateDto template, IReadOnlyList<CardToPrint> cards)
    {
        var document = Document.Create(container =>
        {
            foreach (var card in cards)
            {
                container.Page(page =>
                {
                    page.Size((float)template.WidthMm, (float)template.HeightMm, Unit.Millimetre);
                    page.Margin(0);
                    page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato));
                    page.Content().Element(element => DrawCard(element, template, card));
                });
            }
        });

        return document.GeneratePdf();
    }

    /// <summary>Xếp nhiều phích trên tờ A4 để in rồi cắt.</summary>
    private static byte[] RenderSheets(CardTemplateDto template, IReadOnlyList<CardToPrint> cards)
    {
        var columns = Math.Max(1, (int)((A4WidthMm - 2 * SheetMarginMm + GapMm) / (template.WidthMm + GapMm)));
        var rows = Math.Max(1, (int)((A4HeightMm - 2 * SheetMarginMm + GapMm) / (template.HeightMm + GapMm)));
        var perPage = columns * rows;

        var document = Document.Create(container =>
        {
            for (var start = 0; start < cards.Count; start += perPage)
            {
                var page = cards.Skip(start).Take(perPage).ToList();

                container.Page(pageDescriptor =>
                {
                    pageDescriptor.Size(PageSizes.A4);
                    pageDescriptor.Margin(SheetMarginMm, Unit.Millimetre);
                    pageDescriptor.DefaultTextStyle(style => style.FontFamily(Fonts.Lato));

                    pageDescriptor.Content().Column(column =>
                    {
                        for (var row = 0; row < rows; row++)
                        {
                            var slice = page.Skip(row * columns).Take(columns).ToList();

                            if (slice.Count == 0)
                            {
                                break;
                            }

                            column.Item().PaddingBottom(GapMm, Unit.Millimetre).Row(rowDescriptor =>
                            {
                                foreach (var card in slice)
                                {
                                    rowDescriptor.ConstantItem((float)template.WidthMm, Unit.Millimetre)
                                        .Height((float)template.HeightMm, Unit.Millimetre)
                                        .Element(element => DrawCard(element, template, card));

                                    rowDescriptor.ConstantItem(GapMm, Unit.Millimetre);
                                }
                            });
                        }
                    });
                });
            }
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Vẽ một phích.
    ///
    /// The boxes are placed by absolute position because that is what a card is: a fixed piece of
    /// board with the heading always in the same place, so a drawer of them can be read by flicking
    /// through. Text that does not fit its box is clipped rather than reflowed, for the same reason.
    /// </summary>
    private static void DrawCard(IContainer container, CardTemplateDto template, CardToPrint card)
    {
        var canvas = template.Layout.ShowBorder
            ? container.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
            : container;

        canvas.Padding((float)template.Layout.Padding, Unit.Millimetre).Layers(layers =>
        {
            // The primary layer is what gives the card its size; the boxes sit on top of it.
            layers.PrimaryLayer().Height(
                (float)(template.HeightMm - 2 * template.Layout.Padding), Unit.Millimetre);

            foreach (var box in template.Layout.Boxes)
            {
                var text = CardContentBuilder.Resolve(card.Content, card.Record, box.Source);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(box.Prefix))
                {
                    text = box.Prefix + text;
                }

                layers.Layer()
                    .TranslateX((float)box.X, Unit.Millimetre)
                    .TranslateY((float)box.Y, Unit.Millimetre)
                    .Width((float)box.Width, Unit.Millimetre)
                    .Height((float)box.Height, Unit.Millimetre)
                    .Element(element => DrawBox(element, box, text));
            }
        });
    }

    private static void DrawBox(IContainer container, CardBoxDto box, string text)
    {
        var element = box.Border
            ? container.Border(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(1, Unit.Millimetre)
            : container;

        element = box.Align switch
        {
            "center" => element.AlignCenter(),
            "right" => element.AlignRight(),
            _ => element
        };

        var span = element.Text(text).FontSize((float)box.FontSize);

        if (box.Bold)
        {
            span.Bold();
        }

        if (box.Italic)
        {
            span.Italic();
        }
    }
}
