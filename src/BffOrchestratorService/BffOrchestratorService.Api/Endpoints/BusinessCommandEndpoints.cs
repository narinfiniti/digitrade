using BffOrchestratorService.Application.Contracts;
using BffOrchestratorService.Application.UseCases;
using DigiTrade.SharedKernel.Filters;
using FluentValidation;
using MediatR;

namespace BffOrchestratorService.Api.Endpoints;

public static class BusinessCommandEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCommandEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1").WithTags("BffOrchestratorService");

        group.MapPost("/trades/place", PlaceTradeAsync)
            .WithSummary("Place trade workflow")
            .WithDescription(
                "Starts synchronous trade placement workflow across Account, Instrument, Trade, Order and Risk services.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/trades/close", CloseTradeAsync)
            .WithSummary("Close trade workflow")
            .WithDescription("Starts synchronous trade close workflow with risk and settlement readiness checks.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/orders/create", CreateOrderAsync)
            .WithSummary("Create order workflow")
            .WithDescription("Starts synchronous order creation workflow with account eligibility and risk checks.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/orders/cancel", CancelOrderAsync)
            .WithSummary("Cancel order workflow")
            .WithDescription("Starts synchronous order cancellation workflow and reconciliation checks.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/orders/modify", ModifyOrderAsync)
            .WithSummary("Modify order workflow")
            .WithDescription("Starts synchronous order modify workflow and applies updated risk controls.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/wallet/deposit", DepositAsync)
            .WithSummary("Deposit wallet funds")
            .WithDescription("Starts synchronous deposit workflow for account balance and ledger posting.")
            .Produces<BusinessCommandDto>(StatusCodes.Status200OK)
            .Produces<BusinessCommandDto>(StatusCodes.Status409Conflict)
            .Produces<BusinessCommandDto>(StatusCodes.Status504GatewayTimeout)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/wallet/withdraw", WithdrawAsync)
            .WithSummary("Withdraw wallet funds")
            .WithDescription("Starts asynchronous withdrawal workflow due to downstream settlement and fraud controls.")
            .Produces<BusinessCommandDto>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/wallet/transfer", TransferAsync)
            .WithSummary("Transfer wallet funds")
            .WithDescription(
                "Starts asynchronous transfer workflow for dual-account consistency and settlement checks.")
            .Produces<BusinessCommandDto>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/kyc/submit", SubmitKycAsync)
            .WithSummary("Submit KYC process")
            .WithDescription("Starts asynchronous KYC submission process and emits verification events.")
            .Produces<BusinessCommandDto>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/verification/start", StartVerificationAsync)
            .WithSummary("Start verification process")
            .WithDescription("Starts asynchronous verification process and creates BusinessProcessState for tracking.")
            .Produces<BusinessCommandDto>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/processes/{processId:guid}", GetProcessAsync)
            .WithSummary("Get process details")
            .WithDescription("Returns process lifecycle details and dependency checkpoints.")
            .Produces<ProcessDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/processes/{processId:guid}/status", GetProcessStatusAsync)
            .WithSummary("Get process status")
            .WithDescription("Returns current process status and checkpoint summary for polling.")
            .Produces<ProcessStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> PlaceTradeAsync(
        TradePlaceInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new TradePlaceRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new PlaceTradeCommand(
                new PlaceTradeCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> CloseTradeAsync(
        TradeCloseInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new TradeCloseRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new CloseTradeCommand(
                new CloseTradeCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> CreateOrderAsync(
        OrderCreateInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new OrderCreateRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new CreateOrderCommand(
                new CreateOrderCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);

    }

    private static async Task<IResult> CancelOrderAsync(
        OrderCancelInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new OrderCancelRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new CancelOrderCommand(
                new CancelOrderCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> ModifyOrderAsync(
        OrderModifyInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new OrderModifyRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new ModifyOrderCommand(
                new ModifyOrderCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> DepositAsync(
        WalletDepositInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new WalletDepositRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new DepositWalletCommand(new DepositWalletCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> WithdrawAsync(
        WalletWithdrawInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new WalletWithdrawRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new WithdrawWalletCommand(new WithdrawWalletCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> TransferAsync(
        WalletTransferInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new WalletTransferRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new TransferWalletCommand(new TransferWalletCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> SubmitKycAsync(
        KycSubmitInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new KycSubmitRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new SubmitKycCommand(new SubmitKycCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> StartVerificationAsync(
        VerificationStartInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new VerificationStartRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(
            new StartVerificationCommand(new StartVerificationCommand.Model(request, httpContext)), cancellationToken);

        return result.StatusCode == StatusCodes.Status202Accepted
            ? Results.Accepted(result.Location, result.Response)
            : Results.Json(result.Response, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetProcessAsync(
        Guid processId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetBusinessProcessByIdQuery(new GetBusinessProcessByIdQuery.Model(processId))
            , cancellationToken);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private static async Task<IResult> GetProcessStatusAsync(
        Guid processId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetBusinessProcessStatusQuery(new GetBusinessProcessStatusQuery.Model(processId))
            , cancellationToken);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private sealed class TradePlaceRequestValidator : AbstractValidator<TradePlaceInput>
    {
        public TradePlaceRequestValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.InstrumentId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0m);
            RuleFor(x => x.Price).GreaterThan(0m);
            RuleFor(x => x.Side).Must(side => side is "Buy" or "Sell").WithMessage("Side must be Buy or Sell.");
        }
    }

    private sealed class TradeCloseRequestValidator : AbstractValidator<TradeCloseInput>
    {
        public TradeCloseRequestValidator()
        {
            RuleFor(x => x.TradeId).NotEmpty();
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0m);
            RuleFor(x => x.Reason).NotEmpty();
        }
    }

    private sealed class OrderCreateRequestValidator : AbstractValidator<OrderCreateInput>
    {
        public OrderCreateRequestValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.InstrumentId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0m);
            RuleFor(x => x.Price).GreaterThan(0m);
            RuleFor(x => x.OrderType).Must(type => type is "Limit" or "Market")
                .WithMessage("OrderType must be Limit or Market.");
            RuleFor(x => x.Side).Must(side => side is "Buy" or "Sell").WithMessage("Side must be Buy or Sell.");
        }
    }

    private sealed class OrderCancelRequestValidator : AbstractValidator<OrderCancelInput>
    {
        public OrderCancelRequestValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty();
        }
    }

    private sealed class OrderModifyRequestValidator : AbstractValidator<OrderModifyInput>
    {
        public OrderModifyRequestValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0m);
            RuleFor(x => x.Price).GreaterThan(0m);
            RuleFor(x => x.Reason).NotEmpty();
        }
    }

    private sealed class WalletDepositRequestValidator : AbstractValidator<WalletDepositInput>
    {
        public WalletDepositRequestValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0m);
            RuleFor(x => x.Currency).Length(3);
            RuleFor(x => x.Reference).NotEmpty();
        }
    }

    private sealed class WalletWithdrawRequestValidator : AbstractValidator<WalletWithdrawInput>
    {
        public WalletWithdrawRequestValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0m);
            RuleFor(x => x.Currency).Length(3);
            RuleFor(x => x.Destination).NotEmpty();
        }
    }

    private sealed class WalletTransferRequestValidator : AbstractValidator<WalletTransferInput>
    {
        public WalletTransferRequestValidator()
        {
            RuleFor(x => x.FromAccountId).NotEmpty();
            RuleFor(x => x.ToAccountId).NotEmpty();
            RuleFor(x => x.FromAccountId).NotEqual(x => x.ToAccountId);
            RuleFor(x => x.Amount).GreaterThan(0m);
            RuleFor(x => x.Currency).Length(3);
            RuleFor(x => x.Reference).NotEmpty();
        }
    }

    private sealed class KycSubmitRequestValidator : AbstractValidator<KycSubmitInput>
    {
        public KycSubmitRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CountryCode).Length(2);
            RuleFor(x => x.DocumentType).NotEmpty();
            RuleFor(x => x.DocumentNumber).NotEmpty();
        }
    }

    private sealed class VerificationStartRequestValidator : AbstractValidator<VerificationStartInput>
    {
        public VerificationStartRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.VerificationType).Must(type => type is "Email" or "Sms" or "Document")
                .WithMessage("VerificationType must be Email, Sms, or Document.");
            RuleFor(x => x.Channel).Must(channel => channel is "Portal" or "Mobile" or "Backoffice")
                .WithMessage("Channel must be Portal, Mobile, or Backoffice.");
        }
    }
}