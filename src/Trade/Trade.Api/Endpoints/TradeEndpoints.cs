using AutoMapper;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Models.Response;
using FluentValidation;
using MediatR;
using Trade.Api.Contracts;
using Trade.Application.Models;
using Trade.Application.UseCases;

namespace Trade.Api.Endpoints;

public static class TradeEndpoints
{
    public static IEndpointRouteBuilder MapTradeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/trades");
        group.MapPost(string.Empty, OpenTradeAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/{tradeId:guid}", GetTradeByIdAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{tradeId:guid}/close", CloseTradeAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> OpenTradeAsync(
        OpenTradeInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<OpenTradeCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new OpenTradeCommand.Model(
            request.AccountId,
            request.InstrumentId,
            request.Direction,
            request.Quantity,
            request.OpenPrice);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new OpenTradeCommand(model), cancellationToken);
        return ToTradeResult(result, mapper);
    }

    private static async Task<IResult> GetTradeByIdAsync(
        Guid tradeId,
        IMapper mapper,
        IMediator mediator,
        IValidator<GetTradeByIdQuery.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new GetTradeByIdQuery.Model(tradeId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new GetTradeByIdQuery(model), cancellationToken);
        return ToTradeResult(result, mapper);
    }

    private static async Task<IResult> CloseTradeAsync(
        Guid tradeId,
        CloseTradeInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<CloseTradeCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new CloseTradeCommand.Model(tradeId, request.ClosePrice);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new CloseTradeCommand(model), cancellationToken);
        return ToTradeResult(result, mapper);
    }

    private static IResult ToTradeResult(StatusResult result, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return result switch
        {
            DataResult<TradeDetailsModel> tradeData when tradeData.Status == StatusCodes.Status201Created
                => Results.Created($"/api/v1/trades/{tradeData.Data.TradeId}", mapper.Map<TradeDto>(tradeData.Data)),
            DataResult<TradeDetailsModel> tradeData
                => Results.Json(mapper.Map<TradeDto>(tradeData.Data), statusCode: tradeData.Status),
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
            StatusCodes.Status400BadRequest => "Invalid trade request",
            StatusCodes.Status404NotFound => "Trade not found",
            StatusCodes.Status409Conflict => "Trade conflict",
            _ => "Trade request failed",
        };
    }

    private static string GetErrorCode(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        var separatorIndex = error.IndexOf(':');
        return separatorIndex > 0 ? error[..separatorIndex] : error;
    }
}