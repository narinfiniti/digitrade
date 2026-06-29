using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Options;
using BffOrchestratorService.Domain.Services;
using BffOrchestratorService.Infrastructure.Abstractions;
using BffOrchestratorService.Infrastructure.Clients;
using BffOrchestratorService.Infrastructure.Options;
using BffOrchestratorService.Infrastructure.Services;
using BffOrchestratorService.Persistence;
using BffOrchestratorService.Persistence.Stores;
using DigiTrade.Common.Extensions;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BffOrchestratorService.Application.Services;

namespace BffOrchestratorService.Dependency;

public static class DependencyExtensions
{
    private static readonly (string ServiceName, string ClientName, Func<OrchestratorDownstreamServicesOptions, string> UrlSelector)[] DownstreamServices =
    [
        ("Identity", "IdentityService", static options => options.IdentityServiceBaseUrl),
        ("Account", "AccountService", static options => options.AccountServiceBaseUrl),
        ("Instrument", "InstrumentService", static options => options.InstrumentServiceBaseUrl),
        ("Trade", "TradeService", static options => options.TradeServiceBaseUrl),
        ("Order", "OrderService", static options => options.OrderServiceBaseUrl),
        ("Risk", "RiskService", static options => options.RiskServiceBaseUrl),
        ("Settlement", "SettlementService", static options => options.SettlementServiceBaseUrl),
        ("Ledger", "LedgerService", static options => options.LedgerServiceBaseUrl),
        ("Position", "PositionService", static options => options.PositionServiceBaseUrl),
        ("Portfolio", "PortfolioService", static options => options.PortfolioServiceBaseUrl),
        ("Pricing", "PricingService", static options => options.PricingServiceBaseUrl),
        ("Reporting", "ReportingService", static options => options.ReportingServiceBaseUrl),
        ("Audit", "AuditService", static options => options.AuditServiceBaseUrl),
    ];

    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);

        services.AddOptions<OrchestratorDownstreamServicesOptions>()
            .Bind(configuration.GetSection(OrchestratorDownstreamServicesOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.IdentityServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:IdentityServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.AccountServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:AccountServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.InstrumentServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:InstrumentServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.TradeServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:TradeServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.OrderServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:OrderServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.RiskServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:RiskServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.SettlementServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:SettlementServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.LedgerServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:LedgerServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PositionServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:PositionServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PortfolioServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:PortfolioServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PricingServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:PricingServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.ReportingServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:ReportingServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.AuditServiceBaseUrl, UriKind.Absolute, out _),
                $"{OrchestratorDownstreamServicesOptions.SectionName}:AuditServiceBaseUrl must be an absolute URI.")
            .ValidateOnStart();

        services.AddOptions<ProcessQueueWorkerOptions>()
            .Bind(configuration.GetSection(ProcessQueueWorkerOptions.SectionName))
            .Validate(options => options.BatchSize > 0, $"{ProcessQueueWorkerOptions.SectionName}:BatchSize must be greater than zero.")
            .Validate(options => options.BatchSize <= 512, $"{ProcessQueueWorkerOptions.SectionName}:BatchSize must be less than or equal to 512.")
            .Validate(options => options.PollInterval > TimeSpan.Zero, $"{ProcessQueueWorkerOptions.SectionName}:PollInterval must be greater than zero.")
            .Validate(options => options.LeaseDuration > TimeSpan.Zero, $"{ProcessQueueWorkerOptions.SectionName}:LeaseDuration must be greater than zero.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.LeaseOwner), $"{ProcessQueueWorkerOptions.SectionName}:LeaseOwner must not be empty.")
            .ValidateOnStart();

        services.AddOptions<ProcessRuntimeOutboxPublisherOptions>()
            .Bind(configuration.GetSection(ProcessRuntimeOutboxPublisherOptions.SectionName))
            .Validate(options => options.BatchSize > 0, $"{ProcessRuntimeOutboxPublisherOptions.SectionName}:BatchSize must be greater than zero.")
            .Validate(options => options.BatchSize <= 512, $"{ProcessRuntimeOutboxPublisherOptions.SectionName}:BatchSize must be less than or equal to 512.")
            .Validate(options => options.PollInterval > TimeSpan.Zero, $"{ProcessRuntimeOutboxPublisherOptions.SectionName}:PollInterval must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();

        services.AddOptions<ProcessRuntimeOutboxStoreOptions>()
            .Bind(configuration.GetSection(ProcessRuntimeOutboxStoreOptions.SectionName))
            .Validate(options => options.MaxPublishAttempts > 0, $"{ProcessRuntimeOutboxStoreOptions.SectionName}:MaxPublishAttempts must be greater than zero.")
            .Validate(options => options.BaseRetryDelay > TimeSpan.Zero, $"{ProcessRuntimeOutboxStoreOptions.SectionName}:BaseRetryDelay must be greater than zero.")
            .Validate(options => options.MaxRetryDelay > TimeSpan.Zero, $"{ProcessRuntimeOutboxStoreOptions.SectionName}:MaxRetryDelay must be greater than zero.")
            .Validate(options => options.MaxRetryDelay >= options.BaseRetryDelay, $"{ProcessRuntimeOutboxStoreOptions.SectionName}:MaxRetryDelay must be greater than or equal to BaseRetryDelay.")
            .Validate(options => options.ProcessingLeaseTimeout > TimeSpan.Zero, $"{ProcessRuntimeOutboxStoreOptions.SectionName}:ProcessingLeaseTimeout must be greater than zero.")
            .ValidateOnStart();

        foreach (var service in DownstreamServices)
        {
            services.AddHttpClient(service.ClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OrchestratorDownstreamServicesOptions>>().Value;
                httpClient.BaseAddress = new Uri(service.UrlSelector(options), UriKind.Absolute);
            });
        }

        services.AddDbContext<BffOrchestratorDbContext>(options =>
            options.UseNpgsql(
                GetOrchestratorConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BffOrchestratorDbContext.DefaultSchema)));
        services.AddScoped<IBusinessProcessStateStore, BusinessProcessStateStore>();
        services.AddScoped<IProcessCheckpointStore, ProcessCheckpointStore>();
        services.AddScoped<IProcessQueueStore, ProcessQueueStore>();
        services.AddScoped<IProcessRuntimeOutboxStore, ProcessRuntimeOutboxStore>();

        typeof(IDomainService).ApplyForTypesInAssembly(type => services.AddTransient(type), typeof(OrchestrationShellDomainService).Assembly);

        services.AddScoped<IOrchestrationShellStore, DatabaseOrchestrationShellStore>();

        foreach (var service in DownstreamServices)
        {
            services.AddTransient<IDownstreamOrchestrationProbeClient>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new DownstreamServiceProbeClient(service.ServiceName, httpClientFactory.CreateClient(service.ClientName));
            });
        }

        services.AddTransient<IOrchestrationShellService, OrchestrationShellService>();
        services.AddTransient<IBusinessCommandExecutionService, BusinessCommandExecutionService>();
        services.AddTransient<IProcessQueueWorkItemHandler, ResumeWorkItemHandler>();
        services.AddTransient<IProcessQueueWorkItemHandler, TimeoutResumeOnRestartWorkItemHandler>();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddHostedService<ProcessQueueWorker>();
        services.AddHostedService<ProcessRuntimeOutboxPublisherWorker>();

        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetOrchestratorConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BffOrchestratorService");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["BFF_ORCHESTRATOR_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("BFF_ORCHESTRATOR_DB_CONNECTION or ConnectionStrings:BffOrchestratorService must be configured for BffOrchestratorService persistence.");
    }
}