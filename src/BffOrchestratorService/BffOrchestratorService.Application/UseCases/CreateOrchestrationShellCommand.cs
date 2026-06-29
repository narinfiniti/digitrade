using AutoMapper;
using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class CreateOrchestrationShellCommand(CreateOrchestrationShellCommand.Model? input)
    : IUseCase<CreateOrchestrationShellCommand.Model, (OrchestrationShellDto Response, int StatusCode)>
{
    public Model? Input => input;

    public sealed record Model(CreateOrchestrationShellInput Request, HttpContext HttpContext);

    public sealed class Handler(
        IMapper mapper,
        IOrchestrationShellService orchestrationShellService)
        : IRequestHandler<CreateOrchestrationShellCommand, (OrchestrationShellDto Response, int StatusCode)>
    {
        public async Task<(OrchestrationShellDto Response, int StatusCode)> Handle(CreateOrchestrationShellCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            var correlationId = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.CorrelationHeaderName, input.HttpContext.TraceIdentifier);
            var idempotencyKey = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.IdempotencyHeaderName, correlationId);
            var requestedBySubjectId = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedSubjectHeaderName, "anonymous-subject");
            var requestedByUserName = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedUserNameHeaderName, "anonymous-user");

            var orchestrationShell = await orchestrationShellService.StartAsync(
                input.Request.FlowName.Trim(),
                correlationId,
                idempotencyKey,
                requestedBySubjectId,
                requestedByUserName,
                null,
                cancellationToken);

            var response = mapper.Map<OrchestrationShellDto>(orchestrationShell);
            var statusCode = OrchestrationStatusMapper.MapSyncStatusCode(orchestrationShell.Status);
            return (response, statusCode);
        }
    }
}