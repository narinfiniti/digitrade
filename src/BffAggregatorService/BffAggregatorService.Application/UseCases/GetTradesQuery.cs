using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetTradesQuery(GetTradesQuery.Model? input)
    : IUseCase<GetTradesQuery.Model, TradesQueryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetTradesQuery, TradesQueryDto>
    {
        public async Task<TradesQueryDto> Handle(GetTradesQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Trade", "Order", "Instrument" };

            return new TradesQueryDto(
                [],
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}