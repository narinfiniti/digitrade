using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Order.Application.Abstractions;
using Order.Application.Errors;
using Order.Application.Models;

namespace Order.Application.UseCases;

public sealed class GetOrderByIdQuery(GetOrderByIdQuery.Model? input)
    : IUseCase<GetOrderByIdQuery.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid OrderId) : IRequest<StatusResult>;

    public sealed class Handler(IMapper mapper, IOrderRepository orderRepository) : IRequestHandler<GetOrderByIdQuery, StatusResult>
    {
        public async Task<StatusResult> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.OrderId == Guid.Empty)
            {
                return OrderErrors.InvalidOrderId();
            }

            var order = await orderRepository.FindByIdAsync(input.OrderId, cancellationToken);
            if (order is null)
            {
                return OrderErrors.OrderNotFound(input.OrderId);
            }

            return new DataResult<OrderDetailsModel>(mapper.Map<OrderDetailsModel>(order));
        }
    }
}