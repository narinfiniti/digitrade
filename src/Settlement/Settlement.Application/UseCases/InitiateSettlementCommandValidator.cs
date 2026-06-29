using FluentValidation;

namespace Settlement.Application.UseCases;

public sealed class InitiateSettlementCommandValidator : AbstractValidator<InitiateSettlementCommand.Model>
{
    public InitiateSettlementCommandValidator()
    {
        RuleFor(model => model.TradeId)
            .NotEmpty();

        RuleFor(model => model.AccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(model => model.CurrencyCode)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(model => model.NetAmount)
            .NotEqual(0m);
    }
}