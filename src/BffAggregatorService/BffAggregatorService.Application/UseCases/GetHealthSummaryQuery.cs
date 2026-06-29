using AutoMapper;
using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffAggregatorService.Application.UseCases;

public sealed class GetHealthSummaryQuery(GetHealthSummaryQuery.Model? input)
    : IUseCase<GetHealthSummaryQuery.Model, ServiceHealthSummaryDto>
{
    public Model? Input => input;

    public sealed record Model(string CorrelationId);

    public sealed class Handler(
        IMapper mapper,
        IServicesHealthSummaryReader servicesHealthSummaryReader) : IRequestHandler<GetHealthSummaryQuery, ServiceHealthSummaryDto>
    {
        public async Task<ServiceHealthSummaryDto> Handle(GetHealthSummaryQuery request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Input?.CorrelationId);
            var summary = await servicesHealthSummaryReader.GetAsync(request.Input.CorrelationId, cancellationToken);
            return mapper.Map<ServiceHealthSummaryDto>(summary);
        }
    }
}