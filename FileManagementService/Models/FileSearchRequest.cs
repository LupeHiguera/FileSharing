using System.Text.Json.Serialization;

namespace FileManagementService.Models;

public class FileSearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("ownerId")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("includePublic")]
    public bool IncludePublic { get; set; } = true;

    [JsonPropertyName("includeShared")]
    public bool IncludeShared { get; set; } = true;

    [JsonPropertyName("dateFrom")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    public DateTime? DateTo { get; set; }

    [JsonPropertyName("maxFileSize")]
    public long? MaxFileSize { get; set; }

    [JsonPropertyName("minFileSize")]
    public long? MinFileSize { get; set; }

    [JsonPropertyName("sortBy")]
    public string SortBy { get; set; } = "createdDate"; // createdDate, fileName, fileSize, downloadCount, popularityScore

    [JsonPropertyName("sortDirection")]
    public string SortDirection { get; set; } = "desc"; // asc, desc

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    [JsonPropertyName("useAiSearch")]
    public bool UseAiSearch { get; set; } = false;
}