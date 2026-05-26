namespace SafeZone.Server.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType = null);
    Task<Stream?> DownloadAsync(string blobId);
    Task<bool> DeleteAsync(string blobId);
    string GetPublicUrl(string blobId);
}
