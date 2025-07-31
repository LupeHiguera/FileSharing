using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using UserManagementService.Models;
using UserManagementService.Repository;
using UserManagementService.Services;

namespace UserManagementService.Controller;

[ApiController]
[Route("api/users")]
[Authorize]
public class AuthenticatedUserController : ControllerBase
{
    private readonly IAzureAdAuthService _azureAdAuthService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthenticatedUserController> _logger;

    public AuthenticatedUserController(
        IAzureAdAuthService azureAdAuthService,
        IUserRepository userRepository,
        ILogger<AuthenticatedUserController> logger)
    {
        _azureAdAuthService = azureAdAuthService;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        try
        {
            var user = await _azureAdAuthService.GetOrCreateUserFromClaimsAsync(User);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return BadRequest("Unable to retrieve user information");
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<User>> UpdateCurrentUser([FromBody] User updatedUser)
    {
        try
        {
            var currentUser = await _azureAdAuthService.GetOrCreateUserFromClaimsAsync(User);
            
            // Only allow updating certain fields
            currentUser.Username = updatedUser.Username ?? currentUser.Username;
            currentUser.UserProfile.StorageQuota = updatedUser.UserProfile?.StorageQuota ?? currentUser.UserProfile.StorageQuota;
            currentUser.UpdatedDate = DateTime.UtcNow;

            await _userRepository.UpdateUser(currentUser);
            
            return Ok(currentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current user");
            return BadRequest("Unable to update user information");
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfile>> GetCurrentUserProfile()
    {
        try
        {
            var user = await _azureAdAuthService.GetOrCreateUserFromClaimsAsync(User);
            return Ok(user.UserProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user profile");
            return BadRequest("Unable to retrieve user profile");
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Update last login time
            var user = await _azureAdAuthService.GetOrCreateUserFromClaimsAsync(User);
            await _azureAdAuthService.UpdateUserLastLoginAsync(user.id);
            
            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Ok(new { message = "Logout completed" }); // Still return OK even if update fails
        }
    }
}