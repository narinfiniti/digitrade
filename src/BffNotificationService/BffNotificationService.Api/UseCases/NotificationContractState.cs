using System.Collections.Concurrent;
using BffNotificationService.Api.Contracts;

namespace BffNotificationService.Api.UseCases;

internal static class NotificationContractState
{
    internal static readonly ConcurrentDictionary<string, NotificationPreferenceDto> PreferencesByUser = new(StringComparer.Ordinal);
    internal static readonly ConcurrentDictionary<Guid, PushRegistrationDto> Registrations = new();

    internal static NotificationPreferenceDto CreateDefaultPreferences(string userId)
    {
        return new NotificationPreferenceDto(userId, true, true, true, ["trading", "wallet", "risk", "system"]);
    }

    internal static NotificationHistoryItemDto[] CreateDefaultHistory(string userId, bool unreadOnly)
    {
        var history = new[]
        {
            new NotificationHistoryItemDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                userId,
                "trading",
                "push",
                "Order accepted",
                "Your order ORD-1001 has been accepted.",
                false,
                DateTimeOffset.UtcNow.AddMinutes(-10)),
            new NotificationHistoryItemDto(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                userId,
                "wallet",
                "websocket",
                "Withdrawal completed",
                "Withdrawal workflow completed for account ACC-001.",
                true,
                DateTimeOffset.UtcNow.AddMinutes(-32)),
        };

        return unreadOnly ? history.Where(item => !item.IsRead).ToArray() : history;
    }
}