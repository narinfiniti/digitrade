
using System.Net;
using DigiTrade.SharedKernel.Models.Response;

namespace Identity.Application.Errors;

public static class IdentityErrors
{
    public static ErrorResult InvalidRegistrationInput => new("identity.registration.invalid: User name, email, and password are required.");

    public static ErrorResult UserNameAlreadyTaken => new(
        "identity.registration.username_conflict: The user name is already in use.",
        (int)HttpStatusCode.Conflict);

    public static ErrorResult EmailAlreadyTaken => new(
        "identity.registration.email_conflict: The email address is already in use.",
        (int)HttpStatusCode.Conflict);

    public static ErrorResult InvalidCredentials => new(
        "identity.authentication.invalid_credentials: The supplied credentials are invalid.",
        (int)HttpStatusCode.Unauthorized);
}