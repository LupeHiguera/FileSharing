using System.Text.Json.Serialization;

namespace UserManagementService.Models;

public class UserProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    
    // Foreign key reference to the User
    public string UserId { get; set; }
}