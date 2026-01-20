using BudgetTrackerApp.ApiService.Data;
using BudgetTrackerApp.ApiService.Models;
using BudgetTrackerApp.ApiService.Services;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Configure PostgreSQL with Aspire
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecureKeyHereMinimum32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BudgetTrackerApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BudgetTrackerApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register TokenService
builder.Services.AddScoped<ITokenService, TokenService>();

// Configure CORS for React SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173", // Vite default port
            "https://localhost:3000",
            "https://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("ReactApp");

app.UseAuthentication();
app.UseAuthorization();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization(); // Require authentication for this endpoint

// Authentication endpoints
app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.BadRequest(new { errors = result.Errors });
    }

    return Results.Ok(new { message = "User registered successfully" });
})
.WithName("Register");

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ApplicationDbContext dbContext) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        return Results.Unauthorized();
    }

    var roles = await userManager.GetRolesAsync(user);
    var accessToken = tokenService.GenerateAccessToken(user, roles);
    var refreshToken = tokenService.GenerateRefreshToken();

    // Store refresh token in database
    var refreshTokenEntity = new RefreshToken
    {
        UserId = user.Id,
        Token = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    dbContext.RefreshTokens.Add(refreshTokenEntity);
    await dbContext.SaveChangesAsync();

    var response = new AuthResponse
    {
        Token = accessToken,
        RefreshToken = refreshToken,
        Expiration = DateTime.UtcNow.AddMinutes(
            double.Parse(builder.Configuration["Jwt:ExpiryInMinutes"] ?? "60")),
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    return Results.Ok(response);
})
.WithName("Login");

app.MapPost("/api/auth/refresh", async (
    RefreshTokenRequest request,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) =>
{
    var principal = tokenService.GetPrincipalFromExpiredToken(request.Token);
    if (principal == null)
    {
        return Results.Unauthorized();
    }

    var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId == null)
    {
        return Results.Unauthorized();
    }

    var storedRefreshToken = await dbContext.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

    if (storedRefreshToken == null || storedRefreshToken.IsRevoked || storedRefreshToken.IsUsed ||
        storedRefreshToken.ExpiresAt < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    // Mark the refresh token as used
    storedRefreshToken.IsUsed = true;
    await dbContext.SaveChangesAsync();

    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var roles = await userManager.GetRolesAsync(user);
    var newAccessToken = tokenService.GenerateAccessToken(user, roles);
    var newRefreshToken = tokenService.GenerateRefreshToken();

    // Store new refresh token
    var newRefreshTokenEntity = new RefreshToken
    {
        UserId = user.Id,
        Token = newRefreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    dbContext.RefreshTokens.Add(newRefreshTokenEntity);
    await dbContext.SaveChangesAsync();

    var response = new AuthResponse
    {
        Token = newAccessToken,
        RefreshToken = newRefreshToken,
        Expiration = DateTime.UtcNow.AddMinutes(
            double.Parse(builder.Configuration["Jwt:ExpiryInMinutes"] ?? "60")),
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    return Results.Ok(response);
})
.WithName("RefreshToken");

app.MapPost("/api/auth/logout", async (
    HttpContext httpContext,
    ApplicationDbContext dbContext) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId != null)
    {
        // Revoke all refresh tokens for the user
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync();
    }

    return Results.Ok(new { message = "Logged out successfully" });
})
.WithName("Logout")
.RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

