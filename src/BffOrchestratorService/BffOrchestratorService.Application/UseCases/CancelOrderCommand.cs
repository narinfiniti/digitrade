using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class CancelOrderCommand(CancelOrderCommand.Model? input)
    : IUseCase<CancelOrderCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(OrderCancelInput Request, HttpContext HttpContext);

    public sealed class Handler(
      IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<CancelOrderCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "orders.cancel",
                "orders.cancel",
                "order-cancel",
                "sync",
                ["Identity", "Account", "Order", "Risk", "Audit"],
                null,
                cancellationToken);
        }
    }
}