using System.ComponentModel.DataAnnotations;

namespace BffNotificationService.Infrastructure.Options;

public sealed class TerminalNotificationConsumerOptions
{
    public const string SectionName = "Messaging:Consumers:TerminalNotificationRequested";

    [Required]
    public string ConsumerGroup { get; set; } = "bff-notification-terminal-completion";
}
