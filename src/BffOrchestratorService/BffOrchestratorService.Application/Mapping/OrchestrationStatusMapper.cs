using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.Mapping;

internal static class OrchestrationStatusMapper
{
    internal static int MapSyncStatusCode(string status)
    {
        return status switch
        {
            "Interrupted" => StatusCodes.Status504GatewayTimeout,
            "PendingDependencies" => StatusCodes.Status503ServiceUnavailable,
            "Failed" => StatusCodes.Status409Conflict,
            "Paused" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status200OK,
        };
    }

    internal static string ToBusinessProcessState(string status)
    {
        return status switch
        {
            "Completed" => "Completed",
            "Failed" => "Compensated",
            "Interrupted" => "TimedOut",
            "PendingDependencies" => "AwaitingDependencies",
            "Paused" => "Paused",
            _ => "Running",
        };
    }
}
