using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.Services;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace BffOrchestratorService.Application.UseCases;

public sealed class SubmitKycCommand(SubmitKycCommand.Model? input)
    : IUseCase<SubmitKycCommand.Model, (BusinessCommandDto Response, int StatusCode, string? Location)>
{
    public Model? Input => input;

    public sealed record Model(KycSubmitInput Request, HttpContext HttpContext);

    public sealed class Handler(IBusinessCommandExecutionService businessCommandExecutionService)
        : IRequestHandler<SubmitKycCommand, (BusinessCommandDto Response, int StatusCode, string? Location)>
    {
        public Task<(BusinessCommandDto Response, int StatusCode, string? Location)> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input ?? throw new ArgumentNullException(nameof(request));
            return businessCommandExecutionService.ExecuteAsync(
                input.Request,
                input.HttpContext,
                "kyc.submit",
                "kyc.submit",
                "kyc-submit",
                "async",
                ["Identity", "Account", "Audit", "Reporting"],
                StatusCodes.Status202Accepted,
                cancellationToken);
        }
    }
}