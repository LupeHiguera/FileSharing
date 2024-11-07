using System.Text.Json.Serialization;

namespace UserManagementService.Models;

public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; }
    [JsonPropertyName("password")]
    public string Password { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("userProfile")]
    public UserProfile UserProfile { get; set; }
}