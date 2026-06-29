using AutoMapper;
using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffOrchestratorService.Application.UseCases;

public sealed class GetOrchestrationShellByIdQuery(GetOrchestrationShellByIdQuery.Model? input)
    : IUseCase<GetOrchestrationShellByIdQuery.Model, OrchestrationShellDto?>
{
    public Model? Input => input;

    public sealed record Model(Guid OrchestrationShellId);

    public sealed class Handler(
        IMapper mapper,
        IOrchestrationShellService orchestrationShellService)
        : IRequestHandler<GetOrchestrationShellByIdQuery, OrchestrationShellDto?>
    {
        public async Task<OrchestrationShellDto?> Handle(GetOrchestrationShellByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.OrchestrationShellId == Guid.Empty)
            {
                return null;
            }

            var orchestrationShell = await orchestrationShellService.GetAsync(input.OrchestrationShellId, cancellationToken);
            return orchestrationShell is null ? null : mapper.Map<OrchestrationShellDto>(orchestrationShell);
        }
    }
}