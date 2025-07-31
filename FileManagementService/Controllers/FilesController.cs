using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using FileManagementService.Models;
using FileManagementService.Services;
using FileManagementService.Repository;
using System.Security.Claims;

namespace FileManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<FilesController> _logger;
    private readonly string _defaultContainer = "files";

    public FilesController(
        IBlobStorageService blobStorageService, 
        IFileRepository fileRepository,
        ILogger<FilesController> logger)
    {
        _blobStorageService = blobStorageService;
        _fileRepository = fileRepository;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileMetadata>> UploadFile([FromForm] IFormFile file, [FromForm] string? description, [FromForm] string? tags, [FromForm] bool isPublic = false)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();

            // Generate unique blob name
            var blobName = await _blobStorageService.GenerateUniqueFileNameAsync(_defaultContainer, file.FileName);

            // Upload to blob storage
            using var stream = file.OpenReadStream();
            var blobUrl = await _blobStorageService.UploadFileAsync(_defaultContainer, blobName, stream, file.ContentType);

            // Create file metadata
            var fileMetadata = new FileMetadata
            {
                FileName = file.FileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                OwnerId = userId,
                OwnerEmail = userEmail,
                BlobName = blobName,
                ContainerName = _defaultContainer,
                IsPublic = isPublic,
                Description = description,
                Tags = !string.IsNullOrEmpty(tags) ? tags.Split(',').Select(t => t.Trim()).ToList() : new List<string>()
            };

            // Save metadata to database
            var savedMetadata = await _fileRepository.CreateFileAsync(fileMetadata);

            _logger.LogInformation("File uploaded successfully: {FileName} by user {UserId}", file.FileName, userId);
            return Ok(savedMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, "Error uploading file");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetMyFiles()
    {
        try
        {
            var userId = GetCurrentUserId();
            var files = await _fileRepository.GetFilesByOwnerAsync(userId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Error retrieving files");
        }
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetPublicFiles()
    {
        try
        {
            var files = await _fileRepository.GetPublicFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public files");
            return StatusCode(500, "Error retrieving public files");
        }
    }

    [HttpGet("shared")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetSharedFiles()
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            var files = await _fileRepository.GetSharedFilesAsync(userEmail);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared files for user {UserEmail}", GetCurrentUserEmail());
            return StatusCode(500, "Error retrieving shared files");
        }
    }

    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetPopularFiles([FromQuery] int limit = 10)
    {
        try
        {
            var files = await _fileRepository.GetPopularFilesAsync(limit);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular files");
            return StatusCode(500, "Error retrieving popular files");
        }
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetRecommendedFiles([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var files = await _fileRepository.GetRecommendedFilesAsync(userId, limit);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended files for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Error retrieving recommended files");
        }
    }

    [HttpPost("search")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> SearchFiles([FromBody] FileSearchRequest searchRequest)
    {
        try
        {
            var userId = GetCurrentUserId();
            var files = await _fileRepository.SearchFilesAsync(searchRequest, userId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Error searching files");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileMetadata>> GetFile(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();
            
            var file = await _fileRepository.GetFileByIdAsync(id);
            if (file == null)
            {
                return NotFound("File not found");
            }

            // Check access permissions
            if (!CanAccessFile(file, userId, userEmail))
            {
                return Forbid("Access denied");
            }

            // Update view count
            await _fileRepository.UpdateFileStatsAsync(id, incrementView: true);

            return Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FileId}", id);
            return StatusCode(500, "Error retrieving file");
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();
            
            var file = await _fileRepository.GetFileByIdAsync(id);
            if (file == null)
            {
                return NotFound("File not found");
            }

            // Check access permissions
            if (!CanAccessFile(file, userId, userEmail))
            {
                return Forbid("Access denied");
            }

            // Get file stream from blob storage
            var fileStream = await _blobStorageService.DownloadFileAsync(file.ContainerName, file.BlobName);

            // Update download count
            await _fileRepository.UpdateFileStatsAsync(id, incrementDownload: true);

            _logger.LogInformation("File downloaded: {FileId} by user {UserId}", id, userId);

            // Return file stream with proper headers
            return File(fileStream, file.ContentType, file.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return StatusCode(500, "Error downloading file");
        }
    }

    [HttpGet("{id}/url")]
    public async Task<ActionResult<string>> GetFileUrl(string id, [FromQuery] int expiryHours = 1)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();
            
            var file = await _fileRepository.GetFileByIdAsync(id);
            if (file == null)
            {
                return NotFound("File not found");
            }

            // Check access permissions
            if (!CanAccessFile(file, userId, userEmail))
            {
                return Forbid("Access denied");
            }

            var url = await _blobStorageService.GetFileUrlAsync(file.ContainerName, file.BlobName, TimeSpan.FromHours(expiryHours));
            
            return Ok(new { url, expiresAt = DateTime.UtcNow.AddHours(expiryHours) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL for {FileId}", id);
            return StatusCode(500, "Error generating file URL");
        }
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFile(string id, [FromBody] FileShareRequest shareRequest)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var file = await _fileRepository.GetFileByOwnerAndIdAsync(userId, id);
            if (file == null)
            {
                return NotFound("File not found or access denied");
            }

            file.IsShared = true;
            file.SharedWith.AddRange(shareRequest.ShareWithEmails.Except(file.SharedWith));
            
            if (shareRequest.ExpirationDate.HasValue)
            {
                file.ExpirationDate = shareRequest.ExpirationDate.Value;
            }

            await _fileRepository.UpdateFileAsync(file);

            _logger.LogInformation("File {FileId} shared with {EmailCount} users", id, shareRequest.ShareWithEmails.Count);
            return Ok(new { message = "File shared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing file {FileId}", id);
            return StatusCode(500, "Error sharing file");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FileMetadata>> UpdateFile(string id, [FromBody] FileMetadata updatedFile)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var file = await _fileRepository.GetFileByOwnerAndIdAsync(userId, id);
            if (file == null)
            {
                return NotFound("File not found or access denied");
            }

            // Update only allowed fields
            file.Description = updatedFile.Description;
            file.Tags = updatedFile.Tags;
            file.IsPublic = updatedFile.IsPublic;
            file.ExpirationDate = updatedFile.ExpirationDate;

            var result = await _fileRepository.UpdateFileAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FileId}", id);
            return StatusCode(500, "Error updating file");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var file = await _fileRepository.GetFileByOwnerAndIdAsync(userId, id);
            if (file == null)
            {
                return NotFound("File not found or access denied");
            }

            // Delete from blob storage
            await _blobStorageService.DeleteFileAsync(file.ContainerName, file.BlobName);

            // Delete metadata from database
            await _fileRepository.DeleteFileAsync(id, userId);

            _logger.LogInformation("File deleted: {FileId} by user {UserId}", id, userId);
            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return StatusCode(500, "Error deleting file");
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
        // Owner can always access
        if (file.OwnerId == userId)
            return true;

        // Public files can be accessed by anyone
        if (file.IsPublic)
            return true;

        // Shared files can be accessed by shared users
        if (file.IsShared && file.SharedWith.Contains(userEmail))
            return true;

        return false;
    }
}