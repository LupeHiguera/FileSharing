using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;

[ApiController]
[Route("api/System/userProfiles")]
[Authorize(Roles = "System")]
public class SystemUserProfileController(IUserProfileRepository userProfileRepository) : ControllerBase
{
    [HttpGet]
    public async Task<UserProfile> GetUserProfileById(string id)
    {
        return await userProfileRepository.GetUserProfileById(id);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUserProfile(UserProfile userProfile)
    {
        await userProfileRepository.AddUserProfile(userProfile);
        return Ok("User Profile was added successfully");
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile(UserProfile userProfile)
    {
        await userProfileRepository.UpdateUserProfile(userProfile);
        return Ok("User Profile was updated successfully");
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUserProfile(string id)
    {
        await userProfileRepository.DeleteUserProfile(id);
        return Ok("User Profile was deleted successfully");
    }
}