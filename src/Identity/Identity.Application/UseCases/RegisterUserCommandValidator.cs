using FluentValidation;

namespace Identity.Application.UseCases;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand.Model>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(64);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}