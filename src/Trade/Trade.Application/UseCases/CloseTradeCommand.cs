using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Trade.Application.Abstractions;
using Trade.Application.Errors;
using Trade.Application.Models;
using Trade.Domain.Trades;

namespace Trade.Application.UseCases;

public sealed class CloseTradeCommand(CloseTradeCommand.Model? input)
    : IUseCase<CloseTradeCommand.Model, StatusResult>
{
  public Model? Input => input;
  
  public sealed record Model(Guid TradeId, decimal ClosePrice);

    public sealed class Handler(
        IMapper mapper,
        ITradeRepository tradeRepository,
        ITradeOutboxPublisher tradeOutboxPublisher,
        ITradeOutboxWriter tradeOutboxWriter,
        ITradeService tradeService,
        TimeProvider timeProvider) : IRequestHandler<CloseTradeCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(CloseTradeCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if(input is null || input.TradeId == Guid.Empty)
            {
                return TradeErrors.InvalidTradeId();
            }
            var trade = await tradeRepository.FindByIdAsync(input.TradeId, cancellationToken);

            if (trade is null)
            {
                return TradeErrors.TradeNotFound(input.TradeId);
            }

            if (trade.Status == TradeStatus.Closed)
            {
                return TradeErrors.TradeAlreadyClosed(input.TradeId);
            }

            var closedAtUtc = timeProvider.GetUtcNow();

            if (closedAtUtc < trade.OpenedAtUtc)
            {
                return TradeErrors.InvalidCloseTimestamp(input.TradeId);
            }

            tradeService.Close(trade, input.ClosePrice, closedAtUtc);
            await tradeOutboxWriter.EnqueueAsync(trade.DomainEvents, cancellationToken);
            await tradeRepository.SaveEntitiesAsync(cancellationToken);
            await tradeOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<TradeDetailsModel>(mapper.Map<TradeDetailsModel>(trade));
        }
    }
}