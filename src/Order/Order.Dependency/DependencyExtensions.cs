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
using Order.Application.Abstractions;
using Order.Application.UseCases;
using Order.Infrastructure.Clients;
using Order.Domain.Orders;
using Order.Infrastructure.Outbox;
using Order.Persistence;
using Order.Persistence.Outbox;
using Order.Persistence.Orders;
using Microsoft.AspNetCore.Builder;

namespace Order.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<ILocalDomainEventProjectionStore, InMemoryLocalDomainEventProjectionStore>();
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(
                GetOrderConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", OrderDbContext.DefaultSchema)));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<OrderOutboxStore>();
        services.AddScoped<IOrderOutboxWriter>(sp => sp.GetRequiredService<OrderOutboxStore>());
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<OrderOutboxStore>());
        services.AddScoped<IOrderOutboxPublisher, OrderOutboxPublisher>();
        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .PostConfigure(options =>
            {
                options.TopicName = string.IsNullOrWhiteSpace(options.TopicName) || options.TopicName == "digitrade.domain-events"
                    ? "digitrade.order-events"
                    : options.TopicName;
                options.ClientId = string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId == "digitrade-domain-events"
                    ? "order-outbox-publisher"
                    : options.ClientId;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddHttpClient<IMarginAccountReadPort, RiskMarginAccountReadClient>((_, client) =>
            ConfigureClientBaseAddress(client, configuration["Services:Risk:BaseUrl"]));
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(PlaceOrderCommand).Assembly));

        services.AddTransient<IValidator<PlaceOrderCommand.Model>, PlaceOrderCommandValidator>();
        services.AddTransient<IValidator<GetOrderByIdQuery.Model>, GetOrderByIdQueryValidator>();
        services.AddTransient<IValidator<AcceptOrderCommand.Model>, AcceptOrderCommandValidator>();
        services.AddTransient<IValidator<RejectOrderCommand.Model>, RejectOrderCommandValidator>();
        services.AddTransient<IValidator<CancelOrderCommand.Model>, CancelOrderCommandValidator>();
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetOrderConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Order");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["ORDER_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("ORDER_DB_CONNECTION or ConnectionStrings:Order must be configured for Order persistence.");
    }

    public static async Task<WebApplication> EnsureOrderDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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