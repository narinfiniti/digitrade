using AutoMapper;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Models.Response;
using FluentValidation;
using MediatR;
using Order.Api.Contracts;
using Order.Application.Models;
using Order.Application.UseCases;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/orders");
        group.MapPost(string.Empty, PlaceOrderAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/{orderId:guid}", GetOrderByIdAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{orderId:guid}/accept", AcceptOrderAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{orderId:guid}/reject", RejectOrderAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{orderId:guid}/cancel", CancelOrderAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<PlaceOrderCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new PlaceOrderCommand.Model(
            request.AccountId,
            request.InstrumentId,
            request.Direction,
            request.Quantity,
            request.RequestedPrice);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new PlaceOrderCommand(model), cancellationToken);
        return ToOrderResult(result, mapper);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid orderId,
        IMapper mapper,
        IMediator mediator,
        IValidator<GetOrderByIdQuery.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new GetOrderByIdQuery.Model(orderId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new GetOrderByIdQuery(model), cancellationToken);
        return ToOrderResult(result, mapper);
    }

    private static async Task<IResult> AcceptOrderAsync(
        Guid orderId,
        IMapper mapper,
        IMediator mediator,
        IValidator<AcceptOrderCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new AcceptOrderCommand.Model(orderId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new AcceptOrderCommand(model), cancellationToken);
        return ToOrderResult(result, mapper);
    }

    private static async Task<IResult> RejectOrderAsync(
        Guid orderId,
        IMapper mapper,
        IMediator mediator,
        IValidator<RejectOrderCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new RejectOrderCommand.Model(orderId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new RejectOrderCommand(model), cancellationToken);
        return ToOrderResult(result, mapper);
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid orderId,
        IMapper mapper,
        IMediator mediator,
        IValidator<CancelOrderCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new CancelOrderCommand.Model(orderId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new CancelOrderCommand(model), cancellationToken);
        return ToOrderResult(result, mapper);
    }

    private static IResult ToOrderResult(StatusResult result, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return result switch
        {
            DataResult<OrderDetailsModel> orderData when orderData.Status == StatusCodes.Status201Created
                => Results.Created($"/api/v1/orders/{orderData.Data.OrderId}", mapper.Map<OrderDto>(orderData.Data)),
            DataResult<OrderDetailsModel> orderData
                => Results.Json(mapper.Map<OrderDto>(orderData.Data), statusCode: orderData.Status),
            ErrorResult error
                => Results.Problem(
                    detail: error.Error,
                    statusCode: error.Status,
                    title: GetProblemTitle(error.Status),
                    extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["code"] = GetErrorCode(error.Error),
                    }),
            _ => Results.StatusCode(result.Status),
        };
    }

    private static string GetProblemTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Invalid order request",
            StatusCodes.Status404NotFound => "Order not found",
            StatusCodes.Status409Conflict => "Order conflict",
            _ => "Order request failed",
        };
    }

    private static string GetErrorCode(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        var separatorIndex = error.IndexOf(':');
        return separatorIndex > 0 ? error[..separatorIndex] : error;
    }
}