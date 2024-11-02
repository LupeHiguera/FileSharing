using Microsoft.EntityFrameworkCore;
using UserManagementService.Data;
using UserManagementService.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<UserContext>(options =>
{
    options.UseCosmos(
        builder.Configuration["CosmoDB:Endpoint"] ?? "default_endpoint",
        builder.Configuration["CosmoDB:Key"] ?? "default_key",
        builder.Configuration["CosmoDB:DatabaseId"] ?? "default_database");
});
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


