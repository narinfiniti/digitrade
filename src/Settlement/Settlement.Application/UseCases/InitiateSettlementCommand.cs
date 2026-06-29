using System.Net;
using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Settlement.Application.Abstractions;
using Settlement.Application.Errors;
using Settlement.Application.Models;
using Settlement.Domain.Settlements;

namespace Settlement.Application.UseCases;

public sealed class InitiateSettlementCommand(InitiateSettlementCommand.Model? input)
    : IUseCase<InitiateSettlementCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(
        Guid TradeId,
        string AccountId,
        string CurrencyCode,
        decimal NetAmount) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        ISettlementRepository settlementRepository,
        ISettlementOutboxPublisher settlementOutboxPublisher,
        ISettlementOutboxWriter settlementOutboxWriter,
        ISettlementService settlementService,
        TimeProvider timeProvider) : IRequestHandler<InitiateSettlementCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(InitiateSettlementCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null ||
                input.TradeId == Guid.Empty ||
                string.IsNullOrWhiteSpace(input.AccountId) ||
                string.IsNullOrWhiteSpace(input.CurrencyCode) ||
                input.NetAmount == 0m)
            {
                return SettlementErrors.InvalidSettlementInput();
            }

            var initiatedAtUtc = timeProvider.GetUtcNow();
            var settlement = settlementService.Initiate(
                input.TradeId,
                input.AccountId,
                input.CurrencyCode,
                input.NetAmount,
                initiatedAtUtc);

            await settlementRepository.AddAsync(settlement, cancellationToken);
            await settlementOutboxWriter.EnqueueAsync(settlement.DomainEvents, cancellationToken);
            await settlementRepository.SaveEntitiesAsync(cancellationToken);
            await settlementOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<SettlementDetailsModel>(mapper.Map<SettlementDetailsModel>(settlement), (int)HttpStatusCode.Created);
        }
    }
}