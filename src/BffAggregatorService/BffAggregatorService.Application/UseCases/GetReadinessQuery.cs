using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetReadinessQuery(GetReadinessQuery.Model? input)
    : IUseCase<GetReadinessQuery.Model, ServiceReadinessDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader servicesHealthSummaryReader)
        : IRequestHandler<GetReadinessQuery, ServiceReadinessDto>
    {
        public async Task<ServiceReadinessDto> Handle(GetReadinessQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await servicesHealthSummaryReader.GetAsync(request.Input.CorrelationId, cancellationToken);

            var healthyServices = summary.Services
                .Where(static service => service.IsHealthy)
                .Select(static service => service.ServiceName)
                .ToArray();
            var unhealthyServices = summary.Services
                .Where(static service => !service.IsHealthy)
                .Select(static service => service.ServiceName)
                .ToArray();

            return new ServiceReadinessDto(
                summary.IsHealthy,
                summary.Services.Count,
                healthyServices.Length,
                unhealthyServices.Length,
                healthyServices,
                unhealthyServices);
        }
    }
}