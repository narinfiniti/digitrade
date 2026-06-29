using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Mapping;
using BffOrchestratorService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffOrchestratorService.Application.UseCases;

public sealed class GetBusinessProcessByIdQuery(GetBusinessProcessByIdQuery.Model? input)
    : IUseCase<GetBusinessProcessByIdQuery.Model, ProcessDetailsDto?>
{
    public Model? Input => input;

    public sealed record Model(Guid ProcessId);

    public sealed class Handler(IOrchestrationShellService orchestrationShellService)
        : IRequestHandler<GetBusinessProcessByIdQuery, ProcessDetailsDto?>
    {
        public async Task<ProcessDetailsDto?> Handle(GetBusinessProcessByIdQuery request, CancellationToken cancellationToken)
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

            return new ProcessDetailsDto(
                process.Id,
                process.FlowName,
                process.Status,
                OrchestrationStatusMapper.ToBusinessProcessState(process.Status),
                process.CorrelationId,
                process.RequestedBySubjectId,
                process.RequestedByUserName,
                process.DependenciesHealthy,
                process.CreatedAt,
                process.UpdatedAt,
                checkpoints);
        }
    }
}