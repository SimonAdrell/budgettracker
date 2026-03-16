using System.Runtime.CompilerServices;

namespace BudgetTrackerApp.Tests;

internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("AppHost__StartReactFrontend", "false");
    }
}
