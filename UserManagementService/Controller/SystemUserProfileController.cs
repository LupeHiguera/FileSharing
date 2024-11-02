using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
using UserManagementService.Models;

[ApiController]
[Route("api/System/userProfiles")]
[Authorize(Roles = "System")]
public class SystemUserProfileController : ControllerBase
{
    private readonly IUserProfileRepository _userProfileRepository;
    
    public SystemUserProfileController(IUserRepository userRepository, IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }
    
    [HttpGet]
    public async Task<UserProfile> GetUserProfileById(string id)
    {
        return await _userProfileRepository.GetUserProfileByIdAsync(id);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUserProfile(UserProfile userProfile)
    {
        await _userProfileRepository.AddUserProfileAsync(userProfile);
        return Ok();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile(UserProfile userProfile)
    {
        await _userProfileRepository.UpdateUserProfileAsync(userProfile);
        return Ok();
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUserProfile(string id)
    {
        await _userProfileRepository.DeleteUserProfileAsync(id);
        return Ok();
    }
}