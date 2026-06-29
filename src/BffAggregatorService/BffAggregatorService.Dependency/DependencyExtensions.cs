using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Mapping;
using BffAggregatorService.Application.UseCases;
using BffAggregatorService.Infrastructure.Clients;
using BffAggregatorService.Infrastructure.Options;
using BffAggregatorService.Infrastructure.Services;
using DigiTrade.Observability.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BffAggregatorService.Dependency;

public static class DependencyExtensions
{
    private static readonly (string ServiceName, string ClientName, Func<DownstreamServicesOptions, string> UrlSelector)[] DownstreamServices =
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

        services.AddOptions<DownstreamServicesOptions>()
            .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.IdentityServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:IdentityServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.AccountServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:AccountServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.InstrumentServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:InstrumentServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.TradeServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:TradeServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.OrderServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:OrderServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.RiskServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:RiskServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.SettlementServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:SettlementServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.LedgerServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:LedgerServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PositionServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:PositionServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PortfolioServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:PortfolioServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.PricingServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:PricingServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.ReportingServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:ReportingServiceBaseUrl must be an absolute URI.")
            .Validate(
                options => Uri.TryCreate(options.AuditServiceBaseUrl, UriKind.Absolute, out _),
                $"{DownstreamServicesOptions.SectionName}:AuditServiceBaseUrl must be an absolute URI.")
            .ValidateOnStart();

        foreach (var service in DownstreamServices)
        {
            services.AddHttpClient(service.ClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
                httpClient.BaseAddress = new Uri(service.UrlSelector(options), UriKind.Absolute);
            });

            services.AddTransient<IDownstreamServiceHealthClient>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new DownstreamServiceHealthClient(service.ServiceName, httpClientFactory.CreateClient(service.ClientName));
            });
        }

        services.AddTransient<IServicesHealthSummaryReader, ServicesHealthSummaryReader>();
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(GetHealthSummaryQuery).Assembly));
        services.AddAutoMapper(cfg => { }, typeof(BffAggregatorApiMappingProfile));
        services.AddDigiTradeHealthChecks();

        return services;
    }
}