using UserManagementService.Models;

namespace UserManagementService.Repository;

public interface IUserRepository
{
    Task<IEnumerable<User>>GetUserAsync();
    Task<User> GetUserByIdAsync(string id);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(string id);
}