namespace BffNotificationService.Api.Contracts;

public sealed record TerminalNotificationInput(
    string AggregateId,
    string RecipientId,
    string Channel,
    string Subject,
    string Message);