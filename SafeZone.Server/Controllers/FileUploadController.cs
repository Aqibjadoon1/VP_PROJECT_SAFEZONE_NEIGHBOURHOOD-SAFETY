using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeZone.Server.Services;

namespace SafeZone.Server.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IBlobStorageService _blobStorage;

    public FileUploadController(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".mp4", ".webm" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { success = false, message = $"File type '{ext}' is not allowed." });

        // Sanitize filename to prevent path traversal
        var safeFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return BadRequest(new { success = false, message = "Invalid file name." });

        await using var stream = file.OpenReadStream();
        var blobId = await _blobStorage.UploadAsync(safeFileName, stream, file.ContentType);

        return Ok(new
        {
            success = true,
            message = "File uploaded.",
            blobId,
            url = _blobStorage.GetPublicUrl(blobId)
        });
    }
}
