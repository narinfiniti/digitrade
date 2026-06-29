using DigiTrade.Security.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using Identity.Application.Abstractions;
using Identity.Application.Errors;
using Identity.Application.Models;
using Identity.Application.Support;
using Identity.Domain.Users;
using MediatR;

namespace Identity.Application.UseCases;

public sealed class RegisterUserCommand(RegisterUserCommand.Model? input)
    : IUseCase<RegisterUserCommand.Model, RegisteredUserResultModel>
{
    public Model? Input => input;

    public sealed record Model(string UserName, string Email, string Password);
    public sealed class Handler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        UserDomainService userDomainService) : IRequestHandler<RegisterUserCommand, RegisteredUserResultModel>
    {
        public async Task<RegisteredUserResultModel> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || string.IsNullOrWhiteSpace(input.UserName)
                || string.IsNullOrWhiteSpace(input.Email)
                || string.IsNullOrWhiteSpace(input.Password))
            {
                throw new ArgumentException(IdentityErrors.InvalidRegistrationInput.Error);
            }

            var normalizedUserName = IdentityTextNormalizer.Normalize(input.UserName);
            var normalizedEmail = IdentityTextNormalizer.Normalize(input.Email);

            if (await userRepository.ExistsByUserNameAsync(normalizedUserName, cancellationToken))
            {
                throw new ArgumentException(IdentityErrors.UserNameAlreadyTaken.Error);
            }

            if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            {
                throw new ArgumentException(IdentityErrors.EmailAlreadyTaken.Error);
            }

            var createdAtUtc = timeProvider.GetUtcNow();
            var user = userDomainService.Create(
                Guid.NewGuid(),
                input.UserName.Trim(),
                normalizedUserName,
                input.Email.Trim(),
                normalizedEmail,
                passwordHasher.Hash(input.Password),
                createdAtUtc);

            await userRepository.AddAsync(user, cancellationToken);

            return new RegisteredUserResultModel(user.Id, user.UserName, user.Email, user.CreatedAt);
        }
    }
}