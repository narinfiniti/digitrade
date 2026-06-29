namespace BffOrchestratorService.Application.Contracts;

public sealed record TradePlaceInput(
    string AccountId,
    string InstrumentId,
    decimal Quantity,
    decimal Price,
    string Side,
    string? ClientOrderId);

public sealed record TradeCloseInput(
    string TradeId,
    string AccountId,
    decimal Quantity,
    string Reason);

public sealed record OrderCreateInput(
    string AccountId,
    string InstrumentId,
    decimal Quantity,
    decimal Price,
    string OrderType,
    string Side);

public sealed record OrderCancelInput(
    string OrderId,
    string AccountId,
    string Reason);

public sealed record OrderModifyInput(
    string OrderId,
    string AccountId,
    decimal Quantity,
    decimal Price,
    string Reason);

public sealed record WalletDepositInput(
    string AccountId,
    decimal Amount,
    string Currency,
    string Reference);

public sealed record WalletWithdrawInput(
    string AccountId,
    decimal Amount,
    string Currency,
    string Destination);

public sealed record WalletTransferInput(
    string FromAccountId,
    string ToAccountId,
    decimal Amount,
    string Currency,
    string Reference);

public sealed record KycSubmitInput(
    string UserId,
    string CountryCode,
    string DocumentType,
    string DocumentNumber);

public sealed record VerificationStartInput(
    string UserId,
    string VerificationType,
    string Channel);

public sealed record BusinessCommandDto(
    string Command,
    string ProcessCode,
    string Mode,
    string BusinessProcessState,
    string Message,
    string CorrelationId,
    IReadOnlyCollection<string> InvolvedServices,
    OrchestrationShellDto Orchestration);

public sealed record ProcessCheckpointDto(
    int Sequence,
    string Name,
    string Status,
    DateTimeOffset RecordedAt,
    string? Detail);

public sealed record ProcessStatusDto(
    Guid ProcessId,
    string FlowName,
    string Status,
    string BusinessProcessState,
    bool DependenciesHealthy,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProcessCheckpointDto> Checkpoints);

public sealed record ProcessDetailsDto(
    Guid ProcessId,
    string FlowName,
    string Status,
    string BusinessProcessState,
    string CorrelationId,
    string RequestedBySubjectId,
    string RequestedByUserName,
    bool DependenciesHealthy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProcessCheckpointDto> Checkpoints);
