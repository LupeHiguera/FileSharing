namespace UserManagementService.Models;

public class UserProfile
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    
    // Navigation for User
    public User User { get; set; }
}