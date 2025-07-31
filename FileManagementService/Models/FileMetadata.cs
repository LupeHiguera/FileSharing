using System.Text.Json.Serialization;

namespace FileManagementService.Models;

public class FileMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("ownerEmail")]
    public string OwnerEmail { get; set; } = string.Empty;

    [JsonPropertyName("blobName")]
    public string BlobName { get; set; } = string.Empty;

    [JsonPropertyName("containerName")]
    public string ContainerName { get; set; } = string.Empty;

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; } = false;

    [JsonPropertyName("isShared")]
    public bool IsShared { get; set; } = false;

    [JsonPropertyName("sharedWith")]
    public List<string> SharedWith { get; set; } = new List<string>();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("downloadCount")]
    public int DownloadCount { get; set; } = 0;

    [JsonPropertyName("viewCount")]
    public int ViewCount { get; set; } = 0;

    [JsonPropertyName("lastAccessedDate")]
    public DateTime? LastAccessedDate { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; } = false;

    [JsonPropertyName("aiSummary")]
    public string? AiSummary { get; set; }

    [JsonPropertyName("aiKeywords")]
    public List<string> AiKeywords { get; set; } = new List<string>();

    [JsonPropertyName("popularityScore")]
    public double PopularityScore { get; set; } = 0.0;
}