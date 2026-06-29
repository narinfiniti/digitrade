using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigiTrade.Observability.HealthChecks;

public static class HealthCheckExtensions
{
    private static readonly string[] LiveTags = ["live"];
    private static readonly string[] ReadyTags = ["ready"];

    public static IServiceCollection AddDigiTradeHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHealthChecks()
            .AddCheck("self-live", () => HealthCheckResult.Healthy(), tags: LiveTags)
            .AddCheck("self-ready", () => HealthCheckResult.Healthy(), tags: ReadyTags);

        return services;
    }

    public static IEndpointRouteBuilder MapDigiTradeHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        });

        return endpoints;
    }
}