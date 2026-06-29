using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetAccountProfileQuery(GetAccountProfileQuery.Model? input)
    : IUseCase<GetAccountProfileQuery.Model, AccountProfileDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetAccountProfileQuery, AccountProfileDto>
    {
        public async Task<AccountProfileDto> Handle(GetAccountProfileQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Account", "Identity" };

            return new AccountProfileDto(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}