using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DigiTrade.SharedKernel.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddOtelObservability(
        this IHostApplicationBuilder builder,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        var serviceName = builder.Environment.ApplicationName;
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        var environmentName = builder.Environment.EnvironmentName;

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(BuildResource(serviceName, serviceVersion, environmentName));
            options.AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion);
                resource.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environmentName
                });
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                configureMetrics?.Invoke(metrics);
                metrics.AddOtlpExporter();
            });

        return builder;
    }

    private static ResourceBuilder BuildResource(string serviceName, string serviceVersion, string environmentName)
    {
        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environmentName
            });
    }
}