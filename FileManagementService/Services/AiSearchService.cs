using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FileManagementService.Models;
using System.Text.Json;

namespace FileManagementService.Services;

public class AiSearchService : IAiSearchService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<AiSearchService> _logger;

    public AiSearchService(IConfiguration configuration, ILogger<AiSearchService> logger)
    {
        _logger = logger;
        
        var builder = Kernel.CreateBuilder();
        
        // Configure OpenAI (you can also use Azure OpenAI)
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.AddOpenAIChatCompletion(model, apiKey);
            _kernel = builder.Build();
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }
        else
        {
            _logger.LogWarning("OpenAI API key not configured. AI features will be disabled.");
            _kernel = builder.Build();
        }
    }

    public async Task<IEnumerable<FileMetadata>> SemanticSearchAsync(string query, IEnumerable<FileMetadata> files, int maxResults = 10)
    {
        try
        {
            if (_chatService == null || !files.Any())
            {
                // Fallback to basic text search
                return BasicTextSearch(query, files, maxResults);
            }

            var fileList = files.ToList();
            var searchPrompt = $@"
You are a file search assistant. Given a search query and a list of files, rank the files by relevance to the query.
Consider file names, descriptions, tags, content types, and AI summaries.

Search Query: ""{query}""

Files to search through:
{JsonSerializer.Serialize(fileList.Select(f => new 
{
    id = f.Id,
    fileName = f.FileName,
    description = f.Description,
    tags = f.Tags,
    contentType = f.ContentType,
    aiSummary = f.AiSummary,
    aiKeywords = f.AiKeywords
}).Take(50))} // Limit to prevent token overflow

Return only the IDs of the most relevant files in order of relevance (max {maxResults}), as a JSON array of strings.
If no files are relevant, return an empty array.
";

            var response = await _chatService.GetChatMessageContentAsync(searchPrompt);
            var resultText = response.Content?.Trim() ?? "";

            // Parse the AI response to get file IDs
            var relevantFileIds = ParseAiResponse(resultText);
            
            // Return files in the order specified by AI
            var relevantFiles = relevantFileIds
                .Select(id => fileList.FirstOrDefault(f => f.Id == id))
                .Where(f => f != null)
                .Cast<FileMetadata>()
                .ToList();

            // If AI didn't return enough results, supplement with basic search
            if (relevantFiles.Count < maxResults)
            {
                var basicResults = BasicTextSearch(query, files.Except(relevantFiles), maxResults - relevantFiles.Count);
                relevantFiles.AddRange(basicResults);
            }

            _logger.LogInformation("Semantic search completed for query: {Query}, found {Count} results", query, relevantFiles.Count);
            return relevantFiles.Take(maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in semantic search for query: {Query}", query);
            // Fallback to basic search
            return BasicTextSearch(query, files, maxResults);
        }
    }

    public async Task<string> GenerateFileSummaryAsync(string fileName, string contentType, Stream? fileContent = null)
    {
        try
        {
            if (_chatService == null)
            {
                return GenerateBasicSummary(fileName, contentType);
            }

            var prompt = $@"
Generate a concise summary for a file with the following details:
- File Name: {fileName}
- Content Type: {contentType}

Based on the file name and type, provide a 1-2 sentence summary describing what this file likely contains or what it might be used for.
Be specific and helpful for search purposes.
";

            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var summary = response.Content?.Trim() ?? GenerateBasicSummary(fileName, contentType);

            _logger.LogInformation("AI summary generated for file: {FileName}", fileName);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for file: {FileName}", fileName);
            return GenerateBasicSummary(fileName, contentType);
        }
    }

    public async Task<List<string>> ExtractKeywordsAsync(string fileName, string description, string contentType)
    {
        try
        {
            if (_chatService == null)
            {
                return ExtractBasicKeywords(fileName, description, contentType);
            }

            var prompt = $@"
Extract relevant keywords for search purposes from the following file information:
- File Name: {fileName}
- Description: {description ?? "No description"}
- Content Type: {contentType}

Return 5-10 relevant keywords that would help users find this file.
Return the keywords as a JSON array of strings.
Focus on: file type, purpose, domain, technology, format, and content themes.
";

            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var keywordsText = response.Content?.Trim() ?? "";

            var keywords = ParseKeywordsResponse(keywordsText);
            
            if (!keywords.Any())
            {
                keywords = ExtractBasicKeywords(fileName, description, contentType);
            }

            _logger.LogInformation("AI keywords extracted for file: {FileName}, found {Count} keywords", fileName, keywords.Count);
            return keywords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting AI keywords for file: {FileName}", fileName);
            return ExtractBasicKeywords(fileName, description, contentType);
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetSmartRecommendationsAsync(string userId, IEnumerable<FileMetadata> userFiles, IEnumerable<FileMetadata> allFiles, int maxResults = 10)
    {
        try
        {
            if (_chatService == null)
            {
                return GetBasicRecommendations(userFiles, allFiles, maxResults);
            }

            var userFilesList = userFiles.ToList();
            var allFilesList = allFiles.Where(f => f.OwnerId != userId && f.IsPublic).ToList();

            if (!userFilesList.Any() || !allFilesList.Any())
            {
                return GetBasicRecommendations(userFiles, allFiles, maxResults);
            }

            var prompt = $@"
You are a file recommendation system. Based on a user's file collection, recommend similar or complementary files from the public collection.

User's Files (showing patterns and interests):
{JsonSerializer.Serialize(userFilesList.Select(f => new 
{
    fileName = f.FileName,
    contentType = f.ContentType,
    tags = f.Tags,
    description = f.Description
}).Take(20))}

Available Public Files to recommend from:
{JsonSerializer.Serialize(allFilesList.Select(f => new 
{
    id = f.Id,
    fileName = f.FileName,
    contentType = f.ContentType,
    tags = f.Tags,
    description = f.Description,
    popularityScore = f.PopularityScore
}).Take(100))}

Analyze the user's file patterns and recommend {maxResults} files that would be most relevant or useful.
Consider: similar content types, complementary technologies, related domains, and popular files in areas of interest.

Return only the IDs of recommended files as a JSON array of strings, ordered by relevance.
";

            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var resultText = response.Content?.Trim() ?? "";

            var recommendedIds = ParseAiResponse(resultText);
            var recommendations = recommendedIds
                .Select(id => allFilesList.FirstOrDefault(f => f.Id == id))
                .Where(f => f != null)
                .Cast<FileMetadata>()
                .ToList();

            // Supplement with basic recommendations if needed
            if (recommendations.Count < maxResults)
            {
                var basicRecs = GetBasicRecommendations(userFiles, allFiles.Except(recommendations), maxResults - recommendations.Count);
                recommendations.AddRange(basicRecs);
            }

            _logger.LogInformation("Smart recommendations generated for user: {UserId}, found {Count} recommendations", userId, recommendations.Count);
            return recommendations.Take(maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart recommendations for user: {UserId}", userId);
            return GetBasicRecommendations(userFiles, allFiles, maxResults);
        }
    }

    public async Task<string> GenerateSearchSuggestionsAsync(string partialQuery)
    {
        try
        {
            if (_chatService == null || string.IsNullOrWhiteSpace(partialQuery))
            {
                return string.Empty;
            }

            var prompt = $@"
Given this partial search query: ""{partialQuery}""

Suggest 3-5 complete search queries that a user might be looking for when searching files.
Consider common file types, development terms, document types, and business contexts.

Return suggestions as a JSON array of strings.
Each suggestion should be a complete, useful search query.
";

            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var suggestions = response.Content?.Trim() ?? "";

            _logger.LogInformation("Search suggestions generated for query: {Query}", partialQuery);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating search suggestions for query: {Query}", partialQuery);
            return string.Empty;
        }
    }

    #region Fallback Methods

    private IEnumerable<FileMetadata> BasicTextSearch(string query, IEnumerable<FileMetadata> files, int maxResults)
    {
        var queryLower = query.ToLower();
        return files
            .Where(f => 
                f.FileName.ToLower().Contains(queryLower) ||
                (f.Description?.ToLower().Contains(queryLower) ?? false) ||
                f.Tags.Any(t => t.ToLower().Contains(queryLower)) ||
                (f.AiSummary?.ToLower().Contains(queryLower) ?? false))
            .OrderByDescending(f => f.PopularityScore)
            .Take(maxResults);
    }

    private string GenerateBasicSummary(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        var summaries = new Dictionary<string, string>
        {
            { ".pdf", "PDF document that may contain reports, documentation, or reference material." },
            { ".docx", "Microsoft Word document containing text, formatting, and possibly images." },
            { ".xlsx", "Excel spreadsheet with data, calculations, or charts." },
            { ".pptx", "PowerPoint presentation with slides and visual content." },
            { ".jpg", "Image file that may contain photos, diagrams, or visual content." },
            { ".png", "Image file with graphics, screenshots, or illustrations." },
            { ".mp4", "Video file containing multimedia content." },
            { ".zip", "Compressed archive containing multiple files or folders." }
        };

        return summaries.GetValueOrDefault(extension, $"File of type {contentType} that may be useful for reference or work purposes.");
    }

    private List<string> ExtractBasicKeywords(string fileName, string description, string contentType)
    {
        var keywords = new List<string>();
        
        // Add file extension
        var extension = Path.GetExtension(fileName).Replace(".", "").ToLower();
        if (!string.IsNullOrEmpty(extension))
            keywords.Add(extension);

        // Add content type category
        if (contentType.StartsWith("image/"))
            keywords.Add("image");
        else if (contentType.StartsWith("video/"))
            keywords.Add("video");
        else if (contentType.StartsWith("audio/"))
            keywords.Add("audio");
        else if (contentType.Contains("pdf"))
            keywords.Add("document");
        else if (contentType.Contains("text"))
            keywords.Add("text");

        // Extract words from filename
        var fileNameWords = Path.GetFileNameWithoutExtension(fileName)
            .Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Take(5);
        keywords.AddRange(fileNameWords);

        // Extract words from description
        if (!string.IsNullOrEmpty(description))
        {
            var descWords = description
                .Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Take(5);
            keywords.AddRange(descWords);
        }

        return keywords.Distinct().ToList();
    }

    private IEnumerable<FileMetadata> GetBasicRecommendations(IEnumerable<FileMetadata> userFiles, IEnumerable<FileMetadata> allFiles, int maxResults)
    {
        var userFilesList = userFiles.ToList();
        
        // Get user's most common content types and tags
        var commonContentTypes = userFilesList
            .GroupBy(f => f.ContentType)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key);

        var commonTags = userFilesList
            .SelectMany(f => f.Tags)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key);

        return allFiles
            .Where(f => f.IsPublic)
            .Select(f => new { File = f, Score = CalculateBasicRecommendationScore(f, commonContentTypes, commonTags) })
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.File);
    }

    private double CalculateBasicRecommendationScore(FileMetadata file, IEnumerable<string> userContentTypes, IEnumerable<string> userTags)
    {
        double score = file.PopularityScore;

        if (userContentTypes.Contains(file.ContentType))
            score += 10;

        var matchingTags = file.Tags.Intersect(userTags).Count();
        score += matchingTags * 5;

        return score;
    }

    #endregion

    #region Response Parsing

    private List<string> ParseAiResponse(string response)
    {
        try
        {
            // Try to parse as JSON array
            if (response.Trim().StartsWith("[") && response.Trim().EndsWith("]"))
            {
                return JsonSerializer.Deserialize<List<string>>(response) ?? new List<string>();
            }

            // Try to extract JSON array from response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<List<string>>(jsonContent) ?? new List<string>();
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response: {Response}", response);
            return new List<string>();
        }
    }

    private List<string> ParseKeywordsResponse(string response)
    {
        try
        {
            var keywords = ParseAiResponse(response);
            
            // Clean and validate keywords
            return keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim().ToLower())
                .Where(k => k.Length > 1 && k.Length < 50)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing keywords response: {Response}", response);
            return new List<string>();
        }
    }

    #endregion
}