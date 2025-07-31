using System.Security.Claims;
using UserManagementService.Models;

namespace UserManagementService.Services;

public interface IAzureAdAuthService
{
    Task<User> GetOrCreateUserFromClaimsAsync(ClaimsPrincipal claimsPrincipal);
    User MapClaimsToUser(ClaimsPrincipal claimsPrincipal);
    Task UpdateUserLastLoginAsync(string userId);
}