namespace UserManagementService.Models;

public class User
{
    public string Id { get; set; }
    public string password { get; set; }
    
    // One to one mapping for UserProfile
    public UserProfile UserProfile { get; set; }
}