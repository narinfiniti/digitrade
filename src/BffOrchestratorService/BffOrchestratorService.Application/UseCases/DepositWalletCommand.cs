using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class DepositWalletCommand(DepositWalletCommand.Model? input)
    : IUseCase<DepositWalletCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(WalletDepositInput Request, HttpContext HttpContext);

    public sealed class Handler(IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<DepositWalletCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(DepositWalletCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "wallet.deposit",
                "wallet.deposit",
                "wallet-deposit",
                "sync",
                ["Identity", "Account", "Settlement", "Ledger", "Audit"],
                null,
                cancellationToken);
        }
    }
}