using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.CosmosDb;

namespace UserManagementService.Controller;
using UserManagementService.Models;

[ApiController]
[Route("api/System/userProfiles")]
[Authorize(Roles = "System")]
public class SystemUserProfileController(CosmosDbService<UserProfile> dbService) : ControllerBase
{
    [HttpGet]
    public async Task<UserProfile> GetUserProfileById(string id)
    {
        return await dbService.GetItemAsync(id, id);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUserProfile(UserProfile userProfile)
    {
        await dbService.AddItemAsync(userProfile, userProfile.Id);
        return Ok("User Profile was added successfully");
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile(UserProfile userProfile)
    {
        await dbService.UpdateUser(userProfile.Id, userProfile, userProfile.Id);
        return Ok("User Profile was updated successfully");
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUserProfile(string id)
    {
        await dbService.DeleteItem(id, id);
        return Ok("User Profile was deleted successfully");
    }
}