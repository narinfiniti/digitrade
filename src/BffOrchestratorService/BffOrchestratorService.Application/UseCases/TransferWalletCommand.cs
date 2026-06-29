using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class TransferWalletCommand(TransferWalletCommand.Model? input)
    : IUseCase<TransferWalletCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(WalletTransferInput Request, HttpContext HttpContext);

    public sealed class Handler(IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<TransferWalletCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(TransferWalletCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "wallet.transfer",
                "wallet.transfer",
                "wallet-transfer",
                "async",
                ["Identity", "Account", "Risk", "Settlement", "Ledger", "Audit"],
                null,
                cancellationToken);
        }
    }
}