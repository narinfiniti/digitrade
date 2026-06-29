using AutoMapper;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Models.Response;
using FluentValidation;
using MediatR;
using Settlement.Api.Contracts;
using Settlement.Application.Models;
using Settlement.Application.UseCases;

namespace Settlement.Api.Endpoints;

public static class SettlementEndpoints
{
    public static IEndpointRouteBuilder MapSettlementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/settlements");
        group.MapPost(string.Empty, InitiateSettlementAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/{settlementId:guid}", GetSettlementByIdAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{settlementId:guid}/finalize", FinalizeSettlementAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{settlementId:guid}/fail", FailSettlementAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> InitiateSettlementAsync(
        InitiateSettlementInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<InitiateSettlementCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new InitiateSettlementCommand.Model(
            request.TradeId,
            request.AccountId,
            request.CurrencyCode,
            request.NetAmount);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new InitiateSettlementCommand(model), cancellationToken);
        return ToSettlementResult(result, mapper);
    }

    private static async Task<IResult> GetSettlementByIdAsync(
        Guid settlementId,
        IMapper mapper,
        IMediator mediator,
        IValidator<GetSettlementByIdQuery.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new GetSettlementByIdQuery.Model(settlementId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new GetSettlementByIdQuery(model), cancellationToken);
        return ToSettlementResult(result, mapper);
    }

    private static async Task<IResult> FinalizeSettlementAsync(
        Guid settlementId,
        IMapper mapper,
        IMediator mediator,
        IValidator<FinalizeSettlementCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new FinalizeSettlementCommand.Model(settlementId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new FinalizeSettlementCommand(model), cancellationToken);
        return ToSettlementResult(result, mapper);
    }

    private static async Task<IResult> FailSettlementAsync(
        Guid settlementId,
        FailSettlementInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<FailSettlementCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new FailSettlementCommand.Model(settlementId, request.FailureReason);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new FailSettlementCommand(model), cancellationToken);
        return ToSettlementResult(result, mapper);
    }

    private static IResult ToSettlementResult(StatusResult result, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return result switch
        {
            DataResult<SettlementDetailsModel> settlementData when settlementData.Status == StatusCodes.Status201Created
                => Results.Created($"/api/v1/settlements/{settlementData.Data.SettlementId}", mapper.Map<SettlementDto>(settlementData.Data)),
            DataResult<SettlementDetailsModel> settlementData
                => Results.Json(mapper.Map<SettlementDto>(settlementData.Data), statusCode: settlementData.Status),
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
            StatusCodes.Status400BadRequest => "Invalid settlement request",
            StatusCodes.Status404NotFound => "Settlement not found",
            StatusCodes.Status409Conflict => "Settlement conflict",
            _ => "Settlement request failed",
        };
    }

    private static string GetErrorCode(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        var separatorIndex = error.IndexOf(':');
        return separatorIndex > 0 ? error[..separatorIndex] : error;
    }
}