using FileManagementService.Models;

namespace FileManagementService.Services;

public interface IAiSearchService
{
    Task<IEnumerable<FileMetadata>> SemanticSearchAsync(string query, IEnumerable<FileMetadata> files, int maxResults = 10);
    Task<string> GenerateFileSummaryAsync(string fileName, string contentType, Stream? fileContent = null);
    Task<List<string>> ExtractKeywordsAsync(string fileName, string description, string contentType);
    Task<IEnumerable<FileMetadata>> GetSmartRecommendationsAsync(string userId, IEnumerable<FileMetadata> userFiles, IEnumerable<FileMetadata> allFiles, int maxResults = 10);
    Task<string> GenerateSearchSuggestionsAsync(string partialQuery);
}