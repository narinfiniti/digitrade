using BffAggregatorService.Application.UseCases;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Extensions;
using MediatR;

namespace BffAggregatorService.Api.Endpoints;

public static class ServiceAggregationEndpoints
{
    public static IEndpointRouteBuilder MapServiceAggregationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/aggregations/services");
        group.MapGet("/health-summary", GetHealthSummaryAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/readiness", GetReadinessAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/failures", GetFailuresAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/business-domains", GetBusinessDomainsAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetHealthSummaryAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new GetHealthSummaryQuery(new GetHealthSummaryQuery.Model(httpContext.GetCorrelationId())),
            cancellationToken));
    }

    private static async Task<IResult> GetReadinessAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new GetReadinessQuery(new GetReadinessQuery.Model(httpContext.GetCorrelationId())),
            cancellationToken));
    }

    private static async Task<IResult> GetFailuresAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new GetFailuresQuery(new GetFailuresQuery.Model(httpContext.GetCorrelationId())),
            cancellationToken));
    }

    private static async Task<IResult> GetBusinessDomainsAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new GetBusinessDomainsQuery(new GetBusinessDomainsQuery.Model(httpContext.GetCorrelationId())),
            cancellationToken));
    }

    
}