using AutoMapper;
using BffNotificationService.Api.Contracts;
using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Events;
using DigiTrade.Messaging.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class RecordTerminalCompletionCommand(RecordTerminalCompletionCommand.Model? input)
    : IUseCase<RecordTerminalCompletionCommand.Model, NotificationDeliveryDto?>
{
    public Model? Input => input;

    public sealed record Model(TerminalNotificationInput Request, HttpContext HttpContext);

    public sealed class Handler(
        IMapper mapper,
        IIntegrationEventConsumer<TerminalNotificationRequestedEvent> terminalNotificationRequestedConsumer,
        INotificationDeliveryStore notificationDeliveryStore,
        TimeProvider timeProvider)
        : IRequestHandler<RecordTerminalCompletionCommand, NotificationDeliveryDto?>
    {
        private const string CorrelationHeaderName = "X-Correlation-Id";

        public async Task<NotificationDeliveryDto?> Handle(RecordTerminalCompletionCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null)
            {
                return null;
            }

            var integrationEvent = new TerminalNotificationRequestedEvent(
                Guid.NewGuid(),
                "notification.terminal_completion.requested",
                1,
                input.Request.AggregateId.Trim(),
                timeProvider.GetUtcNow(),
                input.Request.RecipientId.Trim(),
                input.Request.Channel.Trim(),
                input.Request.Subject.Trim(),
                input.Request.Message.Trim(),
                GetHeaderOrFallback(input.HttpContext, CorrelationHeaderName, input.HttpContext.TraceIdentifier));

            await terminalNotificationRequestedConsumer.ConsumeAsync(integrationEvent, cancellationToken);
            var notificationDelivery = await notificationDeliveryStore.GetByEventIdAsync(integrationEvent.EventId, cancellationToken);
            return notificationDelivery is null ? null : mapper.Map<NotificationDeliveryDto>(notificationDelivery);
        }

        private static string GetHeaderOrFallback(HttpContext httpContext, string headerName, string fallbackValue)
        {
            return httpContext.Request.Headers.TryGetValue(headerName, out var values)
                   && !string.IsNullOrWhiteSpace(values.ToString())
                ? values.ToString()
                : fallbackValue;
        }
    }
}