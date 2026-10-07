using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;

namespace WiFiQrGenerator.Services;

/// <summary>
/// QR code generation helpers.
/// </summary>
public static class QrService
{
    /// <summary>
    /// Generates a raw QR code (no text) and returns PNG bytes.
    /// </summary>
    public static byte[] GeneratePng(string text, int pixelsPerModule = 20)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Builds a PNG that contains the QR code plus SSID/password text below it.
    /// Must be called on the UI thread (uses WPF FormattedText).
    /// </summary>
    public static byte[] GeneratePngWithText(
        string qrText,
        string ssid,
        string password,
        string networkLabel,
        string passwordLabel,
        int qrSizePx = 400,
        int pixelsPerModule = 20)
    {
        // 1. Raw QR bytes.
        byte[] qrPng = GeneratePng(qrText, pixelsPerModule);

        // 2. Load the QR PNG into a BitmapImage (ready for drawing).
        var qrImage = new BitmapImage();
        qrImage.BeginInit();
        qrImage.CacheOption = BitmapCacheOption.OnLoad;
        qrImage.StreamSource = new MemoryStream(qrPng);
        qrImage.EndInit();
        qrImage.Freeze();

        // 3. Layout constants.
        const int padding = 30;
        const int fontSize = 20;
        const int lineSpacing = 30;
        int textAreaHeight = lineSpacing * 2 + 20;
        int totalWidth = qrSizePx + padding * 2;
        int totalHeight = qrSizePx + textAreaHeight + padding * 2;

        // 4. Compose on a DrawingVisual.
        var visual = new DrawingVisual();
        double pixelsPerDip = VisualTreeHelper.GetDpi(visual).PixelsPerDip;

        using (var ctx = visual.RenderOpen())
        {
            // White background.
            ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, totalWidth, totalHeight));

            // QR image.
            ctx.DrawImage(qrImage, new Rect(padding, padding, qrSizePx, qrSizePx));

            // Labels.
            var typeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            var textBrush = Brushes.Black;

            var ssidText = new FormattedText(
                $"{networkLabel}: {ssid}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                textBrush,
                pixelsPerDip);

            var passwordText = new FormattedText(
                $"{passwordLabel}: {password}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                textBrush,
                pixelsPerDip);

            double textY = padding + qrSizePx + 20;
            ctx.DrawText(ssidText, new Point(padding, textY));
            ctx.DrawText(passwordText, new Point(padding, textY + lineSpacing));
        }

        // 5. Render to bitmap and encode as PNG.
        var rtb = new RenderTargetBitmap(
            totalWidth, totalHeight,
            96, 96,
            PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds the standard Wi-Fi QR payload string with proper escaping.
    /// </summary>
    public static string BuildWifiPayload(string ssid, string password, string authType)
    {
        return $"WIFI:T:{authType};S:{Escape(ssid)};P:{Escape(password)};;";
    }

    /// <summary>
    /// Escapes special characters as required by the Wi-Fi QR format.
    /// </summary>
    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace(":", "\\:")
            .Replace("\"", "\\\"");
    }
}