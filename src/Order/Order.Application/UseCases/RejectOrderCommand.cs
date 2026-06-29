using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Order.Application.Abstractions;
using Order.Application.Errors;
using Order.Application.Models;
using Order.Domain.Orders;

namespace Order.Application.UseCases;

public sealed class RejectOrderCommand(RejectOrderCommand.Model? input)
    : IUseCase<RejectOrderCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid OrderId) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IOrderRepository orderRepository,
        IOrderOutboxPublisher orderOutboxPublisher,
        IOrderOutboxWriter orderOutboxWriter,
        IOrderService orderService,
        TimeProvider timeProvider) : IRequestHandler<RejectOrderCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(RejectOrderCommand request, CancellationToken cancellationToken)
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

            if (order.Status != OrderStatus.PendingRiskApproval)
            {
                return OrderErrors.OrderCannotBeRejected(input.OrderId);
            }

            var rejectedAtUtc = timeProvider.GetUtcNow();
            if (rejectedAtUtc < order.UpdatedAt)
            {
                return OrderErrors.InvalidMutationTimestamp(input.OrderId);
            }

            orderService.Reject(order, rejectedAtUtc);
            await orderOutboxWriter.EnqueueAsync(order.DomainEvents, cancellationToken);
            await orderRepository.SaveEntitiesAsync(cancellationToken);
            await orderOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<OrderDetailsModel>(mapper.Map<OrderDetailsModel>(order));
        }
    }
}