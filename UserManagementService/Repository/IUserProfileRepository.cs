namespace UserManagementService.Repository;
using UserManagementService.Models;

public interface IUserProfileRepository
{
    Task<UserProfile> GetUserProfileByIdAsync(string id);
    Task AddUserProfileAsync(UserProfile userProfile);
    Task UpdateUserProfileAsync(UserProfile userProfile);
    Task DeleteUserProfileAsync(string id);
}