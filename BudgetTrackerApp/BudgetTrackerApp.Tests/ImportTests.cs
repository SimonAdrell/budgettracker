using System.Net.Http.Json;
using System.Net.Http.Headers;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class ImportTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task CanCreateAccountAndRetrieveAccounts()
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
            ConfirmPassword = testPassword,
            FirstName = "Test",
            LastName = "User"
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act - Create account
        var createAccountRequest = new CreateAccountRequest
        {
            Name = "Test Account",
            AccountNumber = "1234-5678"
        };

        var createResponse = await httpClient.PostAsJsonAsync("/api/accounts", createAccountRequest, cancellationToken);

        // Assert - Account creation
        Assert.True(createResponse.IsSuccessStatusCode, $"Account creation failed: {await createResponse.Content.ReadAsStringAsync(cancellationToken)}");
        
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>(cancellationToken);
        Assert.NotNull(createdAccount);
        Assert.Equal("Test Account", createdAccount.Name);
        Assert.Equal("1234-5678", createdAccount.AccountNumber);

        // Act - Get accounts
        var getAccountsResponse = await httpClient.GetAsync("/api/accounts", cancellationToken);

        // Assert - Get accounts
        Assert.True(getAccountsResponse.IsSuccessStatusCode);
        
        var accounts = await getAccountsResponse.Content.ReadFromJsonAsync<List<AccountDto>>(cancellationToken);
        Assert.NotNull(accounts);
        Assert.Single(accounts);
        Assert.Equal("Test Account", accounts[0].Name);
    }

    [Fact]
    public async Task CannotAccessAccountsWithoutAuthentication()
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
        var getAccountsResponse = await httpClient.GetAsync("/api/accounts", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, getAccountsResponse.StatusCode);
    }

    [Fact]
    public async Task CannotImportWithoutFile()
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
            ConfirmPassword = testPassword,
            FirstName = "Test",
            LastName = "User"
        };

        await httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken);

        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginResponse = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, cancellationToken);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act - Try to import without file
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("1"), "accountId");

        var importResponse = await httpClient.PostAsync("/api/import/upload", formData, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, importResponse.StatusCode);
    }
}
