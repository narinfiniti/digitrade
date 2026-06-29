using FluentValidation;

namespace Trade.Application.UseCases;

public sealed class CloseTradeCommandValidator : AbstractValidator<CloseTradeCommand.Model>
{
    public CloseTradeCommandValidator()
    {
        RuleFor(command => command.TradeId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.ClosePrice)
            .GreaterThan(0m);
    }
}