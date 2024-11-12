using Microsoft.AspNetCore.Mvc;

namespace UserManagementService.Repository;
using UserManagementService.Models;


public interface IUserRepository
{
    public Task<User> GetUserById(string id);
    public Task<IEnumerable<User>> GetUsers(string query);
    public Task<IActionResult> AddUser(User user);
    public Task<IActionResult> UpdateUser(User user);
    public Task<IActionResult> DeleteUser(string id);
}