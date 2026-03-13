namespace BudgetTrackerApp.ApiService.Services;

public static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public void AddServices()
        {
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<ISnapshotService, SnapshotService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IServiceGuard, ServiceGuard>();
        }
    }
}
