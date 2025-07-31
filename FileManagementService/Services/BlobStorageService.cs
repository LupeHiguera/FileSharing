using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FileManagementService.Models;

namespace FileManagementService.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _defaultContainer;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
        _defaultContainer = configuration["BlobStorage:DefaultContainer"] ?? "files";
    }

    public async Task<string> UploadFileAsync(string containerName, string blobName, Stream fileStream, string contentType)
    {
        try
        {
            var containerClient = await GetOrCreateContainerAsync(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Conditions = null
            });

            _logger.LogInformation("File uploaded successfully: {BlobName} to container {ContainerName}", blobName, containerName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {BlobName} to container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File {blobName} not found in container {containerName}");
            }

            var response = await blobClient.DownloadStreamingAsync();
            _logger.LogInformation("File downloaded successfully: {BlobName} from container {ContainerName}", blobName, containerName);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();
            
            if (response.Value)
            {
                _logger.LogInformation("File deleted successfully: {BlobName} from container {ContainerName}", blobName, containerName);
            }
            else
            {
                _logger.LogWarning("File not found for deletion: {BlobName} in container {ContainerName}", blobName, containerName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists {BlobName} in container {ContainerName}", blobName, containerName);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string containerName, string blobName, TimeSpan? expiry = null)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File {blobName} not found in container {containerName}");
            }

            // Generate SAS token for secure access
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromHours(1))
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating file URL for {BlobName} in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<long> GetFileSizeAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync();
            return properties.Value.ContentLength;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size for {BlobName} in container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<string> GenerateUniqueFileNameAsync(string containerName, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var sanitizedName = SanitizeFileName(nameWithoutExtension);
        
        var uniqueFileName = $"{sanitizedName}_{Guid.NewGuid()}{extension}";
        
        // Ensure the filename is unique in the container
        while (await FileExistsAsync(containerName, uniqueFileName))
        {
            uniqueFileName = $"{sanitizedName}_{Guid.NewGuid()}{extension}";
        }

        return uniqueFileName;
    }

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        return containerClient;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}