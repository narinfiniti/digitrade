using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Settlement.Application.Abstractions;
using Settlement.Application.Errors;
using Settlement.Application.Models;
using Settlement.Domain.Settlements;

namespace Settlement.Application.UseCases;

public sealed class FinalizeSettlementCommand(FinalizeSettlementCommand.Model? input)
    : IUseCase<FinalizeSettlementCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid SettlementId) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        ISettlementRepository settlementRepository,
        ISettlementOutboxPublisher settlementOutboxPublisher,
        ISettlementOutboxWriter settlementOutboxWriter,
        ISettlementService settlementService,
        TimeProvider timeProvider) : IRequestHandler<FinalizeSettlementCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(FinalizeSettlementCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.SettlementId == Guid.Empty)
            {
                return SettlementErrors.InvalidSettlementId();
            }

            var settlement = await settlementRepository.FindByIdAsync(input.SettlementId, cancellationToken);
            if (settlement is null)
            {
                return SettlementErrors.SettlementNotFound(input.SettlementId);
            }

            if (settlement.Status != SettlementStatus.PendingFinalization)
            {
                return SettlementErrors.SettlementCannotBeFinalized(input.SettlementId);
            }

            var finalizedAtUtc = timeProvider.GetUtcNow();
            if (finalizedAtUtc < settlement.UpdatedAt)
            {
                return SettlementErrors.InvalidMutationTimestamp(input.SettlementId);
            }

            settlementService.Finalize(settlement, finalizedAtUtc);
            await settlementOutboxWriter.EnqueueAsync(settlement.DomainEvents, cancellationToken);
            await settlementRepository.SaveEntitiesAsync(cancellationToken);
            await settlementOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<SettlementDetailsModel>(mapper.Map<SettlementDetailsModel>(settlement));
        }
    }
}