namespace Identity.Application.Models;

public sealed record RegisteredUserResultModel(Guid UserId, string UserName, string Email, DateTimeOffset CreatedAtUtc);