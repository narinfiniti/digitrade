using BffAggregatorService.Application.Contracts;
using BffAggregatorService.Application.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetPortfolioQuery(GetPortfolioQuery.Model? input)
    : IUseCase<GetPortfolioQuery.Model, PortfolioQueryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetPortfolioQuery, PortfolioQueryDto>
    {
        public async Task<PortfolioQueryDto> Handle(GetPortfolioQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Portfolio", "Position", "Pricing", "Risk" };

            return new PortfolioQueryDto(
                string.Empty,
                0m,
                0m,
                0m,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}