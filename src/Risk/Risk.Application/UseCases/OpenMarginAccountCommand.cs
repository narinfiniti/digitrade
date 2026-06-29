using System.Net;
using AutoMapper;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Models.Response;
using MediatR;
using Risk.Application.Abstractions;
using Risk.Application.Errors;
using Risk.Application.Models;
using Risk.Domain.Margins;

namespace Risk.Application.UseCases;

public sealed class OpenMarginAccountCommand(OpenMarginAccountCommand.Model? input)
    : IUseCase<OpenMarginAccountCommand.Model, StatusResult>
{
    public Model? Input => input;

    public sealed record Model(string AccountId, string CurrencyCode, decimal TotalMargin) : IRequest<StatusResult>;

    public sealed class Handler(
        IMapper mapper,
        IMarginAccountRepository marginAccountRepository,
        IRiskOutboxPublisher riskOutboxPublisher,
        IRiskOutboxWriter riskOutboxWriter,
        IMarginService marginService,
        TimeProvider timeProvider) : IRequestHandler<OpenMarginAccountCommand, StatusResult>
    {
        public async Task<StatusResult> Handle(OpenMarginAccountCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null ||
                string.IsNullOrWhiteSpace(input.AccountId) ||
                string.IsNullOrWhiteSpace(input.CurrencyCode) ||
                input.TotalMargin < 0m)
            {
                return MarginAccountErrors.InvalidMarginAccountInput();
            }

            var openedAtUtc = timeProvider.GetUtcNow();
            var marginAccount = marginService.Open(
                input.AccountId,
                input.CurrencyCode,
                input.TotalMargin,
                openedAtUtc);

            await marginAccountRepository.AddAsync(marginAccount, cancellationToken);
            await riskOutboxWriter.EnqueueAsync(marginAccount.DomainEvents, cancellationToken);
            await marginAccountRepository.SaveEntitiesAsync(cancellationToken);
            await riskOutboxPublisher.PublishPendingAsync(cancellationToken);

            return new DataResult<MarginAccountDetailsModel>(mapper.Map<MarginAccountDetailsModel>(marginAccount), (int)HttpStatusCode.Created);
        }
    }
}