using Microsoft.Azure.Cosmos;
using FileManagementService.Models;
using System.Net;

namespace FileManagementService.Repository;

public class FileRepository : IFileRepository
{
    private readonly Container _container;
    private readonly ILogger<FileRepository> _logger;

    public FileRepository(CosmosClient cosmosClient, IConfiguration configuration, ILogger<FileRepository> logger)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "FileSharing";
        var containerName = configuration["CosmosDb:FileMetadataContainer"] ?? "FileMetadata";
        
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public async Task<FileMetadata> CreateFileAsync(FileMetadata fileMetadata)
    {
        try
        {
            fileMetadata.Id = Guid.NewGuid().ToString();
            fileMetadata.CreatedDate = DateTime.UtcNow;
            fileMetadata.UpdatedDate = DateTime.UtcNow;
            
            var response = await _container.CreateItemAsync(fileMetadata, new PartitionKey(fileMetadata.OwnerId));
            _logger.LogInformation("File metadata created: {FileId} for owner {OwnerId}", fileMetadata.Id, fileMetadata.OwnerId);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file metadata for owner {OwnerId}", fileMetadata.OwnerId);
            throw;
        }
    }

    public async Task<FileMetadata?> GetFileByIdAsync(string id)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);
            var results = await iterator.ReadNextAsync();

            return results.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file by ID: {FileId}", id);
            throw;
        }
    }

    public async Task<FileMetadata?> GetFileByOwnerAndIdAsync(string ownerId, string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<FileMetadata>(id, new PartitionKey(ownerId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FileId} for owner {OwnerId}", id, ownerId);
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetFilesByOwnerAsync(string ownerId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ownerId = @ownerId AND c.isArchived = false ORDER BY c.createdDate DESC")
                .WithParameter("@ownerId", ownerId);

            var files = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                files.AddRange(response);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for owner {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetPublicFilesAsync()
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.isPublic = true AND c.isArchived = false ORDER BY c.popularityScore DESC, c.createdDate DESC");

            var files = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                files.AddRange(response);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public files");
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetSharedFilesAsync(string userEmail)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.isShared = true AND ARRAY_CONTAINS(c.sharedWith, @userEmail) AND c.isArchived = false ORDER BY c.createdDate DESC")
                .WithParameter("@userEmail", userEmail);

            var files = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                files.AddRange(response);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared files for user {UserEmail}", userEmail);
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> SearchFilesAsync(FileSearchRequest searchRequest, string currentUserId)
    {
        try
        {
            var queryText = BuildSearchQuery(searchRequest, currentUserId);
            var query = new QueryDefinition(queryText);

            // Add parameters
            if (!string.IsNullOrEmpty(searchRequest.Query))
            {
                query = query.WithParameter("@searchQuery", $"%{searchRequest.Query.ToLower()}%");
            }
            
            query = query.WithParameter("@currentUserId", currentUserId);

            if (searchRequest.DateFrom.HasValue)
                query = query.WithParameter("@dateFrom", searchRequest.DateFrom.Value);
            
            if (searchRequest.DateTo.HasValue)
                query = query.WithParameter("@dateTo", searchRequest.DateTo.Value);
            
            if (searchRequest.MaxFileSize.HasValue)
                query = query.WithParameter("@maxFileSize", searchRequest.MaxFileSize.Value);
            
            if (searchRequest.MinFileSize.HasValue)
                query = query.WithParameter("@minFileSize", searchRequest.MinFileSize.Value);

            var files = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                files.AddRange(response);
            }

            // Apply pagination
            var skip = (searchRequest.Page - 1) * searchRequest.PageSize;
            return files.Skip(skip).Take(searchRequest.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files for user {UserId}", currentUserId);
            throw;
        }
    }

    public async Task<FileMetadata> UpdateFileAsync(FileMetadata fileMetadata)
    {
        try
        {
            fileMetadata.UpdatedDate = DateTime.UtcNow;
            
            var response = await _container.ReplaceItemAsync(fileMetadata, fileMetadata.Id, new PartitionKey(fileMetadata.OwnerId));
            _logger.LogInformation("File metadata updated: {FileId}", fileMetadata.Id);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file metadata: {FileId}", fileMetadata.Id);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string id, string ownerId)
    {
        try
        {
            await _container.DeleteItemAsync<FileMetadata>(id, new PartitionKey(ownerId));
            _logger.LogInformation("File metadata deleted: {FileId} for owner {OwnerId}", id, ownerId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} for owner {OwnerId}", id, ownerId);
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetPopularFilesAsync(int limit = 10)
    {
        try
        {
            var query = new QueryDefinition($"SELECT TOP {limit} * FROM c WHERE c.isPublic = true AND c.isArchived = false ORDER BY c.popularityScore DESC, c.downloadCount DESC");

            var files = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                files.AddRange(response);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular files");
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetRecommendedFilesAsync(string userId, int limit = 10)
    {
        try
        {
            // Simple recommendation based on user's most downloaded content types and tags
            var userFilesQuery = new QueryDefinition("SELECT * FROM c WHERE c.ownerId = @userId")
                .WithParameter("@userId", userId);

            var userFiles = new List<FileMetadata>();
            var iterator = _container.GetItemQueryIterator<FileMetadata>(userFilesQuery);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                userFiles.AddRange(response);
            }

            // Get most common content types and tags from user's files
            var commonContentTypes = userFiles
                .GroupBy(f => f.ContentType)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            var commonTags = userFiles
                .SelectMany(f => f.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            // Find recommendations based on content types and tags
            var recommendationQuery = new QueryDefinition($"SELECT TOP {limit} * FROM c WHERE c.isPublic = true AND c.ownerId != @userId AND c.isArchived = false ORDER BY c.popularityScore DESC")
                .WithParameter("@userId", userId);

            var recommendations = new List<FileMetadata>();
            var recIterator = _container.GetItemQueryIterator<FileMetadata>(recommendationQuery);

            while (recIterator.HasMoreResults)
            {
                var response = await recIterator.ReadNextAsync();
                recommendations.AddRange(response);
            }

            // Score and sort recommendations
            return recommendations
                .Select(f => new { File = f, Score = CalculateRecommendationScore(f, commonContentTypes, commonTags) })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.File);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended files for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateFileStatsAsync(string fileId, bool incrementDownload = false, bool incrementView = false)
    {
        try
        {
            // First get the file to update stats
            var file = await GetFileByIdAsync(fileId);
            if (file == null) return;

            if (incrementDownload)
            {
                file.DownloadCount++;
            }

            if (incrementView)
            {
                file.ViewCount++;
            }

            file.LastAccessedDate = DateTime.UtcNow;
            
            // Calculate popularity score based on downloads, views, and recency
            file.PopularityScore = CalculatePopularityScore(file);

            await _container.ReplaceItemAsync(file, file.Id, new PartitionKey(file.OwnerId));
            
            _logger.LogInformation("File stats updated for {FileId}: Downloads={Downloads}, Views={Views}", 
                fileId, file.DownloadCount, file.ViewCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file stats for {FileId}", fileId);
            throw;
        }
    }

    private string BuildSearchQuery(FileSearchRequest searchRequest, string currentUserId)
    {
        var conditions = new List<string>
        {
            "c.isArchived = false"
        };

        // Access control
        var accessConditions = new List<string>();
        
        if (searchRequest.IncludePublic)
            accessConditions.Add("c.isPublic = true");
        
        if (searchRequest.IncludeShared)
            accessConditions.Add($"(c.isShared = true AND ARRAY_CONTAINS(c.sharedWith, '{currentUserId}'))");
        
        // User's own files
        accessConditions.Add($"c.ownerId = '{currentUserId}'");

        if (accessConditions.Any())
            conditions.Add($"({string.Join(" OR ", accessConditions)})");

        // Search query
        if (!string.IsNullOrEmpty(searchRequest.Query))
        {
            conditions.Add("(CONTAINS(LOWER(c.fileName), @searchQuery) OR CONTAINS(LOWER(c.description), @searchQuery) OR CONTAINS(LOWER(c.aiSummary), @searchQuery))");
        }

        // Content type filter
        if (!string.IsNullOrEmpty(searchRequest.ContentType))
        {
            conditions.Add($"c.contentType = '{searchRequest.ContentType}'");
        }

        // Date filters
        if (searchRequest.DateFrom.HasValue)
            conditions.Add("c.createdDate >= @dateFrom");
        
        if (searchRequest.DateTo.HasValue)
            conditions.Add("c.createdDate <= @dateTo");

        // File size filters
        if (searchRequest.MinFileSize.HasValue)
            conditions.Add("c.fileSize >= @minFileSize");
        
        if (searchRequest.MaxFileSize.HasValue)
            conditions.Add("c.fileSize <= @maxFileSize");

        // Tags filter
        if (searchRequest.Tags.Any())
        {
            var tagConditions = searchRequest.Tags.Select(tag => $"ARRAY_CONTAINS(c.tags, '{tag}')");
            conditions.Add($"({string.Join(" OR ", tagConditions)})");
        }

        var whereClause = string.Join(" AND ", conditions);
        var orderBy = GetOrderByClause(searchRequest.SortBy, searchRequest.SortDirection);

        return $"SELECT * FROM c WHERE {whereClause} {orderBy}";
    }

    private string GetOrderByClause(string sortBy, string sortDirection)
    {
        var direction = sortDirection.ToUpper() == "ASC" ? "ASC" : "DESC";
        
        return sortBy.ToLower() switch
        {
            "filename" => $"ORDER BY c.fileName {direction}",
            "filesize" => $"ORDER BY c.fileSize {direction}",
            "downloadcount" => $"ORDER BY c.downloadCount {direction}",
            "popularityscore" => $"ORDER BY c.popularityScore {direction}",
            "createddate" or _ => $"ORDER BY c.createdDate {direction}"
        };
    }

    private double CalculatePopularityScore(FileMetadata file)
    {
        var daysSinceCreation = (DateTime.UtcNow - file.CreatedDate).TotalDays;
        var daysSinceLastAccess = file.LastAccessedDate.HasValue 
            ? (DateTime.UtcNow - file.LastAccessedDate.Value).TotalDays 
            : daysSinceCreation;

        // Weighted scoring: downloads (40%), views (20%), recency (40%)
        var downloadScore = Math.Log10(file.DownloadCount + 1) * 40;
        var viewScore = Math.Log10(file.ViewCount + 1) * 20;
        var recencyScore = Math.Max(0, 40 - (daysSinceLastAccess * 2)); // Decreases over time

        return downloadScore + viewScore + recencyScore;
    }

    private double CalculateRecommendationScore(FileMetadata file, List<string> userContentTypes, List<string> userTags)
    {
        double score = file.PopularityScore;

        // Boost score for matching content types
        if (userContentTypes.Contains(file.ContentType))
            score += 10;

        // Boost score for matching tags
        var matchingTags = file.Tags.Intersect(userTags).Count();
        score += matchingTags * 5;

        return score;
    }
}