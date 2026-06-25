using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.SkiaSharp;

namespace Servitore.API.Services;

public interface IBarcodeService
{
    byte[] GenerateBarcode(string content);
    byte[] GenerateQrCode(string content);
}

public class BarcodeService : IBarcodeService
{
    public byte[] GenerateBarcode(string content)
    {
        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions { Height = 100, Width = 300, Margin = 10 }
        };

        using var bitmap = writer.Write(content);
        return EncodeToPng(bitmap);
    }

    public byte[] GenerateQrCode(string content)
    {
        var writer = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Height = 250, Width = 250, Margin = 1 }
        };

        using var bitmap = writer.Write(content);
        return EncodeToPng(bitmap);
    }

    private static byte[] EncodeToPng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
