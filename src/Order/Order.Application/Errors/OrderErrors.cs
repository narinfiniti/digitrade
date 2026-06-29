using System.Net;
using DigiTrade.SharedKernel.Models.Response;

namespace Order.Application.Errors;

public static class OrderErrors
{
    public static ErrorResult OrderNotFound(Guid orderId)
    {
        return new ErrorResult(
            $"order.not_found: Order '{orderId}' was not found.",
            (int)HttpStatusCode.NotFound);
    }

    public static ErrorResult InvalidOrderId()
    {
        return new ErrorResult(
            "order.invalid_id: OrderId must be provided and cannot be empty.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult InvalidOrderInput()
    {
        return new ErrorResult(
            "order.invalid_input: Order input is invalid.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult OrderCannotBeAccepted(Guid orderId)
    {
        return new ErrorResult(
            $"order.accept.invalid_status: Order '{orderId}' must be pending risk approval before acceptance.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult OrderCannotBeRejected(Guid orderId)
    {
        return new ErrorResult(
            $"order.reject.invalid_status: Order '{orderId}' must be pending risk approval before rejection.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult OrderCannotBeCancelled(Guid orderId)
    {
        return new ErrorResult(
            $"order.cancel.invalid_status: Order '{orderId}' cannot be cancelled in its current status.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult InvalidMutationTimestamp(Guid orderId)
    {
        return new ErrorResult(
            $"order.invalid_timestamp: Order '{orderId}' mutation timestamp cannot move backwards.",
            (int)HttpStatusCode.BadRequest);
    }
}