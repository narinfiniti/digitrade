using System.ComponentModel.DataAnnotations;

namespace Audit.Infrastructure.Options;

public sealed class BusinessProcessCompletedConsumerOptions
{
    public const string SectionName = "Messaging:Consumers:BusinessProcessCompleted";

    [Required]
    public string ConsumerGroup { get; set; } = "audit-business-process-evidence";
}
