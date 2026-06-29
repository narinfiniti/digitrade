namespace BffNotificationService.Api.Contracts;

public sealed record NotificationHistoryItemDto(
    Guid NotificationId,
    string UserId,
    string Category,
    string Channel,
    string Subject,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record NotificationHistoryDto(
    string UserId,
    int Total,
    int Unread,
    IReadOnlyCollection<NotificationHistoryItemDto> Items);

public sealed record NotificationPreferenceDto(
    string UserId,
    bool EmailEnabled,
    bool PushEnabled,
    bool WebSocketEnabled,
    IReadOnlyCollection<string> Categories);

public sealed record UpdateNotificationPreferenceInput(
    string UserId,
    bool EmailEnabled,
    bool PushEnabled,
    bool WebSocketEnabled,
    IReadOnlyCollection<string> Categories);

public sealed record PushRegistrationInput(
    string UserId,
    string DeviceId,
    string Platform,
    string Token,
    IReadOnlyCollection<string> Categories);

public sealed record PushRegistrationDto(
    Guid RegistrationId,
    string UserId,
    string DeviceId,
    string Platform,
    bool Active,
    IReadOnlyCollection<string> Categories,
    DateTimeOffset RegisteredAt);
