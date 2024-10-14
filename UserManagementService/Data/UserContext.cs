namespace UserManagementService.Data;

using Microsoft.EntityFrameworkCore;
using Models;

public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
        
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    
    
}