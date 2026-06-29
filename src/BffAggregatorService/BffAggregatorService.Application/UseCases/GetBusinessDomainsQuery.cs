using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetBusinessDomainsQuery(GetBusinessDomainsQuery.Model? input)
    : IUseCase<GetBusinessDomainsQuery.Model, IReadOnlyCollection<BusinessDomainAggregationDto>>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader servicesHealthSummaryReader)
        : IRequestHandler<GetBusinessDomainsQuery, IReadOnlyCollection<BusinessDomainAggregationDto>>
    {
        private static readonly IReadOnlyCollection<BusinessDomainAggregationDto> DomainTemplates =
        [
            new BusinessDomainAggregationDto(
                "client-onboarding",
                "Validates onboarding readiness across identity, account, and instrument microservices.",
                false,
                ["Identity", "Account", "Instrument"],
                [],
                []),
            new BusinessDomainAggregationDto(
                "trade-execution",
                "Validates trade execution path across account, instrument, trade, order, and risk microservices.",
                false,
                ["Account", "Instrument", "Trade", "Order", "Risk"],
                [],
                []),
            new BusinessDomainAggregationDto(
                "settlement-ledger",
                "Validates post-trade settlement and ledger posting path across settlement and ledger microservices.",
                false,
                ["Trade", "Order", "Settlement", "Ledger"],
                [],
                []),
            new BusinessDomainAggregationDto(
                "portfolio-valuation",
                "Validates portfolio valuation and risk views across position, portfolio, pricing, and risk microservices.",
                false,
                ["Position", "Portfolio", "Pricing", "Risk"],
                [],
                []),
            new BusinessDomainAggregationDto(
                "post-trade-controls",
                "Validates compliance and reporting views across reporting and audit microservices.",
                false,
                ["Reporting", "Audit", "Settlement", "Ledger"],
                [],
                []),
        ];

        public async Task<IReadOnlyCollection<BusinessDomainAggregationDto>> Handle(GetBusinessDomainsQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await servicesHealthSummaryReader.GetAsync(request.Input.CorrelationId, cancellationToken);

            var healthMap = summary.Services.ToDictionary(
                service => service.ServiceName,
                service => service.IsHealthy,
                StringComparer.Ordinal);

            var domains = DomainTemplates.Select(template =>
            {
                var healthy = template.RequiredServices
                    .Where(service => healthMap.TryGetValue(service, out var isHealthy) && isHealthy)
                    .ToArray();

                var unhealthy = template.RequiredServices
                    .Where(service => !healthMap.TryGetValue(service, out var isHealthy) || !isHealthy)
                    .ToArray();

                return template with
                {
                    IsHealthy = unhealthy.Length == 0,
                    HealthyServices = healthy,
                    UnhealthyServices = unhealthy,
                };
            }).ToArray();

            return domains;
        }
    }
}