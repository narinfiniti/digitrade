using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetOrderByIdQuery(GetOrderByIdQuery.Model? input)
    : IUseCase<GetOrderByIdQuery.Model, OrderViewDto?>
{
    public Model? Input => input;

    public sealed record Model(string Id, string CorrelationId);

    public sealed class Handler : IRequestHandler<GetOrderByIdQuery, OrderViewDto?>
    {
        public Task<OrderViewDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult<OrderViewDto?>(null);
        }
    }
}