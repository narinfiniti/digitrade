using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetFailuresQuery(GetFailuresQuery.Model? input)
    : IUseCase<GetFailuresQuery.Model, ServiceFailuresReportDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader servicesHealthSummaryReader)
        : IRequestHandler<GetFailuresQuery, ServiceFailuresReportDto>
    {
        public async Task<ServiceFailuresReportDto> Handle(GetFailuresQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await servicesHealthSummaryReader.GetAsync(request.Input.CorrelationId, cancellationToken);

            var failures = summary.Services
                .Where(static service => !service.IsHealthy)
                .Select(static service => new ServiceFailureDto(
                    service.ServiceName,
                    service.StatusCode,
                    service.Endpoint,
                    service.FailureReason))
                .ToArray();

            return new ServiceFailuresReportDto(
                failures.Length == 0,
                failures.Length,
                failures);
        }
    }
}