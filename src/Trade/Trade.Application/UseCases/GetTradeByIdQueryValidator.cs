using FluentValidation;

namespace Trade.Application.UseCases;

public sealed class GetTradeByIdQueryValidator : AbstractValidator<GetTradeByIdQuery.Model>
{
    public GetTradeByIdQueryValidator()
    {
        RuleFor(query => query.TradeId)
            .NotEqual(Guid.Empty);
    }
}