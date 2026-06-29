using FluentValidation;

namespace Order.Application.UseCases;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand.Model>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(model => model.AccountId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(model => model.InstrumentId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(model => model.Quantity)
            .GreaterThan(0m);

        RuleFor(model => model.RequestedPrice)
            .GreaterThan(0m);
    }
}