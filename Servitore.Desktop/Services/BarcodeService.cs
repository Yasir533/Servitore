  using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Servitore.Desktop.Services;

/// <summary>
/// Wraps ZXing.Net for decoding barcodes/QR codes from image data.
/// Uses SkiaSharp instead of System.Drawing so it works cross-platform and on .NET 8.
/// </summary>
public class BarcodeService
{
    private readonly ZXing.SkiaSharp.BarcodeReader _reader = new()
    {
        AutoRotate = true,
        Options = new DecodingOptions { TryInverted = true }
    };

    /// <summary>Decodes a barcode/QR code from an SKBitmap (e.g. from a camera frame).</summary>
    public string? DecodeFromBitmap(SKBitmap bitmap)
    {
        var result = _reader.Decode(bitmap);
        return result?.Text;
    }

    /// <summary>Decodes a barcode/QR code from raw PNG/JPEG bytes.</summary>
    public string? DecodeFromBytes(byte[] imageBytes)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap is null) return null;
        return DecodeFromBitmap(bitmap);
    }
}
