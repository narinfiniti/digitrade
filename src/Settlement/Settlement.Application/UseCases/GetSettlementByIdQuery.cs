using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Settlement.Application.Abstractions;
using Settlement.Application.Errors;
using Settlement.Application.Models;

namespace Settlement.Application.UseCases;

public sealed class GetSettlementByIdQuery(GetSettlementByIdQuery.Model? input)
    : IUseCase<GetSettlementByIdQuery.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid SettlementId) : IRequest<StatusResult>;

    public sealed class Handler(IMapper mapper, ISettlementRepository settlementRepository) : IRequestHandler<GetSettlementByIdQuery, StatusResult>
    {
        public async Task<StatusResult> Handle(GetSettlementByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.SettlementId == Guid.Empty)
            {
                return SettlementErrors.InvalidSettlementId();
            }

            var settlement = await settlementRepository.FindByIdAsync(input.SettlementId, cancellationToken);
            if (settlement is null)
            {
                return SettlementErrors.SettlementNotFound(input.SettlementId);
            }

            return new DataResult<SettlementDetailsModel>(mapper.Map<SettlementDetailsModel>(settlement));
        }
    }
}