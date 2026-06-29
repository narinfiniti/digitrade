using System.Net;
using DigiTrade.SharedKernel.Models.Response;

namespace Trade.Application.Errors;

public static class TradeErrors
{
    public static ErrorResult TradeNotFound(Guid tradeId)
    {
        return new ErrorResult(
            $"trade.not_found: Trade '{tradeId}' was not found.",
            (int)HttpStatusCode.NotFound);
    }

    public static ErrorResult TradeAlreadyClosed(Guid tradeId)
    {
        return new ErrorResult(
            $"trade.close.already_closed: Trade '{tradeId}' is already closed.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult InvalidCloseTimestamp(Guid tradeId)
    {
        return new ErrorResult(
            $"trade.close.invalid_timestamp: Trade '{tradeId}' cannot close before it was opened.",
            (int)HttpStatusCode.BadRequest);
    }

    public static StatusResult InvalidTradeId()
    {
        return new ErrorResult(
                $"InvalidTradeId: TradeId must be provided and cannot be empty.",
                (int)HttpStatusCode.BadRequest);
    }

    public static StatusResult InvalidTradeInput()
    {
        return new ErrorResult(
                    $"InvalidTradeInput: Trade input is invalid.",
                    (int)HttpStatusCode.BadRequest);
    }
}