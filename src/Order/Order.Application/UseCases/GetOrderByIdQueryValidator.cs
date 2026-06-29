using FluentValidation;

namespace Order.Application.UseCases;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery.Model>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(model => model.OrderId)
            .NotEmpty();
    }
}