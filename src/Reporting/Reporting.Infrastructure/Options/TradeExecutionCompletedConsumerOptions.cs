using System.ComponentModel.DataAnnotations;

namespace Reporting.Infrastructure.Options;

public sealed class TradeExecutionCompletedConsumerOptions
{
    public const string SectionName = "Messaging:Consumers:TradeExecutionCompleted";

    [Required]
    public string ConsumerGroup { get; set; } = "reporting-trade-execution-projections";
}
