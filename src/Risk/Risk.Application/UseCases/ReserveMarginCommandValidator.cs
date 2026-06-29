using FluentValidation;

namespace Risk.Application.UseCases;

public sealed class ReserveMarginCommandValidator : AbstractValidator<ReserveMarginCommand.Model>
{
    public ReserveMarginCommandValidator()
    {
        RuleFor(model => model.MarginAccountId)
            .NotEmpty();

        RuleFor(model => model.Amount)
            .GreaterThan(0m);
    }
}