using BffNotificationService.Api.Contracts;
using BffNotificationService.Api.UseCases;
using DigiTrade.SharedKernel.Filters;
using FluentValidation;
using MediatR;

namespace BffNotificationService.Api.Endpoints;

public static class NotificationContractEndpoints
{
    private const string AuthenticatedSubjectHeaderName = "X-Authenticated-Subject";

    public static IEndpointRouteBuilder MapNotificationContractEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1").WithTags("BffNotificationService");

        group.MapGet("/notifications", GetHistoryAsync)
            .WithSummary("Get notification history")
            .WithDescription("Returns notification history for an authenticated user.")
            .Produces<NotificationHistoryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/notifications/unread", GetUnreadAsync)
            .WithSummary("Get unread notifications")
            .WithDescription("Returns unread notification history items for an authenticated user.")
            .Produces<NotificationHistoryDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/notifications/{id:guid}", GetNotificationByIdAsync)
            .WithSummary("Get notification by id")
            .WithDescription("Returns one notification by identifier.")
            .Produces<NotificationHistoryItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapGet("/notification-preferences", GetPreferencesAsync)
            .WithSummary("Get notification preferences")
            .WithDescription("Returns user notification delivery preferences.")
            .Produces<NotificationPreferenceDto>(StatusCodes.Status200OK)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPut("/notification-preferences", PutPreferencesAsync)
            .WithSummary("Update notification preferences")
            .WithDescription("Updates user notification preferences for category subscriptions and channels.")
            .Produces<NotificationPreferenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapPost("/push/registrations", RegisterPushAsync)
            .WithSummary("Register push token")
            .WithDescription("Registers a user device token for push notifications.")
            .Produces<PushRegistrationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .AddEndpointFilter<ResponseResultFilter>();

        group.MapDelete("/push/devices/{deviceId}", DeletePushDeviceAsync)
            .WithSummary("Delete push device")
            .WithDescription("Deactivates push registration for a device.")
            .Produces(StatusCodes.Status204NoContent)
            .AddEndpointFilter<ResponseResultFilter>();

        endpoints.MapGet("/ws", StreamNotificationsAsync)
            .WithSummary("Notification websocket endpoint")
            .WithDescription(
                "Establishes websocket stream for real-time notifications with heartbeat and reconnect support.")
            .AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetHistoryAsync(HttpContext httpContext, IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var input = new GetNotificationHistoryQuery.Model(userId);
        return Results.Ok(await mediator.Send(new GetNotificationHistoryQuery(input), cancellationToken));
    }

    private static async Task<IResult> GetUnreadAsync(HttpContext httpContext, IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var input = new GetUnreadNotificationsQuery.Model(userId);
        return Results.Ok(await mediator.Send(new GetUnreadNotificationsQuery(input), cancellationToken));
    }

    private static async Task<IResult> GetNotificationByIdAsync(Guid id, HttpContext httpContext, IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var input = new GetNotificationByIdQuery.Model(userId, id);
        var item = await mediator.Send(new GetNotificationByIdQuery(input), cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> GetPreferencesAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var input = new GetNotificationPreferencesQuery.Model(userId);
        return Results.Ok(await mediator.Send(new GetNotificationPreferencesQuery(input), cancellationToken));
    }

    private static async Task<IResult> PutPreferencesAsync(
        UpdateNotificationPreferenceInput request,
        IMediator mediator,  CancellationToken cancellationToken)
    {
        var validation = new UpdateNotificationPreferenceRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }
        var input = new PutNotificationPreferencesCommand.Model(request);
        var response = await mediator.Send(new PutNotificationPreferencesCommand(input), cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> RegisterPushAsync(PushRegistrationInput request, IMediator mediator,
        CancellationToken cancellationToken)
    {
        var validation = new PushRegistrationRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }
        var input = new RegisterPushCommand.Model(request);
        var registration = await mediator.Send(new RegisterPushCommand(input), cancellationToken);
        return Results.Created($"/api/v1/push/registrations/{registration.RegistrationId}",
            registration);
    }

    private static async Task<IResult> DeletePushDeviceAsync(string deviceId, IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeletePushDeviceCommand(new DeletePushDeviceCommand.Model(deviceId)), cancellationToken);
        return Results.NoContent();
    }

    private static async Task StreamNotificationsAsync(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new StreamNotificationCommand(new StreamNotificationCommand.Model(httpContext)), cancellationToken);
    }

    private static string GetUserId(HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue(AuthenticatedSubjectHeaderName, out var values)
               && !string.IsNullOrWhiteSpace(values.ToString())
            ? values.ToString()
            : "anonymous-subject";
    }

    private sealed class
        UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceInput>
    {
        public UpdateNotificationPreferenceRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Categories).NotNull();
            RuleForEach(x => x.Categories).NotEmpty();
        }
    }

    private sealed class PushRegistrationRequestValidator : AbstractValidator<PushRegistrationInput>
    {
        public PushRegistrationRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DeviceId).NotEmpty();
            RuleFor(x => x.Platform).Must(platform => platform is "ios" or "android" or "web")
                .WithMessage("Platform must be ios, android, or web.");
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Categories).NotNull();
            RuleForEach(x => x.Categories).NotEmpty();
        }
    }
}