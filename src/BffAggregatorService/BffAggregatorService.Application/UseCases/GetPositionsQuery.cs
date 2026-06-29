using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetPositionsQuery(GetPositionsQuery.Model? input)
    : IUseCase<GetPositionsQuery.Model, PositionsQueryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetPositionsQuery, PositionsQueryDto>
    {
        public async Task<PositionsQueryDto> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Position", "Pricing", "Instrument" };

            return new PositionsQueryDto(
                string.Empty,
                [],
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}