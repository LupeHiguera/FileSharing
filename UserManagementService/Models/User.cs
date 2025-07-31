using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace UserManagementService.Models;

public class User
{
    [Required]
    [JsonPropertyName("id")]
    public string id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("userProfile")]
    public UserProfile UserProfile { get; set; } = new UserProfile();
    
    [JsonPropertyName("azureAdObjectId")]
    public string? AzureAdObjectId { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }
    
    [JsonPropertyName("surname")]
    public string? Surname { get; set; }
    
    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }
    
    [JsonPropertyName("department")]
    public string? Department { get; set; }
    
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("lastLoginDate")]
    public DateTime? LastLoginDate { get; set; }
    
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}