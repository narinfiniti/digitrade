namespace BffAggregatorService.Infrastructure.Options;

public sealed class DownstreamServicesOptions
{
    public const string SectionName = "DownstreamServices";

    public string IdentityServiceBaseUrl { get; set; } = "http://127.0.0.1:5010";

    public string AccountServiceBaseUrl { get; set; } = "http://127.0.0.1:5011";

    public string InstrumentServiceBaseUrl { get; set; } = "http://127.0.0.1:5012";

    public string TradeServiceBaseUrl { get; set; } = "http://127.0.0.1:5013";

    public string OrderServiceBaseUrl { get; set; } = "http://127.0.0.1:5014";

    public string RiskServiceBaseUrl { get; set; } = "http://127.0.0.1:5015";

    public string SettlementServiceBaseUrl { get; set; } = "http://127.0.0.1:5016";

    public string LedgerServiceBaseUrl { get; set; } = "http://127.0.0.1:5017";

    public string PositionServiceBaseUrl { get; set; } = "http://127.0.0.1:5018";

    public string PortfolioServiceBaseUrl { get; set; } = "http://127.0.0.1:5019";

    public string PricingServiceBaseUrl { get; set; } = "http://127.0.0.1:5020";

    public string ReportingServiceBaseUrl { get; set; } = "http://127.0.0.1:5021";

    public string AuditServiceBaseUrl { get; set; } = "http://127.0.0.1:5022";
}