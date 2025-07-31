using System.Text.Json.Serialization;

namespace FileManagementService.Models;

public class FileUploadRequest
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; } = false;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();

    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
}

public class FileShareRequest
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("shareWithEmails")]
    public List<string> ShareWithEmails { get; set; } = new List<string>();

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
}