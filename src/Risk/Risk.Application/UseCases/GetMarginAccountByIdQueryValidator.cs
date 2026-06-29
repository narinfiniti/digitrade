using FluentValidation;

namespace Risk.Application.UseCases;

public sealed class GetMarginAccountByIdQueryValidator : AbstractValidator<GetMarginAccountByIdQuery.Model>
{
    public GetMarginAccountByIdQueryValidator()
    {
        RuleFor(model => model.MarginAccountId)
            .NotEmpty();
    }
}