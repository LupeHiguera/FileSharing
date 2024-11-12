using Microsoft.AspNetCore.Mvc;
using UserManagementService.CosmosDb;
using UserManagementService.Models;

namespace UserManagementService.Repository;

public class UserProfileRepository(CosmosDbService<UserProfile> dbService): IUserProfileRepository
{
    public async Task<UserProfile> GetUserProfileById(string id)
    {
        return await dbService.GetItemAsync(id, id);
    }

    public async Task<IActionResult> AddUserProfile(UserProfile userProfile)
    {
        await dbService.AddItemAsync(userProfile, userProfile.Id);
        return new OkResult();
    }

    public async Task<IActionResult> UpdateUserProfile(UserProfile userProfile)
    {
        await dbService.UpdateUser(userProfile.Id, userProfile, userProfile.Id);
        return new OkResult();
    }

    public async Task<IActionResult> DeleteUserProfile(string id)
    {
        await dbService.DeleteItem(id, id);
        return new OkResult();
    }   
}