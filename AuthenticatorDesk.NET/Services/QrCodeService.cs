using QRCoder;
using AuthenticatorDesk.Localization;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace AuthenticatorDesk.Services;

public static class QrCodeService
{
    public static string DecodeFile(string filePath)
    {
        using var source = Image.FromFile(filePath);
        using var bitmap = new Bitmap(source);
        return Decode(bitmap);
    }

    public static string Decode(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var reader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.QR_CODE]
            }
        };

        var result = reader.Decode(bitmap);
        if (result is null || string.IsNullOrWhiteSpace(result.Text))
        {
            throw new System.FormatException(
                L.Get("service.qr.error.notDetected"));
        }

        return result.Text.Trim();
    }

    public static Bitmap Create(string content, int pixelsPerModule = 8)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                L.Get("service.qr.error.contentRequired"),
                nameof(content));
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            content,
            QRCodeGenerator.ECCLevel.Q,
            forceUtf8: true,
            utf8BOM: false);
        using var qrCode = new QRCode(data);
        return qrCode.GetGraphic(
            Math.Clamp(pixelsPerModule, 2, 24),
            Color.FromArgb(20, 24, 38),
            Color.White,
            drawQuietZones: true);
    }
}
