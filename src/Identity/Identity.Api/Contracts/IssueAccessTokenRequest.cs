namespace Identity.Api.Contracts;

public sealed record IssueAccessTokenInput(string Login, string Password);