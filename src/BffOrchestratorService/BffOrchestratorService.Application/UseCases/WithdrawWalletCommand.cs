using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class WithdrawWalletCommand(WithdrawWalletCommand.Model? input)
    : IUseCase<WithdrawWalletCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(WalletWithdrawInput Request, HttpContext HttpContext);

    public sealed class Handler(IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<WithdrawWalletCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(WithdrawWalletCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "wallet.withdraw",
                "wallet.withdraw",
                "wallet-withdraw",
                "async",
                ["Identity", "Account", "Risk", "Settlement", "Ledger", "Audit"],
                null,
                cancellationToken);
        }
    }
}