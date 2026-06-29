using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetPerformanceQuery(GetPerformanceQuery.Model? input)
    : IUseCase<GetPerformanceQuery.Model, AnalyticsPerformanceDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetPerformanceQuery, AnalyticsPerformanceDto>
    {
        public async Task<AnalyticsPerformanceDto> Handle(GetPerformanceQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Trade", "Portfolio", "Reporting" };

            return new AnalyticsPerformanceDto(
                string.Empty,
                0m,
                0m,
                0m,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}