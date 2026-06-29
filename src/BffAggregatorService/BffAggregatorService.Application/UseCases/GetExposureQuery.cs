using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetExposureQuery(GetExposureQuery.Model? input)
    : IUseCase<GetExposureQuery.Model, ExposureQueryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(IServicesHealthSummaryReader reader) : IRequestHandler<GetExposureQuery, ExposureQueryDto>
    {
        public async Task<ExposureQueryDto> Handle(GetExposureQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await reader.GetAsync(request.Input.CorrelationId, cancellationToken);
            var sourceServices = new[] { "Account", "Portfolio", "Risk" };

            return new ExposureQueryDto(
                string.Empty,
                0m,
                0m,
                0m,
                sourceServices,
                QueryCompleteness.IsComplete(summary, sourceServices));
        }
    }
}