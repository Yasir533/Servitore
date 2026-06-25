using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BarcodeController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;

    public BarcodeController(IBarcodeService barcodeService)
    {
        _barcodeService = barcodeService;
    }

    [HttpGet("barcode/{content}")]
    public IActionResult GetBarcode(string content)
    {
        try
        {
            var bytes = _barcodeService.GenerateBarcode(content);
            return File(bytes, "image/png");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("qr/{content}")]
    public IActionResult GetQrCode(string content)
    {
        try
        {
            var bytes = _barcodeService.GenerateQrCode(content);
            return File(bytes, "image/png");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
