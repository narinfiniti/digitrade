using FluentValidation;

namespace Risk.Application.UseCases;

public sealed class OpenMarginAccountCommandValidator : AbstractValidator<OpenMarginAccountCommand.Model>
{
    public OpenMarginAccountCommandValidator()
    {
        RuleFor(model => model.AccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(model => model.CurrencyCode)
            .NotEmpty()
            .Length(3);

        RuleFor(model => model.TotalMargin)
            .GreaterThanOrEqualTo(0m);
    }
}