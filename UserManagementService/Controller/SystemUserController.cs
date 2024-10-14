using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/system/users")]
[Authorize(Roles = "System")]
public class SystemUserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SystemUserController> _logger;

    public SystemUserController(IUserRepository userRepository, ILogger<SystemUserController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<IEnumerable<User>> GetUsers()
    {
        return await _userRepository.GetUserAsync();
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUser(User user)
    {
        await _userRepository.AddUserAsync(user);
        return Ok();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUser(User user)
    {
        await _userRepository.UpdateUserAsync(user);
        return Ok();
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await _userRepository.DeleteUserAsync(id);
        return Ok();
    }
}