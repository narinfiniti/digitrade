using FluentValidation;

namespace Trade.Application.UseCases;

public sealed class OpenTradeCommandValidator : AbstractValidator<OpenTradeCommand.Model>
{
    public OpenTradeCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(command => command.InstrumentId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(command => command.Direction)
            .IsInEnum();

        RuleFor(command => command.Quantity)
            .GreaterThan(0m);

        RuleFor(command => command.OpenPrice)
            .GreaterThan(0m);
    }
}