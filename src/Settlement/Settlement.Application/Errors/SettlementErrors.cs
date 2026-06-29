using System.Net;
using DigiTrade.SharedKernel.Models.Response;

namespace Settlement.Application.Errors;

public static class SettlementErrors
{
    public static ErrorResult SettlementNotFound(Guid settlementId)
    {
        return new ErrorResult(
            $"settlement.not_found: Settlement '{settlementId}' was not found.",
            (int)HttpStatusCode.NotFound);
    }

    public static ErrorResult InvalidSettlementId()
    {
        return new ErrorResult(
            "settlement.invalid_id: SettlementId must be provided and cannot be empty.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult InvalidSettlementInput()
    {
        return new ErrorResult(
            "settlement.invalid_input: Settlement input is invalid.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult InvalidFailureReason()
    {
        return new ErrorResult(
            "settlement.failure.invalid_reason: Settlement failure reason must be provided.",
            (int)HttpStatusCode.BadRequest);
    }

    public static ErrorResult SettlementCannotBeFinalized(Guid settlementId)
    {
        return new ErrorResult(
            $"settlement.finalize.invalid_status: Settlement '{settlementId}' must be pending finalization before finalization.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult SettlementCannotBeFailed(Guid settlementId)
    {
        return new ErrorResult(
            $"settlement.fail.invalid_status: Settlement '{settlementId}' must be pending finalization before failure.",
            (int)HttpStatusCode.Conflict);
    }

    public static ErrorResult InvalidMutationTimestamp(Guid settlementId)
    {
        return new ErrorResult(
            $"settlement.invalid_timestamp: Settlement '{settlementId}' mutation timestamp cannot move backwards.",
            (int)HttpStatusCode.BadRequest);
    }
}