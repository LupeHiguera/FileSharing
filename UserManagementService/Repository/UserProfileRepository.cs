namespace UserManagementService.Repository;
using UserManagementService.Models;
using UserManagementService.Data;

public class UserProfileRepository(UserContext userContext, ILogger<UserRepository> userLogger) : IUserProfileRepository
{
    public async Task<UserProfile> GetUserProfileByIdAsync(string id)
    {
        var userProfile = new UserProfile();
        try
        {
            userProfile = await userContext.UserProfiles.FindAsync(id) ?? throw new Exception("User Profile not found");
        }
        catch (Exception e)
        {
            userLogger.LogError(e, "Error in GetUserProfileByIdAsync");
        }
        finally
        {
            userLogger.LogInformation("GetUserProfileByIdAsync called");
        }
        return userProfile;
    }
    
    public async Task AddUserProfileAsync(UserProfile userProfile)
    {
        await userContext.UserProfiles.AddAsync(userProfile);
        await userContext.SaveChangesAsync();
    }
    
    public async Task UpdateUserProfileAsync(UserProfile userProfile)
    {
        userContext.UserProfiles.Update(userProfile);
        await userContext.SaveChangesAsync();
    }
    
    public async Task DeleteUserProfileAsync(string id)
    {
        var userProfile = await GetUserProfileByIdAsync(id);
        userContext.UserProfiles.Remove(userProfile);
        await userContext.SaveChangesAsync();
    }
}