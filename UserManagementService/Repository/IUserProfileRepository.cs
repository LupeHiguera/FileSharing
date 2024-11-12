using Microsoft.AspNetCore.Mvc;
using UserManagementService.Models;

namespace UserManagementService.Repository;

public interface IUserProfileRepository
{
    public Task<UserProfile> GetUserProfileById(string id);
    public Task<IActionResult> AddUserProfile(UserProfile userProfile);
    public Task<IActionResult> UpdateUserProfile(UserProfile userProfile);
    public Task<IActionResult> DeleteUserProfile(string id);
}