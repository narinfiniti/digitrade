using FluentValidation;

namespace Identity.Application.UseCases;

public sealed class IssueAccessTokenCommandValidator : AbstractValidator<IssueAccessTokenCommand.Model>
{
    public IssueAccessTokenCommandValidator()
    {
        RuleFor(command => command.Login)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}