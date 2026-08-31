using RealEstate.Core.ValueObjects;

namespace RealEstate.Application.Interfaces;

public interface IImageStorageService
{
    /// <summary>Uploads a file under the given folder (e.g. "projects", "properties", "units", "layouts") and returns its public URL.</summary>
    Task<ImageAsset> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);

    /// <summary>Deletes a previously uploaded object given its public URL.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);
}
