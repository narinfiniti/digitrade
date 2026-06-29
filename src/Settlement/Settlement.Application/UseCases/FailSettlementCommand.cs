using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Settlement.Application.Abstractions;
using Settlement.Application.Errors;
using Settlement.Application.Models;
using Settlement.Domain.Settlements;

namespace Settlement.Application.UseCases;

public sealed class FailSettlementCommand(FailSettlementCommand.Model? input)
    : IUseCase<FailSettlementCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid SettlementId, string FailureReason) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        ISettlementRepository settlementRepository,
        ISettlementOutboxPublisher settlementOutboxPublisher,
        ISettlementOutboxWriter settlementOutboxWriter,
        ISettlementService settlementService,
        TimeProvider timeProvider) : IRequestHandler<FailSettlementCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(FailSettlementCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.SettlementId == Guid.Empty)
            {
                return SettlementErrors.InvalidSettlementId();
            }

            if (string.IsNullOrWhiteSpace(input.FailureReason))
            {
                return SettlementErrors.InvalidFailureReason();
            }

            var settlement = await settlementRepository.FindByIdAsync(input.SettlementId, cancellationToken);
            if (settlement is null)
            {
                return SettlementErrors.SettlementNotFound(input.SettlementId);
            }

            if (settlement.Status != SettlementStatus.PendingFinalization)
            {
                return SettlementErrors.SettlementCannotBeFailed(input.SettlementId);
            }

            var failedAtUtc = timeProvider.GetUtcNow();
            if (failedAtUtc < settlement.UpdatedAt)
            {
                return SettlementErrors.InvalidMutationTimestamp(input.SettlementId);
            }

            settlementService.Fail(settlement, input.FailureReason, failedAtUtc);
            await settlementOutboxWriter.EnqueueAsync(settlement.DomainEvents, cancellationToken);
            await settlementRepository.SaveEntitiesAsync(cancellationToken);
            await settlementOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<SettlementDetailsModel>(mapper.Map<SettlementDetailsModel>(settlement));
        }
    }
}