using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace WiFiQrGenerator.Services;

/// <summary>
/// Creates a Word document that contains the QR image and the network credentials.
/// </summary>
public static class WordService
{
    /// <summary>
    /// Creates a .docx file with a centered QR image and the network name / password below it.
    /// </summary>
    public static void Create(
        string docPath,
        byte[] qrPng,
        string ssid,
        string password,
        string networkLabel,
        string passwordLabel)
    {
        using var wordDocument = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document);

        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        // --- QR image (centered) ---
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var ms = new MemoryStream(qrPng))
        {
            imagePart.FeedData(ms);
        }
        string relationshipId = mainPart.GetIdOfPart(imagePart);

        var imageParagraph = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center }
            )
        );
        imageParagraph.Append(new Run(CreateImageDrawing(relationshipId, 300, 300)));
        body.Append(imageParagraph);

        // --- Spacer ---
        body.Append(new Paragraph(new Run(new Text(""))));

        // --- "Network name: <ssid>" ---
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
            new Run(new Text($"{networkLabel}: {ssid}"))
        ));

        // --- "Password: <password>" ---
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
            new Run(new Text($"{passwordLabel}: {password}"))
        ));

        mainPart.Document.Save();
    }

    /// <summary>
    /// Builds a Drawing element that embeds a picture into the Word document.
    /// </summary>
    private static Drawing CreateImageDrawing(string relationshipId, long widthPx, long heightPx)
    {
        // Convert pixels to EMU (English Metric Units): 1 px = 9525 EMU.
        long widthEmu = widthPx * 9525;
        long heightEmu = heightPx * 9525;

        var element = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = 1U, Name = "WiFi QR Code" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }
                ),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "wifi_qr.png" },
                                new PIC.NonVisualPictureDrawingProperties()
                            ),
                            new PIC.BlipFill(
                                new A.Blip(
                                    new A.BlipExtensionList(
                                        new A.BlipExtension { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }
                                    )
                                )
                                {
                                    Embed = relationshipId,
                                    CompressionState = A.BlipCompressionValues.Print
                                },
                                new A.Stretch(new A.FillRectangle())
                            ),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }
                                ),
                                new A.PresetGeometry(new A.AdjustValueList())
                                {
                                    Preset = A.ShapeTypeValues.Rectangle
                                }
                            )
                        )
                    )
                    {
                        // XML namespace identifier for the picture schema (not a network call).
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                    }
                )
            )
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
                EditId = "50D07946"
            }
        );

        return element;
    }
}