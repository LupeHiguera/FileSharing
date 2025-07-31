using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using UserManagementService.CosmosDb;
using UserManagementService.Repository;
using UserManagementService.Services;
using User = UserManagementService.Models.User;

var builder = WebApplication.CreateBuilder(args);

// Add Azure AD authentication
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// Add CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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
builder.Services.AddScoped<IAzureAdAuthService, AzureAdAuthService>();

// Add Swagger with Azure AD authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "User Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();