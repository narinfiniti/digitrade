using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetAccountLimitsQuery(GetAccountLimitsQuery.Model? input)
    : IUseCase<GetAccountLimitsQuery.Model, AccountLimitsDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetAccountLimitsQuery, AccountLimitsDto>
    {
        public async Task<AccountLimitsDto> Handle(GetAccountLimitsQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Account", "Risk" };

            return new AccountLimitsDto(
                string.Empty,
                0m,
                0m,
                0m,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}