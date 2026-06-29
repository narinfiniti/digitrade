using FluentValidation;

namespace Order.Application.UseCases;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand.Model>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(model => model.OrderId)
            .NotEmpty();
    }
}