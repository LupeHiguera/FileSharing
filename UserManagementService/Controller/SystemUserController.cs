using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.CosmosDb;
using UserManagementService.Models;
using UserManagementService.Repository;

namespace UserManagementService.Controller;
[ApiController]
[Route("api/system/users")]
[Authorize(Roles = "System")]
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
        var paritionKey = user.Id;

        try
        {
            await _dbService.GetItemAsync(user.Id, paritionKey);
            return Ok("User was added successfully");
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUser(User user)
    {
        var paritionKey = user.Id;
        await _dbService.UpdateUser(user.Id, user, paritionKey);
        return Ok();
    }
    
    [HttpDelete]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await _userRepository.DeleteUserAsync(id);
        return Ok();
    }
}