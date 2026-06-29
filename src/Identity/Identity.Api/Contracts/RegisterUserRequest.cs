namespace Identity.Api.Contracts;

public sealed record RegisterUserInput(string UserName, string Email, string Password);