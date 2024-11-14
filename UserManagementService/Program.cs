using Microsoft.Azure.Cosmos;
using UserManagementService.CosmosDb;
using UserManagementService.Repository;
using User = UserManagementService.Models.User;

var builder = WebApplication.CreateBuilder(args);

// Register CosmosDbService for User
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<CosmosClient>>();
    var endpoint = configuration["CosmoDB:Endpoint"] ?? "default_endpoint";
    var key = configuration["CosmoDB:Key"] ?? "default_key";
    var databaseId = configuration["CosmoDB:DatabaseId"] ?? "default_database";
    var containerId = configuration["CosmoDB:UserContainerId"] ?? "Users";
    
    return new CosmosDbService<User>(endpoint, key, databaseId, containerId, logger);
});

builder.Services.AddControllers();
builder.Services.AddScoped<IUserRepository, UserRepository>();


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