using FluentValidation;

namespace Settlement.Application.UseCases;

public sealed class GetSettlementByIdQueryValidator : AbstractValidator<GetSettlementByIdQuery.Model>
{
    public GetSettlementByIdQueryValidator()
    {
        RuleFor(model => model.SettlementId)
            .NotEmpty();
    }
}