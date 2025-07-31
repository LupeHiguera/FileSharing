using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FileManagementService.Models;
using FileManagementService.Repository;

namespace FileManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(IFileRepository fileRepository, ILogger<LeaderboardController> logger)
    {
        _fileRepository = fileRepository;
        _logger = logger;
    }

    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileLeaderboardEntry>>> GetPopularFiles([FromQuery] int limit = 10)
    {
        try
        {
            var popularFiles = await _fileRepository.GetPopularFilesAsync(limit);
            
            var leaderboard = popularFiles.Select((file, index) => new FileLeaderboardEntry
            {
                Rank = index + 1,
                FileId = file.Id,
                FileName = file.FileName,
                OwnerEmail = file.OwnerEmail,
                DownloadCount = file.DownloadCount,
                ViewCount = file.ViewCount,
                PopularityScore = file.PopularityScore,
                ContentType = file.ContentType,
                Tags = file.Tags,
                CreatedDate = file.CreatedDate
            });

            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular files leaderboard");
            return StatusCode(500, "Error retrieving leaderboard");
        }
    }

    [HttpGet("most-downloaded")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileLeaderboardEntry>>> GetMostDownloadedFiles([FromQuery] int limit = 10)
    {
        try
        {
            var files = await _fileRepository.GetPublicFilesAsync();
            
            var topDownloaded = files
                .OrderByDescending(f => f.DownloadCount)
                .ThenByDescending(f => f.CreatedDate)
                .Take(limit)
                .Select((file, index) => new FileLeaderboardEntry
                {
                    Rank = index + 1,
                    FileId = file.Id,
                    FileName = file.FileName,
                    OwnerEmail = file.OwnerEmail,
                    DownloadCount = file.DownloadCount,
                    ViewCount = file.ViewCount,
                    PopularityScore = file.PopularityScore,
                    ContentType = file.ContentType,
                    Tags = file.Tags,
                    CreatedDate = file.CreatedDate
                });

            return Ok(topDownloaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting most downloaded files");
            return StatusCode(500, "Error retrieving most downloaded files");
        }
    }

    [HttpGet("recent-popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileLeaderboardEntry>>> GetRecentPopularFiles([FromQuery] int days = 7, [FromQuery] int limit = 10)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var files = await _fileRepository.GetPublicFilesAsync();
            
            var recentPopular = files
                .Where(f => f.CreatedDate >= cutoffDate || (f.LastAccessedDate.HasValue && f.LastAccessedDate >= cutoffDate))
                .OrderByDescending(f => f.PopularityScore)
                .ThenByDescending(f => f.DownloadCount)
                .Take(limit)
                .Select((file, index) => new FileLeaderboardEntry
                {
                    Rank = index + 1,
                    FileId = file.Id,
                    FileName = file.FileName,
                    OwnerEmail = file.OwnerEmail,
                    DownloadCount = file.DownloadCount,
                    ViewCount = file.ViewCount,
                    PopularityScore = file.PopularityScore,
                    ContentType = file.ContentType,
                    Tags = file.Tags,
                    CreatedDate = file.CreatedDate,
                    LastAccessedDate = file.LastAccessedDate
                });

            return Ok(recentPopular);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent popular files for {Days} days", days);
            return StatusCode(500, "Error retrieving recent popular files");
        }
    }

    [HttpGet("by-category")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CategoryLeaderboard>>> GetLeaderboardByCategory([FromQuery] int limit = 5)
    {
        try
        {
            var files = await _fileRepository.GetPublicFilesAsync();
            
            var categoryLeaderboards = files
                .GroupBy(f => GetFileCategory(f.ContentType))
                .Select(g => new CategoryLeaderboard
                {
                    Category = g.Key,
                    TopFiles = g.OrderByDescending(f => f.PopularityScore)
                               .Take(limit)
                               .Select((file, index) => new FileLeaderboardEntry
                               {
                                   Rank = index + 1,
                                   FileId = file.Id,
                                   FileName = file.FileName,
                                   OwnerEmail = file.OwnerEmail,
                                   DownloadCount = file.DownloadCount,
                                   ViewCount = file.ViewCount,
                                   PopularityScore = file.PopularityScore,
                                   ContentType = file.ContentType,
                                   Tags = file.Tags,
                                   CreatedDate = file.CreatedDate
                               })
                               .ToList()
                })
                .Where(c => c.TopFiles.Any())
                .OrderByDescending(c => c.TopFiles.Sum(f => f.PopularityScore));

            return Ok(categoryLeaderboards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard by category");
            return StatusCode(500, "Error retrieving category leaderboard");
        }
    }

    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileLeaderboardEntry>>> GetTrendingFiles([FromQuery] int hours = 24, [FromQuery] int limit = 10)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-hours);
            var files = await _fileRepository.GetPublicFilesAsync();
            
            // Calculate trending score based on recent activity
            var trendingFiles = files
                .Where(f => f.LastAccessedDate.HasValue && f.LastAccessedDate >= cutoffDate)
                .Select(f => new { File = f, TrendingScore = CalculateTrendingScore(f, cutoffDate) })
                .OrderByDescending(x => x.TrendingScore)
                .Take(limit)
                .Select((item, index) => new FileLeaderboardEntry
                {
                    Rank = index + 1,
                    FileId = item.File.Id,
                    FileName = item.File.FileName,
                    OwnerEmail = item.File.OwnerEmail,
                    DownloadCount = item.File.DownloadCount,
                    ViewCount = item.File.ViewCount,
                    PopularityScore = item.File.PopularityScore,
                    ContentType = item.File.ContentType,
                    Tags = item.File.Tags,
                    CreatedDate = item.File.CreatedDate,
                    LastAccessedDate = item.File.LastAccessedDate,
                    TrendingScore = item.TrendingScore
                });

            return Ok(trendingFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending files for {Hours} hours", hours);
            return StatusCode(500, "Error retrieving trending files");
        }
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<ActionResult<LeaderboardStats>> GetLeaderboardStats()
    {
        try
        {
            var files = await _fileRepository.GetPublicFilesAsync();
            var filesList = files.ToList();

            var stats = new LeaderboardStats
            {
                TotalPublicFiles = filesList.Count,
                TotalDownloads = filesList.Sum(f => f.DownloadCount),
                TotalViews = filesList.Sum(f => f.ViewCount),
                AveragePopularityScore = filesList.Any() ? filesList.Average(f => f.PopularityScore) : 0,
                MostPopularContentType = filesList
                    .GroupBy(f => GetFileCategory(f.ContentType))
                    .OrderByDescending(g => g.Sum(f => f.PopularityScore))
                    .FirstOrDefault()?.Key ?? "Unknown",
                TopTags = filesList
                    .SelectMany(f => f.Tags)
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new TagStats { Tag = g.Key, Count = g.Count() })
                    .ToList(),
                FilesCreatedToday = filesList.Count(f => f.CreatedDate.Date == DateTime.UtcNow.Date),
                FilesAccessedToday = filesList.Count(f => f.LastAccessedDate.HasValue && f.LastAccessedDate.Value.Date == DateTime.UtcNow.Date)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard stats");
            return StatusCode(500, "Error retrieving leaderboard stats");
        }
    }

    private string GetFileCategory(string contentType)
    {
        return contentType.ToLower() switch
        {
            var ct when ct.StartsWith("image/") => "Images",
            var ct when ct.StartsWith("video/") => "Videos",
            var ct when ct.StartsWith("audio/") => "Audio",
            var ct when ct.Contains("pdf") => "Documents",
            var ct when ct.Contains("word") || ct.Contains("document") => "Documents",
            var ct when ct.Contains("excel") || ct.Contains("spreadsheet") => "Spreadsheets",
            var ct when ct.Contains("powerpoint") || ct.Contains("presentation") => "Presentations",
            var ct when ct.StartsWith("text/") => "Text Files",
            var ct when ct.Contains("zip") || ct.Contains("archive") => "Archives",
            var ct when ct.Contains("json") || ct.Contains("xml") => "Data Files",
            _ => "Other"
        };
    }

    private double CalculateTrendingScore(FileMetadata file, DateTime cutoffDate)
    {
        if (!file.LastAccessedDate.HasValue || file.LastAccessedDate < cutoffDate)
            return 0;

        var hoursSinceCutoff = (DateTime.UtcNow - cutoffDate).TotalHours;
        var hoursSinceAccess = (DateTime.UtcNow - file.LastAccessedDate.Value).TotalHours;
        
        // Recent activity gets higher score
        var recencyMultiplier = Math.Max(0, (hoursSinceCutoff - hoursSinceAccess) / hoursSinceCutoff);
        
        // Combine with base popularity score
        return file.PopularityScore * recencyMultiplier * 2;
    }
}

public class FileLeaderboardEntry
{
    public int Rank { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public int ViewCount { get; set; }
    public double PopularityScore { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? LastAccessedDate { get; set; }
    public double TrendingScore { get; set; }
}

public class CategoryLeaderboard
{
    public string Category { get; set; } = string.Empty;
    public List<FileLeaderboardEntry> TopFiles { get; set; } = new();
}

public class LeaderboardStats
{
    public int TotalPublicFiles { get; set; }
    public int TotalDownloads { get; set; }
    public int TotalViews { get; set; }
    public double AveragePopularityScore { get; set; }
    public string MostPopularContentType { get; set; } = string.Empty;
    public List<TagStats> TopTags { get; set; } = new();
    public int FilesCreatedToday { get; set; }
    public int FilesAccessedToday { get; set; }
}

public class TagStats
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
}