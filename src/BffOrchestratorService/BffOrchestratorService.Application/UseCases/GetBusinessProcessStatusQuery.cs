using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffOrchestratorService.Application.UseCases;

public sealed class GetBusinessProcessStatusQuery(GetBusinessProcessStatusQuery.Model? input)
    : IUseCase<GetBusinessProcessStatusQuery.Model, ProcessStatusDto?>
{
    public Model? Input => input;

    public sealed record Model(Guid ProcessId);

    public sealed class Handler(IOrchestrationShellService orchestrationShellService)
        : IRequestHandler<GetBusinessProcessStatusQuery, ProcessStatusDto?>
    {
        public async Task<ProcessStatusDto?> Handle(GetBusinessProcessStatusQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.ProcessId == Guid.Empty)
            {
                return null;
            }

            var process = await orchestrationShellService.GetAsync(input.ProcessId, cancellationToken);
            if (process is null)
            {
                return null;
            }

            var checkpoints = process.Dependencies.Select((dependency, index) => new ProcessCheckpointDto(
                    index + 1,
                    dependency.ServiceName,
                    dependency.IsHealthy ? "Completed" : "Failed",
                    process.UpdatedAt,
                    dependency.FailureReason))
                .ToArray();

            return new ProcessStatusDto(
                process.Id,
                process.FlowName,
                process.Status,
                OrchestrationStatusMapper.ToBusinessProcessState(process.Status),
                process.DependenciesHealthy,
                process.UpdatedAt,
                checkpoints);
        }
    }
}