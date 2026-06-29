using FluentValidation;

namespace Settlement.Application.UseCases;

public sealed class FailSettlementCommandValidator : AbstractValidator<FailSettlementCommand.Model>
{
    public FailSettlementCommandValidator()
    {
        RuleFor(model => model.SettlementId)
            .NotEmpty();

        RuleFor(model => model.FailureReason)
            .NotEmpty()
            .MaximumLength(512);
    }
}