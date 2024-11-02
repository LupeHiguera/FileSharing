using Microsoft.Azure.Cosmos;
using UserManagementService.CosmosDb;
using UserManagementService.Models;
using User = UserManagementService.Models.User;

var builder = WebApplication.CreateBuilder(args);

// Register CosmosDbService for UserProfile and User
void RegisterCosmosDbService<T>(string containerId) where T : class
{
    builder.Services.AddSingleton(serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosClient>>();
        var endpoint = configuration["CosmoDB:Endpoint"] ?? "default_endpoint";
        var key = configuration["CosmoDB:Key"] ?? "default_key";
        var databaseId = configuration["CosmoDB:DatabaseId"] ?? "default_database";
        
        return new CosmosDbService<T>(endpoint, key, databaseId, containerId, logger);
    });
}

// Register services for User and UserProfile with respective container IDs
RegisterCosmosDbService<User>(builder.Configuration["CosmoDB:UserContainerId"] ?? "default_user_container");
RegisterCosmosDbService<UserProfile>(builder.Configuration["CosmoDB:UserProfileContainerId"] ?? "default_userprofile_container");

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();