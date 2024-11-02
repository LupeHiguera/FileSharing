using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.CosmosDb;
using UserManagementService.Models;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/system/users")]
//[Authorize(Roles = "System")]
public class SystemUserController : ControllerBase
{
    private readonly CosmosDbService<User> _dbService;
    public SystemUserController(CosmosDbService<User> dbService)
    {
        _dbService = dbService;
    }

    [HttpGet]
    public async Task<IEnumerable<User>> GetUsers(string query)
    {
        return await _dbService.GetItemsAsync(query);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] User user)
    {
        await _dbService.GetItemAsync(user.Id, user.Id);
        return Ok("User was added successfully");
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUser(User user)
    {
        await _dbService.UpdateUser(user.Id, user, user.Id);
        return Ok("User was updated successfully");
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await _dbService.DeleteItem(id, id);
        return Ok("User was deleted successfully");
    }
}