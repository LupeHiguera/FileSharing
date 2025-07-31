using System.Security.Claims;
using Microsoft.Identity.Web;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Services;

public class AzureAdAuthService : IAzureAdAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AzureAdAuthService> _logger;

    public AzureAdAuthService(IUserRepository userRepository, ILogger<AzureAdAuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserFromClaimsAsync(ClaimsPrincipal claimsPrincipal)
    {
        var azureObjectId = claimsPrincipal.GetObjectId();
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ??
                   claimsPrincipal.FindFirst("preferred_username")?.Value;

        if (string.IsNullOrEmpty(azureObjectId) || string.IsNullOrEmpty(email))
        {
            throw new ArgumentException("Required claims (object ID or email) are missing");
        }

        try
        {
            // Try to find user by Azure AD Object ID first
            var existingUsers = await _userRepository.GetUsers("");
            var existingUser = existingUsers.FirstOrDefault(u => u.AzureAdObjectId == azureObjectId);

            if (existingUser != null)
            {
                // Update user information from current claims
                UpdateUserFromClaims(existingUser, claimsPrincipal);
                existingUser.LastLoginDate = DateTime.UtcNow;
                existingUser.UpdatedDate = DateTime.UtcNow;
                
                await _userRepository.UpdateUser(existingUser);
                return existingUser;
            }

            // Create new user if not found
            var newUser = MapClaimsToUser(claimsPrincipal);
            newUser.id = Guid.NewGuid().ToString();
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.LastLoginDate = DateTime.UtcNow;
            
            // Create default user profile
            newUser.UserProfile = new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                UserId = newUser.id,
                StorageQuota = 500 * 1024 * 1024, // 500MB default
                UsedStorage = 0,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _userRepository.AddUser(newUser);
            _logger.LogInformation("Created new user from Azure AD: {Email}", email);
            
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating user from Azure AD claims for email: {Email}", email);
            throw;
        }
    }

    public User MapClaimsToUser(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ??
                   claimsPrincipal.FindFirst("preferred_username")?.Value;
        
        var user = new User
        {
            AzureAdObjectId = claimsPrincipal.GetObjectId(),
            Email = email ?? string.Empty,
            Username = email?.Split('@')[0] ?? string.Empty,
            DisplayName = claimsPrincipal.FindFirst("name")?.Value,
            GivenName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value,
            Surname = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value,
            JobTitle = claimsPrincipal.FindFirst("jobTitle")?.Value,
            Department = claimsPrincipal.FindFirst("department")?.Value,
            TenantId = claimsPrincipal.GetTenantId(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        return user;
    }

    public async Task UpdateUserLastLoginAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user != null)
            {
                user.LastLoginDate = DateTime.UtcNow;
                user.UpdatedDate = DateTime.UtcNow;
                await _userRepository.UpdateUser(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
        }
    }

    private void UpdateUserFromClaims(User user, ClaimsPrincipal claimsPrincipal)
    {
        user.DisplayName = claimsPrincipal.FindFirst("name")?.Value ?? user.DisplayName;
        user.GivenName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value ?? user.GivenName;
        user.Surname = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value ?? user.Surname;
        user.JobTitle = claimsPrincipal.FindFirst("jobTitle")?.Value ?? user.JobTitle;
        user.Department = claimsPrincipal.FindFirst("department")?.Value ?? user.Department;
        
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ??
                   claimsPrincipal.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            user.Email = email;
        }
    }
}