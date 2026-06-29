using BffAggregatorService.Application.Contracts;
using BffAggregatorService.Application.UseCases;
using DigiTrade.SharedKernel.Filters;
using MediatR;

namespace BffAggregatorService.Api.Endpoints;

public static class TradingQueryEndpoints
{
    public static IEndpointRouteBuilder MapTradingQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1").WithTags("BffAggregatorService");

        group.MapGet("/portfolio", GetPortfolioAsync)
            .WithSummary("Get portfolio read model")
            .WithDescription(
                "Aggregates position, portfolio, pricing and risk services into a unified portfolio response.")
            .Produces<PortfolioQueryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/positions", GetPositionsAsync)
            .WithSummary("Get positions read model")
            .WithDescription("Aggregates position, pricing and instrument services into account positions.")
            .Produces<PositionsQueryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/exposure", GetExposureAsync)
            .WithSummary("Get exposure read model")
            .WithDescription("Aggregates account, portfolio and risk services into exposure metrics.")
            .Produces<ExposureQueryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/orders", GetOrdersAsync)
            .WithSummary("Get orders list")
            .WithDescription("Aggregates order, account and instrument services into a read-only orders view.")
            .Produces<OrdersQueryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/orders/{id}", GetOrderByIdAsync)
            .WithSummary("Get order by id")
            .WithDescription("Returns one aggregated order view by order identifier.")
            .Produces<OrderViewDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/trades", GetTradesAsync)
            .WithSummary("Get trades list")
            .WithDescription("Aggregates trade, order and instrument services into a trades read model.")
            .Produces<TradesQueryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/account/summary", GetAccountSummaryAsync)
            .WithSummary("Get account summary")
            .WithDescription("Aggregates account, risk and ledger services into account summary metrics.")
            .Produces<AccountSummaryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/account/profile", GetAccountProfileAsync)
            .WithSummary("Get account profile")
            .WithDescription("Aggregates account and identity services into account profile view.")
            .Produces<AccountProfileDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/account/limits", GetAccountLimitsAsync)
            .WithSummary("Get account limits")
            .WithDescription("Aggregates account and risk services into trading limits view.")
            .Produces<AccountLimitsDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/analytics/pnl", GetPnlAsync)
            .WithSummary("Get PnL analytics")
            .WithDescription("Aggregates trade, position and ledger services into PnL analytics.")
            .Produces<AnalyticsPnlDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/analytics/performance", GetPerformanceAsync)
            .WithSummary("Get performance analytics")
            .WithDescription("Aggregates trade, portfolio and reporting services into performance analytics.")
            .Produces<AnalyticsPerformanceDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/analytics/risk", GetRiskAsync)
            .WithSummary("Get risk analytics")
            .WithDescription("Aggregates risk, pricing and portfolio services into risk analytics.")
            .Produces<AnalyticsRiskDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetPortfolioAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetPortfolioQuery(new GetPortfolioQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetPositionsAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetPositionsQuery(new GetPositionsQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetExposureAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetExposureQuery(new GetExposureQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetOrdersAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetOrdersQuery(new GetOrdersQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetOrderByIdAsync(
        string id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetOrderByIdQuery(new GetOrderByIdQuery.Model(id, GetCorrelationId(httpContext))),
            cancellationToken);

        return response is null
            ? Results.NotFound()
            : Results.Ok(response);
    }

    private static async Task<IResult> GetTradesAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetTradesQuery(new GetTradesQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetAccountSummaryAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(
                new GetAccountSummaryQuery(new GetAccountSummaryQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetAccountProfileAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(
                new GetAccountProfileQuery(new GetAccountProfileQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetAccountLimitsAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(
                new GetAccountLimitsQuery(new GetAccountLimitsQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetPnlAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetPnlQuery(new GetPnlQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetPerformanceAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetPerformanceQuery(new GetPerformanceQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static async Task<IResult> GetRiskAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(
            await mediator.Send(new GetRiskQuery(new GetRiskQuery.Model(GetCorrelationId(httpContext))),
                cancellationToken)
        );
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var values)
               && !string.IsNullOrWhiteSpace(values.ToString())
            ? values.ToString()
            : httpContext.TraceIdentifier;
    }
}