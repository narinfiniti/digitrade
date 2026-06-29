using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.UseCases;
using DigiTrade.SharedKernel.Filters;
using MediatR;

namespace BffOrchestratorService.Api.Endpoints;

public static class OrchestrationShellEndpoints
{
    public static IEndpointRouteBuilder MapOrchestrationShellEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/orchestrations/requests");
        group.MapPost("/", CreateAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/{orchestrationShellId:guid}", GetByIdAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateOrchestrationShellInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FlowName))
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(CreateOrchestrationShellInput.FlowName)] = ["'Flow Name' must not be empty."],
            };
            return Results.ValidationProblem(errors);
        }

        var result = await mediator.Send(
            new CreateOrchestrationShellCommand(new CreateOrchestrationShellCommand.Model(request, httpContext))
            , cancellationToken);
        return Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid orchestrationShellId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var orchestrationShell = await mediator.Send(
            new GetOrchestrationShellByIdQuery(new GetOrchestrationShellByIdQuery.Model(orchestrationShellId))
            , cancellationToken);
        return orchestrationShell is null ? Results.NotFound() : Results.Ok(orchestrationShell);
    }
}