using System.Net.Http.Json;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class IdentityTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task CanRegisterAndLoginUser()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Act - Register
        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword,
            FirstName = "Test",
            LastName = "User"
        };

        var registerResponse = await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        // Assert - Registration
        Assert.True(registerResponse.IsSuccessStatusCode, $"Registration failed: {await registerResponse.Content.ReadAsStringAsync(cancellationToken)}");

        // Act - Login
        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);

        // Assert - Login
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed: {await loginResponse.Content.ReadAsStringAsync(cancellationToken)}");
        
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.NotEmpty(authResponse.RefreshToken);
        Assert.Equal(testEmail, authResponse.Email);
        Assert.Equal("Test", authResponse.FirstName);
        Assert.Equal("User", authResponse.LastName);
    }

    [Fact]
    public async Task CanAccessProtectedEndpointWithValidToken()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Register and login
        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        // Act - Access protected endpoint
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        var weatherResponse = await httpClient.GetAsync("/weatherforecast", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, weatherResponse.StatusCode);
    }

    [Fact]
    public async Task CannotAccessProtectedEndpointWithoutToken()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var weatherResponse = await httpClient.GetAsync("/weatherforecast", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, weatherResponse.StatusCode);
    }

    [Fact]
    public async Task CanRefreshToken()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Register and login
        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        // Act - Refresh token
        var refreshRequest = new RefreshTokenRequest
        {
            Token = authResponse!.Token,
            RefreshToken = authResponse.RefreshToken
        };

        var refreshResponse = await httpClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        
        var newAuthResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(newAuthResponse);
        Assert.NotEmpty(newAuthResponse.Token);
        Assert.NotEmpty(newAuthResponse.RefreshToken);
        Assert.NotEqual(authResponse.Token, newAuthResponse.Token);
        Assert.NotEqual(authResponse.RefreshToken, newAuthResponse.RefreshToken);
    }
}
