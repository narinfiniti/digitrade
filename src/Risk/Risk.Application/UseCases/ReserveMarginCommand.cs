using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Risk.Application.Abstractions;
using Risk.Application.Errors;
using Risk.Application.Models;

namespace Risk.Application.UseCases;

public sealed class ReserveMarginCommand(ReserveMarginCommand.Model? input)
    : IUseCase<ReserveMarginCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid MarginAccountId, decimal Amount) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IMarginAccountRepository marginAccountRepository,
        IRiskOutboxPublisher riskOutboxPublisher,
        IRiskOutboxWriter riskOutboxWriter,
        TimeProvider timeProvider,
        Domain.Margins.IMarginService marginService) : IRequestHandler<ReserveMarginCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(ReserveMarginCommand request, CancellationToken cancellationToken)
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

            var reservedAtUtc = timeProvider.GetUtcNow();
            if (reservedAtUtc < marginAccount.UpdatedAt)
            {
                return MarginAccountErrors.InvalidMutationTimestamp(input.MarginAccountId);
            }

            if (input.Amount > marginAccount.TotalMargin - marginAccount.ReservedMargin)
            {
                return MarginAccountErrors.ReserveExceedsAvailableMargin(input.MarginAccountId);
            }

            marginService.Reserve(marginAccount, input.Amount, reservedAtUtc);
            await riskOutboxWriter.EnqueueAsync(marginAccount.DomainEvents, cancellationToken);
            await marginAccountRepository.SaveEntitiesAsync(cancellationToken);
            await riskOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<MarginAccountDetailsModel>(mapper.Map<MarginAccountDetailsModel>(marginAccount));
        }
    }
}