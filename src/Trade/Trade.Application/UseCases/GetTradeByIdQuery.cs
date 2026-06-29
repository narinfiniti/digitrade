using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Trade.Application.Abstractions;
using Trade.Application.Errors;
using Trade.Application.Models;

namespace Trade.Application.UseCases;

public sealed class GetTradeByIdQuery(GetTradeByIdQuery.Model? input)
    : IUseCase<GetTradeByIdQuery.Model, StatusResult>
{
  public Model? Input => input;

  public sealed record Model(Guid TradeId);

    public sealed class Handler(IMapper mapper, ITradeRepository tradeRepository) : IRequestHandler<GetTradeByIdQuery, StatusResult>
    {
        public async Task<StatusResult> Handle(GetTradeByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.TradeId == Guid.Empty)
            {
                return TradeErrors.InvalidTradeId();
            }
            var trade = await tradeRepository.FindByIdAsync(input.TradeId, cancellationToken);

            if (trade is null)
            {
                return TradeErrors.TradeNotFound(input.TradeId);
            }

            return new DataResult<TradeDetailsModel>(mapper.Map<TradeDetailsModel>(trade));
        }
    }
}