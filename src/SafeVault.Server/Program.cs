using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeVault.Server.Data;
using SafeVault.Server.Models;

var builder = WebApplication.CreateBuilder(args);


// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SafeVault.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    options.LoginPath = "/api/auth/login";
    options.AccessDeniedPath = "/api/auth/forbidden";
});
// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ITOnly", policy =>
    {
        policy.RequireClaim("Department", "IT");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(
    scope.ServiceProvider,
    app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI JSON: /openapi/v1.json
    app.MapOpenApi();

    // Swagger UI: /swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SafeVault API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();