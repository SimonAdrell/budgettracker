using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetTrackerApp.ApiService.DTOs;
using Microsoft.Extensions.Logging;

namespace BudgetTrackerApp.Tests;

public class AnalyticsIntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task AnalyticsEndpoints_RequireAuthentication()
    {
        await ExecuteInTestingEnvironmentAsync(async cancellationToken =>
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());
            await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var client = app.CreateHttpClient("apiservice");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var endpoints = new[]
            {
                "/api/v1/analytics/balance-over-time",
                "/api/v1/analytics/income-vs-expenses",
                "/api/v1/analytics/spending-by-category",
                "/api/v1/analytics/category-spending-over-time",
                "/api/v1/analytics/net-worth-over-time"
            };

            foreach (var endpoint in endpoints)
            {
                var response = await client.GetAsync(endpoint, cancellationToken);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AnalyticsEndpoints_UserIsolationAndAllAccountsDefault()
    {
        await ExecuteInTestingEnvironmentAsync(async cancellationToken =>
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());
            await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var client = app.CreateHttpClient("apiservice");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var user1Email = $"analytics_u1_{Guid.NewGuid()}@example.com";
            var user2Email = $"analytics_u2_{Guid.NewGuid()}@example.com";
            var token1 = await RegisterAndLoginAsync(client, user1Email, cancellationToken);
            var token2 = await RegisterAndLoginAsync(client, user2Email, cancellationToken);
            var user1Account1Id = await CreateAccountAsync(client, token1, "User1 Account 1", cancellationToken);
            await CreateAccountAsync(client, token1, "User1 Account 2", cancellationToken);
            var user2AccountId = await CreateAccountAsync(client, token2, "User2 Account", cancellationToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

            var from = "2026-01-01T00:00:00Z";
            var to = "2026-01-03T00:00:00Z";

            var incomeResponse = await GetJsonAsync<IncomeVsExpensesResponse>(
                client,
                $"/api/v1/analytics/income-vs-expenses?bucket=day&fromUtc={from}&toUtc={to}",
                cancellationToken);
            Assert.NotNull(incomeResponse);
            Assert.Equal("USD", incomeResponse.Metadata.CurrencyCode);
            Assert.Equal(3, incomeResponse.Points.Count);
            Assert.All(incomeResponse.Points, p =>
            {
                Assert.Equal(0m, p.Income);
                Assert.Equal(0m, p.Expenses);
                Assert.Equal(0m, p.Net);
            });

            var balanceResponse = await GetJsonAsync<BalanceOverTimeResponse>(
                client,
                $"/api/v1/analytics/balance-over-time?bucket=day&fromUtc={from}&toUtc={to}",
                cancellationToken);
            Assert.NotNull(balanceResponse);
            Assert.Equal(3, balanceResponse.Points.Count);
            Assert.All(balanceResponse.Points, p => Assert.Equal(0m, p.Balance));

            var forbidden = await client.GetAsync(
                $"/api/v1/analytics/net-worth-over-time?bucket=day&fromUtc={from}&toUtc={to}&accountId={user2AccountId}",
                cancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            Assert.NotEqual(user2AccountId, user1Account1Id);
        }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AnalyticsEndpoints_OptionalAccountFilterAndForbiddenAccount()
    {
        await ExecuteInTestingEnvironmentAsync(async cancellationToken =>
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());
            await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var client = app.CreateHttpClient("apiservice");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var user1Email = $"analytics_u1_{Guid.NewGuid()}@example.com";
            var user2Email = $"analytics_u2_{Guid.NewGuid()}@example.com";
            var token1 = await RegisterAndLoginAsync(client, user1Email, cancellationToken);
            var token2 = await RegisterAndLoginAsync(client, user2Email, cancellationToken);
            var user1AccountId = await CreateAccountAsync(client, token1, "User1 Account", cancellationToken);
            var user2AccountId = await CreateAccountAsync(client, token2, "User2 Account", cancellationToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            var from = "2026-01-01T00:00:00Z";
            var to = "2026-01-03T00:00:00Z";

            var filtered = await GetJsonAsync<IncomeVsExpensesResponse>(
                client,
                $"/api/v1/analytics/income-vs-expenses?bucket=day&fromUtc={from}&toUtc={to}&accountId={user1AccountId}",
                cancellationToken);
            Assert.NotNull(filtered);
            Assert.All(filtered.Points, p =>
            {
                Assert.Equal(0m, p.Income);
                Assert.Equal(0m, p.Expenses);
            });

            var forbidden = await client.GetAsync(
                $"/api/v1/analytics/income-vs-expenses?bucket=day&fromUtc={from}&toUtc={to}&accountId={user2AccountId}",
                cancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AnalyticsEndpoints_ReturnExpectedShapesAndTotals()
    {
        await ExecuteInTestingEnvironmentAsync(async cancellationToken =>
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BudgetTrackerApp_AppHost>(cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());
            await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var client = app.CreateHttpClient("apiservice");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            var user1Email = $"analytics_u1_{Guid.NewGuid()}@example.com";
            var user2Email = $"analytics_u2_{Guid.NewGuid()}@example.com";
            var token1 = await RegisterAndLoginAsync(client, user1Email, cancellationToken);
            var token2 = await RegisterAndLoginAsync(client, user2Email, cancellationToken);
            await CreateAccountAsync(client, token1, "User1 Account", cancellationToken);
            await CreateAccountAsync(client, token2, "User2 Account", cancellationToken);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            var from = "2026-01-01T00:00:00Z";
            var to = "2026-01-03T00:00:00Z";

            var spendingByCategory = await GetJsonAsync<SpendingByCategoryResponse>(
                client,
                $"/api/v1/analytics/spending-by-category?bucket=day&fromUtc={from}&toUtc={to}",
                cancellationToken);
            Assert.NotNull(spendingByCategory);
            Assert.Empty(spendingByCategory.Rows);

            var categoryOverTime = await GetJsonAsync<CategorySpendingOverTimeResponse>(
                client,
                $"/api/v1/analytics/category-spending-over-time?bucket=day&fromUtc={from}&toUtc={to}",
                cancellationToken);
            Assert.NotNull(categoryOverTime);
            Assert.Equal(3, categoryOverTime.Points.Count);
            Assert.All(categoryOverTime.Points, p => Assert.Empty(p.Categories));

            var netWorth = await GetJsonAsync<NetWorthOverTimeResponse>(
                client,
                $"/api/v1/analytics/net-worth-over-time?bucket=day&fromUtc={from}&toUtc={to}",
                cancellationToken);
            Assert.NotNull(netWorth);
            Assert.Equal(3, netWorth.Points.Count);
            Assert.All(netWorth.Points, p => Assert.Equal(0m, p.NetWorth));
        }, TestContext.Current.CancellationToken);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        const string password = "Test123!";

        var register = new RegisterRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Analytics",
            LastName = "Tester"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register, cancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        var login = new LoginRequest
        {
            Email = email,
            Password = password
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", login, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        return auth!.Token;
    }

    private static async Task<int> CreateAccountAsync(HttpClient client, string token, string accountName, CancellationToken cancellationToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest
        {
            Name = accountName,
            AccountNumber = $"{Guid.NewGuid():N}".Substring(0, 10)
        }, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var account = await createResponse.Content.ReadFromJsonAsync<AccountDto>(cancellationToken);
        return account!.Id;
    }

    private static async Task<T> GetJsonAsync<T>(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"Request to '{url}' failed with {(int)response.StatusCode}: {body}");
        var parsed = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        Assert.NotNull(parsed);
        return parsed;
    }

    private static async Task ExecuteInTestingEnvironmentAsync(Func<CancellationToken, Task> testBody, CancellationToken cancellationToken)
    {
        var previousEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
            await testBody(cancellationToken);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", previousEnvironment);
        }
    }
}
