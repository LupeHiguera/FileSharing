using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using FileManagementService.Models;
using FileManagementService.Repository;
using FileManagementService.Services;
using System.Security.Claims;

namespace FileManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly IAiSearchService _aiSearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IFileRepository fileRepository,
        IAiSearchService aiSearchService,
        ILogger<SearchController> logger)
    {
        _fileRepository = fileRepository;
        _aiSearchService = aiSearchService;
        _logger = logger;
    }

    [HttpPost("semantic")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> SemanticSearch([FromBody] SemanticSearchRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Get all accessible files for the user
            var searchRequest = new FileSearchRequest
            {
                Query = request.Query,
                IncludePublic = request.IncludePublic,
                IncludeShared = request.IncludeShared,
                PageSize = 100 // Get more files for AI to analyze
            };

            var allAccessibleFiles = await _fileRepository.SearchFilesAsync(searchRequest, userId);
            
            // Use AI to perform semantic search
            var semanticResults = await _aiSearchService.SemanticSearchAsync(
                request.Query, 
                allAccessibleFiles, 
                request.MaxResults);

            _logger.LogInformation("Semantic search performed for user {UserId} with query: {Query}", userId, request.Query);
            return Ok(semanticResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", request.Query);
            return StatusCode(500, "Error performing semantic search");
        }
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<SearchSuggestionsResponse>> GetSearchSuggestions([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new SearchSuggestionsResponse { Suggestions = new List<string>() });
            }

            // Get AI-powered suggestions
            var aiSuggestions = await _aiSearchService.GenerateSearchSuggestionsAsync(query);
            
            // Parse AI response
            var suggestions = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(aiSuggestions))
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(aiSuggestions);
                    if (parsed != null)
                        suggestions.AddRange(parsed);
                }
            }
            catch
            {
                // Fallback to basic suggestions if AI parsing fails
                suggestions = GetBasicSuggestions(query);
            }

            // Add basic suggestions if AI didn't provide enough
            if (suggestions.Count < 3)
            {
                var basicSuggestions = GetBasicSuggestions(query);
                suggestions.AddRange(basicSuggestions.Except(suggestions));
            }

            var response = new SearchSuggestionsResponse
            {
                Suggestions = suggestions.Take(5).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return Ok(new SearchSuggestionsResponse { Suggestions = GetBasicSuggestions(query) });
        }
    }

    [HttpPost("analyze-file")]
    public async Task<ActionResult<FileAnalysisResult>> AnalyzeFile([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            var fileName = file.FileName;
            var contentType = file.ContentType;

            // Generate AI summary and keywords
            using var stream = file.OpenReadStream();
            var summary = await _aiSearchService.GenerateFileSummaryAsync(fileName, contentType, stream);
            var keywords = await _aiSearchService.ExtractKeywordsAsync(fileName, summary, contentType);

            var result = new FileAnalysisResult
            {
                FileName = fileName,
                ContentType = contentType,
                FileSize = file.Length,
                AiSummary = summary,
                SuggestedKeywords = keywords,
                SuggestedTags = keywords.Take(5).ToList() // Top 5 as tags
            };

            _logger.LogInformation("File analyzed: {FileName} for user {UserId}", fileName, GetCurrentUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file: {FileName}", file?.FileName);
            return StatusCode(500, "Error analyzing file");
        }
    }

    [HttpGet("similar/{fileId}")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> FindSimilarFiles(string fileId, [FromQuery] int maxResults = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();
            
            var targetFile = await _fileRepository.GetFileByIdAsync(fileId);
            if (targetFile == null)
            {
                return NotFound("File not found");
            }

            // Check access permissions
            if (!CanAccessFile(targetFile, userId, userEmail))
            {
                return Forbid("Access denied");
            }

            // Get all accessible files
            var searchRequest = new FileSearchRequest
            {
                IncludePublic = true,
                IncludeShared = true,
                PageSize = 100
            };

            var allFiles = await _fileRepository.SearchFilesAsync(searchRequest, userId);
            var otherFiles = allFiles.Where(f => f.Id != fileId);

            // Use AI to find similar files based on content, tags, and metadata
            var searchQuery = $"files similar to {targetFile.FileName} {targetFile.Description} {string.Join(" ", targetFile.Tags)}";
            var similarFiles = await _aiSearchService.SemanticSearchAsync(searchQuery, otherFiles, maxResults);

            _logger.LogInformation("Similar files found for {FileId}: {Count} results", fileId, similarFiles.Count());
            return Ok(similarFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar files for {FileId}", fileId);
            return StatusCode(500, "Error finding similar files");
        }
    }

    [HttpGet("trending-searches")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TrendingSearch>>> GetTrendingSearches([FromQuery] int limit = 10)
    {
        try
        {
            // This would typically come from a search analytics database
            // For now, we'll return some sample trending searches based on popular file types
            var files = await _fileRepository.GetPopularFilesAsync(50);
            
            var trendingSearches = files
                .SelectMany(f => f.Tags.Concat(new[] { GetFileCategory(f.ContentType) }))
                .GroupBy(term => term.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => new TrendingSearch 
                { 
                    Term = g.Key, 
                    Count = g.Count(),
                    Category = GetSearchCategory(g.Key)
                });

            return Ok(trendingSearches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending searches");
            return StatusCode(500, "Error retrieving trending searches");
        }
    }

    [HttpPost("save-search")]
    public async Task<IActionResult> SaveSearch([FromBody] SaveSearchRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // This would typically save to a user searches database
            // For now, we'll just log it for analytics
            _logger.LogInformation("Search saved for user {UserId}: {SearchTerm}", userId, request.SearchTerm);
            
            return Ok(new { message = "Search saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving search for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Error saving search");
        }
    }

    private string GetCurrentUserId()
    {
        return User.GetObjectId() ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found");
    }

    private string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? throw new UnauthorizedAccessException("User email not found");
    }

    private bool CanAccessFile(FileMetadata file, string userId, string userEmail)
    {
        return file.OwnerId == userId || file.IsPublic || (file.IsShared && file.SharedWith.Contains(userEmail));
    }

    private List<string> GetBasicSuggestions(string query)
    {
        var queryLower = query.ToLower();
        var suggestions = new List<string>();

        // File type suggestions
        var fileTypes = new[] { "pdf", "doc", "image", "video", "excel", "powerpoint", "text", "archive" };
        suggestions.AddRange(fileTypes.Where(t => t.StartsWith(queryLower)).Select(t => $"{t} files"));

        // Common search terms
        var commonTerms = new[] { "document", "report", "presentation", "spreadsheet", "contract", "invoice", "template", "backup" };
        suggestions.AddRange(commonTerms.Where(t => t.StartsWith(queryLower)));

        // Action-based suggestions
        if (queryLower.Length > 2)
        {
            suggestions.Add($"files containing '{query}'");
            suggestions.Add($"recent {query}");
            suggestions.Add($"shared {query}");
        }

        return suggestions.Distinct().Take(5).ToList();
    }

    private string GetFileCategory(string contentType)
    {
        return contentType.ToLower() switch
        {
            var ct when ct.StartsWith("image/") => "images",
            var ct when ct.StartsWith("video/") => "videos",
            var ct when ct.StartsWith("audio/") => "audio",
            var ct when ct.Contains("pdf") => "documents",
            var ct when ct.Contains("word") => "documents",
            var ct when ct.Contains("excel") => "spreadsheets",
            var ct when ct.Contains("powerpoint") => "presentations",
            _ => "files"
        };
    }

    private string GetSearchCategory(string term)
    {
        var categories = new Dictionary<string, string>
        {
            { "documents", "File Types" },
            { "images", "File Types" },
            { "videos", "File Types" },
            { "pdf", "File Types" },
            { "excel", "File Types" },
            { "contract", "Business" },
            { "report", "Business" },
            { "invoice", "Business" },
            { "template", "Templates" },
            { "backup", "System" }
        };

        return categories.GetValueOrDefault(term.ToLower(), "General");
    }
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public bool IncludePublic { get; set; } = true;
    public bool IncludeShared { get; set; } = true;
    public int MaxResults { get; set; } = 10;
}

public class SearchSuggestionsResponse
{
    public List<string> Suggestions { get; set; } = new();
}

public class FileAnalysisResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string AiSummary { get; set; } = string.Empty;
    public List<string> SuggestedKeywords { get; set; } = new();
    public List<string> SuggestedTags { get; set; } = new();
}

public class TrendingSearch
{
    public string Term { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class SaveSearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public int ResultCount { get; set; }
}