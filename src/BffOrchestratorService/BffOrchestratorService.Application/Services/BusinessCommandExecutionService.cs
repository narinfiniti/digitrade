using AutoMapper;
using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.Services;

public interface IBusinessCommandExecutionService
{
    Task<(BusinessCommandDto Response, int StatusCode, string? Location)> ExecuteAsync<TInput>(
        TInput request,
        HttpContext httpContext,
        string commandName,
        string processCode,
        string flowName,
        string mode,
        IReadOnlyCollection<string> involvedServices,
        int? forceStatusCode,
        CancellationToken cancellationToken)
        where TInput : class;
}

public sealed class BusinessCommandExecutionService(
    IMapper mapper,
    IOrchestrationShellService orchestrationShellService)
    : IBusinessCommandExecutionService
{
    public async Task<(BusinessCommandDto Response, int StatusCode, string? Location)> ExecuteAsync<TInput>(
        TInput request,
        HttpContext httpContext,
        string commandName,
        string processCode,
        string flowName,
        string mode,
        IReadOnlyCollection<string> involvedServices,
        int? forceStatusCode,
        CancellationToken cancellationToken)
        where TInput : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = httpContext.GetHeaderOrDefault(HttpContextExtensions.CorrelationHeaderName, httpContext.TraceIdentifier);
        var requestedBySubjectId = httpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedSubjectHeaderName, "anonymous-subject");
        var requestedByUserName = httpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedUserNameHeaderName, "anonymous-user");

        var requestFingerprint = request.ToString() ?? typeof(TInput).Name;
        var idempotencyFallback = $"{flowName}:{correlationId}:{requestFingerprint}";
        var idempotencyKey = httpContext.GetHeaderOrDefault(HttpContextExtensions.IdempotencyHeaderName, idempotencyFallback);

        var orchestration = await orchestrationShellService.StartAsync(
            flowName,
            correlationId,
            idempotencyKey,
            requestedBySubjectId,
            requestedByUserName,
            involvedServices,
            cancellationToken);

        var orchestrationResponse = mapper.Map<OrchestrationShellDto>(orchestration);
        var response = new BusinessCommandDto(
            commandName,
            processCode,
            mode,
            OrchestrationStatusMapper.ToBusinessProcessState(orchestration.Status),
            $"Business process '{processCode}' accepted by orchestrator.",
            correlationId,
            involvedServices,
            orchestrationResponse);

        if (string.Equals(mode, "async", StringComparison.OrdinalIgnoreCase) || forceStatusCode == StatusCodes.Status202Accepted)
        {
            return (response, StatusCodes.Status202Accepted, $"/api/v1/processes/{orchestration.Id}");
        }

        var statusCode = forceStatusCode ?? OrchestrationStatusMapper.MapSyncStatusCode(orchestration.Status);
        return (response, statusCode, null);
    }
}