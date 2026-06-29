using DigiTrade.Common.Projections;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.Messaging.Configuration;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Publishers;
using DigiTrade.Messaging.Persistence.Outbox;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risk.Application.Abstractions;
using Risk.Application.UseCases;
using Risk.Domain.Margins;
using Risk.Infrastructure.Clients;
using Risk.Infrastructure.Outbox;
using Risk.Persistence;
using Risk.Persistence.Outbox;
using Risk.Persistence.Margins;
using Microsoft.AspNetCore.Builder;

namespace Risk.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IMarginService, MarginService>();
        services.AddSingleton<ILocalDomainEventProjectionStore, InMemoryLocalDomainEventProjectionStore>();
        services.AddDbContext<RiskDbContext>(options =>
            options.UseNpgsql(
                GetRiskConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", RiskDbContext.DefaultSchema)));
        services.AddScoped<IMarginAccountRepository, MarginAccountRepository>();
        services.AddScoped<RiskOutboxStore>();
        services.AddScoped<IRiskOutboxWriter>(sp => sp.GetRequiredService<RiskOutboxStore>());
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<RiskOutboxStore>());
        services.AddScoped<IRiskOutboxPublisher, RiskOutboxPublisher>();
        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .PostConfigure(options =>
            {
                options.TopicName = string.IsNullOrWhiteSpace(options.TopicName) || options.TopicName == "digitrade.domain-events"
                    ? "digitrade.risk-events"
                    : options.TopicName;
                options.ClientId = string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId == "digitrade-domain-events"
                    ? "risk-outbox-publisher"
                    : options.ClientId;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddHttpClient<IOrderReadPort, OrderReadClient>((_, client) =>
            ConfigureClientBaseAddress(client, configuration["Services:Order:BaseUrl"]));
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(OpenMarginAccountCommand).Assembly));

        services.AddTransient<IValidator<OpenMarginAccountCommand.Model>, OpenMarginAccountCommandValidator>();
        services.AddTransient<IValidator<GetMarginAccountByIdQuery.Model>, GetMarginAccountByIdQueryValidator>();
        services.AddTransient<IValidator<ReserveMarginCommand.Model>, ReserveMarginCommandValidator>();
        services.AddTransient<IValidator<ReleaseMarginCommand.Model>, ReleaseMarginCommandValidator>();
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetRiskConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Risk");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["RISK_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("RISK_DB_CONNECTION or ConnectionStrings:Risk must be configured for Risk persistence.");
    }

    public static async Task<WebApplication> EnsureRiskDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }

    private static void ConfigureClientBaseAddress(HttpClient client, string? baseUrl)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
    }
}