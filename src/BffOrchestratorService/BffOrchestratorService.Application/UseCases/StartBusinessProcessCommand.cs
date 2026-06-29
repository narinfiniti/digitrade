using AutoMapper;
using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class StartBusinessProcessCommand(StartBusinessProcessCommand.Model? input)
    : IUseCase<StartBusinessProcessCommand.Model, (BusinessProcessStartDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(
        string ProcessCode,
        string Mode,
        string FlowName,
        IReadOnlyCollection<string> InvolvedServices,
        StartBusinessProcessInput Request,
        HttpContext HttpContext);

    public sealed class Handler(
        IMapper mapper,
        IOrchestrationShellService orchestrationShellService)
        : IRequestHandler<StartBusinessProcessCommand, (BusinessProcessStartDto Response, int StatusCode, string? Location)>
    {
        public async Task<(BusinessProcessStartDto Response, int StatusCode, string? Location)> Handle(StartBusinessProcessCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            var correlationId = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.CorrelationHeaderName, input.HttpContext.TraceIdentifier);
            var requestedBySubjectId = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedSubjectHeaderName, "anonymous-subject");
            var requestedByUserName = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.AuthenticatedUserNameHeaderName, "anonymous-user");

            var businessKeySegment = string.IsNullOrWhiteSpace(input.Request.BusinessKey) ? "default" : input.Request.BusinessKey.Trim();
            var idempotencyFallback = $"{input.FlowName}:{requestedBySubjectId}:{businessKeySegment}";
            var idempotencyKey = input.HttpContext.GetHeaderOrDefault(HttpContextExtensions.IdempotencyHeaderName, idempotencyFallback);

            var orchestrationShell = await orchestrationShellService.StartAsync(
                input.FlowName,
                correlationId,
                idempotencyKey,
                requestedBySubjectId,
                requestedByUserName,
                input.InvolvedServices,
                cancellationToken);

            var orchestrationResponse = mapper.Map<OrchestrationShellDto>(orchestrationShell);
            var response = new BusinessProcessStartDto(
                input.ProcessCode,
                input.Mode,
                input.FlowName,
                input.InvolvedServices,
                orchestrationResponse);

            if (string.Equals(input.Mode, "async", StringComparison.OrdinalIgnoreCase))
            {
                var location = $"/orchestrations/requests/{orchestrationShell.Id}";
                return (response, StatusCodes.Status202Accepted, location);
            }

            return (response, OrchestrationStatusMapper.MapSyncStatusCode(orchestrationShell.Status), null);
        }
    }
}