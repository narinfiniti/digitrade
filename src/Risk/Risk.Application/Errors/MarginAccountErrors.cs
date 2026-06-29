using System.Net;
using DigiTrade.SharedKernel.Models.Response;

namespace Risk.Application.Errors;

public static class MarginAccountErrors
{
    public static ErrorResult MarginAccountNotFound(Guid marginAccountId)
    {
        return new ErrorResult(
            $"margin_account.not_found: Margin account '{marginAccountId}' was not found.",
            (int)HttpStatusCode.NotFound);
    }

    public static ErrorResult InvalidMarginAccountId()
    {
        return new ErrorResult(
            "margin_account.invalid_id: MarginAccountId must be provided and cannot be empty.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult InvalidMarginAccountInput()
    {
        return new ErrorResult(
            "margin_account.invalid_input: Margin account input is invalid.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult InvalidMutationAmount()
    {
        return new ErrorResult(
            "margin_account.invalid_amount: Margin mutation amount must be greater than zero.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult ReserveExceedsAvailableMargin(Guid marginAccountId)
    {
        return new ErrorResult(
            $"margin_account.reserve.exceeds_available: Margin account '{marginAccountId}' does not have enough available margin.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult ReleaseExceedsReservedMargin(Guid marginAccountId)
    {
        return new ErrorResult(
            $"margin_account.release.exceeds_reserved: Margin account '{marginAccountId}' does not have enough reserved margin.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult InvalidMutationTimestamp(Guid marginAccountId)
    {
        return new ErrorResult(
            $"margin_account.invalid_timestamp: Margin account '{marginAccountId}' mutation timestamp cannot move backwards.",
            (int)HttpStatusCode.BadRequest);
    }
}