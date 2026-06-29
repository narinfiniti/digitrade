using BffNotificationService.Api.Contracts;
using BffNotificationService.Api.UseCases;
using DigiTrade.SharedKernel.Filters;
using MediatR;

namespace BffNotificationService.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/notifications");
        group.MapPost("/terminal-completions", RecordTerminalCompletionAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/deliveries/{notificationDeliveryId:guid}", GetDeliveryAsync)
            .AddEndpointFilter<ResponseResultFilter>();
        group.MapGet("/stream", StreamNotificationsAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> RecordTerminalCompletionAsync(
        TerminalNotificationInput request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var errors = await mediator.Send(
            new ValidateTerminalCompletionQuery(new ValidateTerminalCompletionQuery.Model(request)),
            cancellationToken);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var notificationDelivery = await mediator.Send(
            new RecordTerminalCompletionCommand(new RecordTerminalCompletionCommand.Model(request, httpContext)),
            cancellationToken);
        if (notificationDelivery is null)
        {
            return Results.Problem(
                detail: "The notification delivery record was not persisted after terminal completion processing.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Notification delivery persistence failed");
        }

        return Results.Accepted(
            $"/notifications/deliveries/{notificationDelivery.NotificationDeliveryId}",
            notificationDelivery);
    }

    private static async Task<IResult> GetDeliveryAsync(
        Guid notificationDeliveryId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var notificationDelivery = await mediator.Send(
            new GetNotificationDeliveryQuery(new GetNotificationDeliveryQuery.Model(notificationDeliveryId)),
            cancellationToken);
        return notificationDelivery is null
            ? Results.NotFound()
            : Results.Ok(notificationDelivery);
    }

    private static async Task StreamNotificationsAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new StreamNotificationCommand(new StreamNotificationCommand.Model(httpContext)),
            cancellationToken);
    }
}