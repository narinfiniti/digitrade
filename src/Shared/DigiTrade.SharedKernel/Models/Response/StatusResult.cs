using Microsoft.AspNetCore.Mvc;

namespace DigiTrade.SharedKernel.Models.Response;

public class StatusResult : IActionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusResult"/> class
    /// with the given <paramref name="status"/>.
    /// </summary>
    /// <param name="status">The HTTP status code of the response.</param>
    public StatusResult(int status)
    {
        Status = status;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int Status { get; }
    public virtual Task ExecuteResultAsync(ActionContext context)
    {
        return Task.CompletedTask;
    }
}