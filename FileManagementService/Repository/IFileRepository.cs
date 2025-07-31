using FileManagementService.Models;

namespace FileManagementService.Repository;

public interface IFileRepository
{
    Task<FileMetadata> CreateFileAsync(FileMetadata fileMetadata);
    Task<FileMetadata?> GetFileByIdAsync(string id);
    Task<FileMetadata?> GetFileByOwnerAndIdAsync(string ownerId, string id);
    Task<IEnumerable<FileMetadata>> GetFilesByOwnerAsync(string ownerId);
    Task<IEnumerable<FileMetadata>> GetPublicFilesAsync();
    Task<IEnumerable<FileMetadata>> GetSharedFilesAsync(string userEmail);
    Task<IEnumerable<FileMetadata>> SearchFilesAsync(FileSearchRequest searchRequest, string currentUserId);
    Task<FileMetadata> UpdateFileAsync(FileMetadata fileMetadata);
    Task<bool> DeleteFileAsync(string id, string ownerId);
    Task<IEnumerable<FileMetadata>> GetPopularFilesAsync(int limit = 10);
    Task<IEnumerable<FileMetadata>> GetRecommendedFilesAsync(string userId, int limit = 10);
    Task UpdateFileStatsAsync(string fileId, bool incrementDownload = false, bool incrementView = false);
}