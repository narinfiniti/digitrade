using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.UseCases;
using DigiTrade.SharedKernel.Filters;
using FluentValidation;
using MediatR;

namespace BffOrchestratorService.Api.Endpoints;

public static class BusinessProcessEndpoints
{
  public static IEndpointRouteBuilder MapBusinessProcessEndpoints(this IEndpointRouteBuilder endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    var group = endpoints.MapGroup("/orchestrations/processes");

    group.MapGet("/catalog", GetCatalog).AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/sync/trade-order-risk",
    (StartBusinessProcessInput request,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken cancellationToken)
            => StartProcessAsync("sync-trade-order-risk", "sync", "sync-trade-order-risk",
              ["Identity", "Account", "Instrument", "Trade", "Order", "Risk"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/sync/settlement-ledger",
    (StartBusinessProcessInput request,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken cancellationToken)
            => StartProcessAsync("sync-settlement-ledger", "sync", "sync-settlement-ledger",
              ["Identity", "Account", "Trade", "Order", "Risk", "Settlement", "Ledger"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/sync/portfolio-pricing",
      (StartBusinessProcessInput request,
      HttpContext httpContext,
      IMediator mediator,
      CancellationToken cancellationToken)
        => StartProcessAsync("sync-portfolio-pricing", "sync", "sync-portfolio-pricing",
          ["Identity", "Account", "Position", "Portfolio", "Pricing", "Risk"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/async/trade-lifecycle",
      (StartBusinessProcessInput request,
      HttpContext httpContext,
      IMediator mediator,
      CancellationToken cancellationToken)
        => StartProcessAsync("async-trade-lifecycle", "async", "async-trade-lifecycle",
          ["Identity", "Account", "Instrument", "Trade", "Order", "Risk", "Settlement", "Ledger"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/async/risk-rebalance",
      (StartBusinessProcessInput request,
      HttpContext httpContext,
      IMediator mediator,
      CancellationToken cancellationToken)
        => StartProcessAsync("async-risk-rebalance", "async", "async-risk-rebalance",
          ["Identity", "Account", "Risk", "Position", "Portfolio", "Pricing", "Reporting", "Audit"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    group.MapPost("/async/post-trade-reporting",
      (StartBusinessProcessInput request,
      HttpContext httpContext,
      IMediator mediator,
      CancellationToken cancellationToken)
        => StartProcessAsync("async-post-trade-reporting", "async", "async-post-trade-reporting",
          ["Identity", "Trade", "Order", "Settlement", "Ledger", "Reporting", "Audit"], request, httpContext, mediator,
              cancellationToken))
        .AddEndpointFilter<ResponseResultFilter>();

    return endpoints;
  }

  private static async Task<IReadOnlyCollection<BusinessProcessDefinitionDto>> GetCatalog(
      IMediator mediator,
      CancellationToken cancellationToken)
  {
    return await mediator.Send(
            new GetBusinessProcessCatalogQuery(new GetBusinessProcessCatalogQuery.Model())
            , cancellationToken);
  }

  private static async Task<IResult> StartProcessAsync(
      string processCode,
      string mode,
      string flowName,
      IReadOnlyCollection<string> involvedServices,
      StartBusinessProcessInput request,
      HttpContext httpContext,
      IMediator mediator,
      CancellationToken cancellationToken)
  {
        var validation = new StartBusinessProcessRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
          return Results.ValidationProblem(validation.ToDictionary());
        }

      var input = new StartBusinessProcessCommand.Model(
          processCode,
          mode,
          flowName,
          involvedServices,
          request,
          httpContext);
      var result = await mediator.Send(new StartBusinessProcessCommand(input), cancellationToken);

    if (result.StatusCode == StatusCodes.Status202Accepted)
    {
      return Results.Accepted(result.Location, result.Response);
    }

    return Results.Json(result.Response, statusCode: result.StatusCode);
  }

  private sealed class StartBusinessProcessRequestValidator : AbstractValidator<StartBusinessProcessInput>
  {
      public StartBusinessProcessRequestValidator()
      {
          RuleForEach(x => x.Attributes!)
              .Must(entry => !string.IsNullOrWhiteSpace(entry.Key))
              .WithMessage("Attributes keys must not be empty when provided.");
      }
  }
}
