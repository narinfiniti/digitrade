using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetOrdersQuery(GetOrdersQuery.Model? input)
    : IUseCase<GetOrdersQuery.Model, OrdersQueryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetOrdersQuery, OrdersQueryDto>
    {
        public async Task<OrdersQueryDto> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Order", "Account", "Instrument" };

            return new OrdersQueryDto(
                [],
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}