using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/system/users")]
[Authorize(Roles = "System")]
public class SystemUserController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<User>> GetUsers(string query)
    {
        return await userRepository.GetUsers(query);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] User user)
    {
        await userRepository.AddUser(user);
        return Ok("User was added successfully");
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUser(User user)
    {
        await userRepository.UpdateUser(user);
        return Ok("User was updated successfully");
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await userRepository.DeleteUser(id);
        return Ok("User was deleted successfully");
    }
}