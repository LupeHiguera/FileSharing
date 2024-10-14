using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/Admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminUserController> _logger;

    public AdminUserController(IUserRepository userRepository, ILogger<AdminUserController> logger)
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
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await _userRepository.DeleteUserAsync(id);
        return Ok();
    }
}