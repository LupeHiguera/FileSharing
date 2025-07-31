using System.Text.Json.Serialization;

namespace UserManagementService.Models;

public class UserProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("storageQuota")]
    public long StorageQuota { get; set; } = 500 * 1024 * 1024; // 500MB default
    
    [JsonPropertyName("usedStorage")]
    public long UsedStorage { get; set; } = 0;
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}