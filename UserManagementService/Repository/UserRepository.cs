using Microsoft.EntityFrameworkCore;
using UserManagementService.Models;
using UserManagementService.Data;

namespace UserManagementService.Repository;
public class UserRepository : IUserRepository
{
    private readonly UserContext _userContext;
    private readonly ILogger<UserRepository> _logger;
    
    public UserRepository(UserContext userContext, ILogger<UserRepository> logger)
    {
        _userContext = userContext;
        _logger = logger;
    }
    
    public async Task<IEnumerable<User>> GetUserAsync()
    {
        var userList = new List<User>();
        try
        {
            userList = await _userContext.Users.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetUserAsync");
        }
        finally
        {
            _logger.LogInformation("GetUserAsync called");
        }
        return userList;
    }
    
    public async Task<User> GetUserByIdAsync(string id)
    {
        var user = new User();
        try
        {
            user = await _userContext.Users.FindAsync(id) ?? throw new Exception("User not found");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetUserByIdAsync");
        }
        finally
        {
            _logger.LogInformation("GetUserByIdAsync called");
        }
        return user;
    }
    
    public async Task AddUserAsync(User user)
    {
        await _userContext.Users.AddAsync(user);
        await _userContext.SaveChangesAsync();
    }
    
    public async Task UpdateUserAsync(User user)
    {
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }
    
    public async Task DeleteUserAsync(string id)
    {
        var user = await GetUserByIdAsync(id);
        _userContext.Users.Remove(user);
        await _userContext.SaveChangesAsync();
    }
}