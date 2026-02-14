namespace BudgetTrackerApp.ApiService.Extensions;

public static class LoggingExtensions
{
    extension(ILogger logger)
    {
        public void Informaion(string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(message, args);
            }
        }

        public void Debug(string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(message, args);
            }
        }
    }
}
