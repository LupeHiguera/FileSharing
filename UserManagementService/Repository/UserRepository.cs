using Microsoft.EntityFrameworkCore;
using UserManagementService.CosmosDb;
using UserManagementService.Models;
using UserManagementService.Data;

namespace UserManagementService.Repository;
public class UserRepository(CosmosDbService<User> dbService, ILogger<UserRepository> logger) : IUserRepository
{
    private readonly CosmosDbService<User> _dbService;
    
    
    public async Task<IEnumerable<User>> GetUserAsync(string query)
    {
        var userList = new List<User>();
        try
        {
            userList = await dbService.GetItemsAsync(query);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in GetUserAsync");
        }
        finally
        {
            logger.LogInformation("GetUserAsync called");
        }
        return userList;
    }
    public async Task<User> GetUserByIdAsync(string id)
    {
        var user = new User();
        try
        {
            user = await userContext.Users.FindAsync(id) ?? throw new Exception("User not found");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in GetUserByIdAsync");
        }
        finally
        {
            logger.LogInformation("GetUserByIdAsync called");
        }
        return user;
    }
    
    public async Task AddUserAsync(User user)
    {
        await userContext.Users.AddAsync(user);
        await userContext.SaveChangesAsync();
    }
    
    public async Task UpdateUserAsync(User user)
    {
        userContext.Users.Update(user);
        await userContext.SaveChangesAsync();
    }
    
    public async Task DeleteUserAsync(string id)
    {
        var user = await GetUserByIdAsync(id);
        userContext.Users.Remove(user);
        await userContext.SaveChangesAsync();
    }
}