using FluentValidation;

namespace Order.Application.UseCases;

public sealed class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand.Model>
{
    public RejectOrderCommandValidator()
    {
        RuleFor(model => model.OrderId)
            .NotEmpty();
    }
}