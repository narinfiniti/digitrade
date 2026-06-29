using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetRiskQuery(GetRiskQuery.Model? input)
    : IUseCase<GetRiskQuery.Model, AnalyticsRiskDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetRiskQuery, AnalyticsRiskDto>
    {
        public async Task<AnalyticsRiskDto> Handle(GetRiskQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Risk", "Pricing", "Portfolio" };

            return new AnalyticsRiskDto(
                string.Empty,
                0m,
                0m,
                0m,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}