using Microsoft.AspNetCore.Mvc;
using UserManagementService.CosmosDb;
using UserManagementService.Models;

namespace UserManagementService.Repository;

public class UserRepository(CosmosDbService<User> dbService): IUserRepository
{
    public async Task<User> GetUserById(string id)
    {
        return await dbService.GetItemAsync(id, id);
    }

    public async Task<IEnumerable<User>> GetUsers(string query)
    {
        return await dbService.GetItemsAsync(query);
    }

    public async Task<IActionResult> AddUser(User user)
    {
        await dbService.AddItemAsync(user, user.id);
        return new OkResult();
    }

    public async Task<IActionResult> UpdateUser(User user)
    {
        await dbService.UpdateUser(user.id, user, user.id);
        return new OkResult();
    }

    public async Task<IActionResult> DeleteUser(string id)
    {
        await dbService.DeleteItem(id, id);
        return new OkResult();
    }
}