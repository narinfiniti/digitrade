using FluentValidation;

namespace Settlement.Application.UseCases;

public sealed class FinalizeSettlementCommandValidator : AbstractValidator<FinalizeSettlementCommand.Model>
{
    public FinalizeSettlementCommandValidator()
    {
        RuleFor(model => model.SettlementId)
            .NotEmpty();
    }
}