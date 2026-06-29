using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Risk.Application.Abstractions;
using Risk.Application.Errors;
using Risk.Application.Models;

namespace Risk.Application.UseCases;

public sealed class GetMarginAccountByIdQuery(GetMarginAccountByIdQuery.Model? input)
    : IUseCase<GetMarginAccountByIdQuery.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(Guid MarginAccountId) : IRequest<StatusResult>;

    public sealed class Handler(IMapper mapper, IMarginAccountRepository marginAccountRepository) : IRequestHandler<GetMarginAccountByIdQuery, StatusResult>
    {
        public async Task<StatusResult> Handle(GetMarginAccountByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.MarginAccountId == Guid.Empty)
            {
                return MarginAccountErrors.InvalidMarginAccountId();
            }

            var marginAccount = await marginAccountRepository.FindByIdAsync(input.MarginAccountId, cancellationToken);
            if (marginAccount is null)
            {
                return MarginAccountErrors.MarginAccountNotFound(input.MarginAccountId);
            }

            return new DataResult<MarginAccountDetailsModel>(mapper.Map<MarginAccountDetailsModel>(marginAccount));
        }
    }
}