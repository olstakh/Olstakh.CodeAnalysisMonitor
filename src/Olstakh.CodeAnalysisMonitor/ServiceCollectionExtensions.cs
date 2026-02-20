using Microsoft.Extensions.DependencyInjection;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Extension methods for registering Code Analysis Monitor services in DI.
/// New event handlers should be registered here.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all event handlers and supporting services for the Code Analysis Monitor.
    /// </summary>
    /// <param name="services">The service collection to register services in.</param>
    /// <param name="filter">The event filter parsed from CLI arguments.</param>
    /// <param name="liveOutput">Whether live event output is enabled.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCodeAnalysisMonitor(
        this IServiceCollection services,
        EventFilter filter,
        bool liveOutput)
    {
        // Core services
        services.AddSingleton(filter);
        services.AddSingleton<ILiveOutputWriter>(new ConsoleLiveOutputWriter(liveOutput));

        // Event handlers — add new handlers here as they're implemented:
        services.AddSingleton<ICaptureHandler, SingleGeneratorRunTimeHandler>();

        return services;
    }
}
