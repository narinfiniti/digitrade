using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Risk.Application.Abstractions;
using Risk.Application.Errors;
using Risk.Application.Models;

namespace Risk.Application.UseCases;

public sealed class ReleaseMarginCommand(ReleaseMarginCommand.Model? input)
    : IUseCase<ReleaseMarginCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid MarginAccountId, decimal Amount) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IMarginAccountRepository marginAccountRepository,
        IRiskOutboxPublisher riskOutboxPublisher,
        IRiskOutboxWriter riskOutboxWriter,
        TimeProvider timeProvider,
        Domain.Margins.IMarginService marginService) : IRequestHandler<ReleaseMarginCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(ReleaseMarginCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.MarginAccountId == Guid.Empty)
            {
                return MarginAccountErrors.InvalidMarginAccountId();
            }

            if (input.Amount <= 0m)
            {
                return MarginAccountErrors.InvalidMutationAmount();
            }

            var marginAccount = await marginAccountRepository.FindByIdAsync(input.MarginAccountId, cancellationToken);
            if (marginAccount is null)
            {
                return MarginAccountErrors.MarginAccountNotFound(input.MarginAccountId);
            }

            var releasedAtUtc = timeProvider.GetUtcNow();
            if (releasedAtUtc < marginAccount.UpdatedAt)
            {
                return MarginAccountErrors.InvalidMutationTimestamp(input.MarginAccountId);
            }

            if (input.Amount > marginAccount.ReservedMargin)
            {
                return MarginAccountErrors.ReleaseExceedsReservedMargin(input.MarginAccountId);
            }

            marginService.Release(marginAccount, input.Amount, releasedAtUtc);
            await riskOutboxWriter.EnqueueAsync(marginAccount.DomainEvents, cancellationToken);
            await marginAccountRepository.SaveEntitiesAsync(cancellationToken);
            await riskOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<MarginAccountDetailsModel>(mapper.Map<MarginAccountDetailsModel>(marginAccount));
        }
    }
}