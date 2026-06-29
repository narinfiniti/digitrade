using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Order.Application.Abstractions;
using Order.Application.Errors;
using Order.Application.Models;
using Order.Domain.Orders;

namespace Order.Application.UseCases;

public sealed class CancelOrderCommand(CancelOrderCommand.Model? input)
    : IUseCase<CancelOrderCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid OrderId) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IOrderRepository orderRepository,
        IOrderOutboxPublisher orderOutboxPublisher,
        IOrderOutboxWriter orderOutboxWriter,
        IOrderService orderService,
        TimeProvider timeProvider) : IRequestHandler<CancelOrderCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
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

            if (order.Status is OrderStatus.Rejected or OrderStatus.Cancelled)
            {
                return OrderErrors.OrderCannotBeCancelled(input.OrderId);
            }

            var cancelledAtUtc = timeProvider.GetUtcNow();
            if (cancelledAtUtc < order.UpdatedAt)
            {
                return OrderErrors.InvalidMutationTimestamp(input.OrderId);
            }

            orderService.Cancel(order, cancelledAtUtc);
            await orderOutboxWriter.EnqueueAsync(order.DomainEvents, cancellationToken);
            await orderRepository.SaveEntitiesAsync(cancellationToken);
            await orderOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<OrderDetailsModel>(mapper.Map<OrderDetailsModel>(order));
        }
    }
}