using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/Admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUserController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<User>> GetUsers(string query)
    {
        return await userRepository.GetUsers(query);
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(User user)
    {
        await userRepository.AddUser(user);
        return Ok("User was added successfully");
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await userRepository.DeleteUser(id);
        return Ok("User was deleted successfully");
    }
}