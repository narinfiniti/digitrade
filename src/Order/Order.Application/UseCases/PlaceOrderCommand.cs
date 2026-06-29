using System.Net;
using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Order.Application.Abstractions;
using Order.Application.Errors;
using Order.Application.Models;
using Order.Domain.Orders;

namespace Order.Application.UseCases;

public sealed class PlaceOrderCommand(PlaceOrderCommand.Model? input)
    : IUseCase<PlaceOrderCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(
        string AccountId,
        string InstrumentId,
        OrderDirection Direction,
        decimal Quantity,
        decimal RequestedPrice) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IOrderRepository orderRepository,
        IOrderOutboxPublisher orderOutboxPublisher,
        IOrderOutboxWriter orderOutboxWriter,
        IOrderService orderService,
        TimeProvider timeProvider) : IRequestHandler<PlaceOrderCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null ||
                string.IsNullOrWhiteSpace(input.AccountId) ||
                string.IsNullOrWhiteSpace(input.InstrumentId) ||
                input.Quantity <= 0m ||
                input.RequestedPrice <= 0m)
            {
                return OrderErrors.InvalidOrderInput();
            }

            var submittedAtUtc = timeProvider.GetUtcNow();
            var order = orderService.Place(
                input.AccountId,
                input.InstrumentId,
                input.Direction,
                input.Quantity,
                input.RequestedPrice,
                submittedAtUtc);

            await orderRepository.AddAsync(order, cancellationToken);
            await orderOutboxWriter.EnqueueAsync(order.DomainEvents, cancellationToken);
            await orderRepository.SaveEntitiesAsync(cancellationToken);
            await orderOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<OrderDetailsModel>(mapper.Map<OrderDetailsModel>(order), (int)HttpStatusCode.Created);
        }
    }
}