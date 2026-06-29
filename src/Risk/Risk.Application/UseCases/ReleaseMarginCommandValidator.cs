using FluentValidation;

namespace Risk.Application.UseCases;

public sealed class ReleaseMarginCommandValidator : AbstractValidator<ReleaseMarginCommand.Model>
{
    public ReleaseMarginCommandValidator()
    {
        RuleFor(model => model.MarginAccountId)
            .NotEmpty();

        RuleFor(model => model.Amount)
            .GreaterThan(0m);
    }
}