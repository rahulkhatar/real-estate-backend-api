using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Exceptions;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/images")]
[Authorize(Roles = "Admin")]
public class ImagesController(IImageStorageService imageStorageService) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "projects", "properties", "units", "layouts", "agents",
    };

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string folder = "units")
    {
        if (file is null || file.Length == 0)
            throw new ValidationAppException(new Dictionary<string, string[]> { ["file"] = ["A file is required."] });

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationAppException(new Dictionary<string, string[]> { ["file"] = ["File exceeds the 10 MB limit."] });

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Unsupported content type '{file.ContentType}'. Allowed: {string.Join(", ", AllowedContentTypes)}"]
            });

        if (!AllowedFolders.Contains(folder))
            folder = "units";

        await using var stream = file.OpenReadStream();
        var image = await imageStorageService.UploadAsync(stream, file.FileName, file.ContentType, folder.ToLowerInvariant());

        return Ok(image);
    }
}
