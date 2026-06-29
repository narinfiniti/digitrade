using BffNotificationService.Infrastructure.Abstractions;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BffNotificationService.Api.UseCases;

public sealed class StreamNotificationCommand(StreamNotificationCommand.Model? input)
    : IUseCase<StreamNotificationCommand.Model, Unit>
{
    public Model? Input => input;

    public sealed record Model(HttpContext HttpContext);

    public sealed class Handler(INotificationWebSocketConnectionService notificationWebSocketConnectionService)
        : IRequestHandler<StreamNotificationCommand, Unit>
    {
        private const string AuthenticatedSubjectHeaderName = "X-Authenticated-Subject";

        public async Task<Unit> Handle(StreamNotificationCommand request, CancellationToken cancellationToken)
        {
            var httpContext = request.Input?.HttpContext ?? throw new ArgumentNullException(nameof(request));
            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "WebSocket upgrade required",
                    "Notification streaming requires a WebSocket upgrade request.",
                    "notification.streaming.invalid_transport",
                    cancellationToken);
                return Unit.Value;
            }

            if (!httpContext.Request.Headers.TryGetValue(AuthenticatedSubjectHeaderName, out var authenticatedSubjectValues)
                || string.IsNullOrWhiteSpace(authenticatedSubjectValues.ToString()))
            {
                await WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    "Notification streaming requires an authenticated subject forwarded by the gateway.",
                    "notification.streaming.missing_authenticated_subject",
                    cancellationToken);
                return Unit.Value;
            }

            using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
            await notificationWebSocketConnectionService.RunConnectionAsync(
                authenticatedSubjectValues.ToString(),
                webSocket,
                cancellationToken);

            return Unit.Value;
        }

        private static Task WriteProblemAsync(
            HttpContext httpContext,
            int statusCode,
            string title,
            string detail,
            string code,
            CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            };

            problem.Extensions["code"] = code;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }
    }
}