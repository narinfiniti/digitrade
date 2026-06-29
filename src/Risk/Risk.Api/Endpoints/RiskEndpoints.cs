using AutoMapper;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Models.Response;
using FluentValidation;
using MediatR;
using Risk.Api.Contracts;
using Risk.Application.Models;
using Risk.Application.UseCases;

namespace Risk.Api.Endpoints;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/margin-accounts");
        group.MapPost(string.Empty, OpenMarginAccountAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/{marginAccountId:guid}", GetMarginAccountByIdAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{marginAccountId:guid}/reserve", ReserveMarginAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/{marginAccountId:guid}/release", ReleaseMarginAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> OpenMarginAccountAsync(
        OpenMarginAccountInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<OpenMarginAccountCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new OpenMarginAccountCommand.Model(
            request.AccountId,
            request.CurrencyCode,
            request.TotalMargin);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new OpenMarginAccountCommand(model), cancellationToken);
        return ToRiskResult(result, mapper);
    }

    private static async Task<IResult> GetMarginAccountByIdAsync(
        Guid marginAccountId,
        IMapper mapper,
        IMediator mediator,
        IValidator<GetMarginAccountByIdQuery.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new GetMarginAccountByIdQuery.Model(marginAccountId);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new GetMarginAccountByIdQuery(model), cancellationToken);
        return ToRiskResult(result, mapper);
    }

    private static async Task<IResult> ReserveMarginAsync(
        Guid marginAccountId,
        AdjustMarginInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<ReserveMarginCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new ReserveMarginCommand.Model(marginAccountId, request.Amount);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new ReserveMarginCommand(model), cancellationToken);
        return ToRiskResult(result, mapper);
    }

    private static async Task<IResult> ReleaseMarginAsync(
        Guid marginAccountId,
        AdjustMarginInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<ReleaseMarginCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new ReleaseMarginCommand.Model(marginAccountId, request.Amount);

        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(new ReleaseMarginCommand(model), cancellationToken);
        return ToRiskResult(result, mapper);
    }

    private static IResult ToRiskResult(StatusResult result, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return result switch
        {
            DataResult<MarginAccountDetailsModel> marginAccountData when marginAccountData.Status == StatusCodes.Status201Created
                => Results.Created($"/api/v1/margin-accounts/{marginAccountData.Data.MarginAccountId}", mapper.Map<MarginAccountDto>(marginAccountData.Data)),
            DataResult<MarginAccountDetailsModel> marginAccountData
                => Results.Json(mapper.Map<MarginAccountDto>(marginAccountData.Data), statusCode: marginAccountData.Status),
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
            StatusCodes.Status400BadRequest => "Invalid margin account request",
            StatusCodes.Status404NotFound => "Margin account not found",
            StatusCodes.Status409Conflict => "Margin account conflict",
            _ => "Margin account request failed",
        };
    }

    private static string GetErrorCode(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        var separatorIndex = error.IndexOf(':');
        return separatorIndex > 0 ? error[..separatorIndex] : error;
    }
}