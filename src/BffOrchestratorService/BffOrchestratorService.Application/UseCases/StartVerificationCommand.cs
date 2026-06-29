using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class StartVerificationCommand(StartVerificationCommand.Model? input)
    : IUseCase<StartVerificationCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(VerificationStartInput Request, HttpContext HttpContext);

    public sealed class Handler(IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<StartVerificationCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(StartVerificationCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "verification.start",
                "verification.start",
                "verification-start",
                "async",
                ["Identity", "Account", "Audit", "Reporting"],
                StatusCodes.Status202Accepted,
                cancellationToken);
        }
    }
}