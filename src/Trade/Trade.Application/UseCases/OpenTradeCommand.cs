using System.Net;
using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Trade.Application.Abstractions;
using Trade.Application.Errors;
using Trade.Application.Models;
using Trade.Domain.Trades;

namespace Trade.Application.UseCases;

public sealed class OpenTradeCommand(OpenTradeCommand.Model? input)
    : IUseCase<OpenTradeCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(
        string AccountId,
        string InstrumentId,
        TradeDirection Direction,
        decimal Quantity,
        decimal OpenPrice) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        ITradeRepository tradeRepository,
        ITradeOutboxPublisher tradeOutboxPublisher,
        ITradeOutboxWriter tradeOutboxWriter,
        ITradeService tradeService,
        TimeProvider timeProvider) : IRequestHandler<OpenTradeCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(OpenTradeCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null ||
                string.IsNullOrWhiteSpace(input.AccountId) ||
                string.IsNullOrWhiteSpace(input.InstrumentId) ||
                input.Quantity <= 0 || input.OpenPrice <= 0)
            {
                return TradeErrors.InvalidTradeInput();
            }
            var openedAtUtc = timeProvider.GetUtcNow();
            var trade = tradeService.Open(
                input.AccountId,
                input.InstrumentId,
                input.Direction,
                input.Quantity,
                input.OpenPrice,
                openedAtUtc);

            await tradeRepository.AddAsync(trade, cancellationToken);
            await tradeOutboxWriter.EnqueueAsync(trade.DomainEvents, cancellationToken);
            await tradeRepository.SaveEntitiesAsync(cancellationToken);
            await tradeOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<TradeDetailsModel>(mapper.Map<TradeDetailsModel>(trade), (int)HttpStatusCode.Created);
        }
    }
}