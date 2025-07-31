using FileManagementService.Models;

namespace FileManagementService.Services;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(string containerName, string blobName, Stream fileStream, string contentType);
    Task<Stream> DownloadFileAsync(string containerName, string blobName);
    Task<bool> DeleteFileAsync(string containerName, string blobName);
    Task<bool> FileExistsAsync(string containerName, string blobName);
    Task<string> GetFileUrlAsync(string containerName, string blobName, TimeSpan? expiry = null);
    Task<long> GetFileSizeAsync(string containerName, string blobName);
    Task<string> GenerateUniqueFileNameAsync(string containerName, string originalFileName);
}