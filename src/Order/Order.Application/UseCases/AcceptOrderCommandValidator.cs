using FluentValidation;

namespace Order.Application.UseCases;

public sealed class AcceptOrderCommandValidator : AbstractValidator<AcceptOrderCommand.Model>
{
    public AcceptOrderCommandValidator()
    {
        RuleFor(model => model.OrderId)
            .NotEmpty();
    }
}